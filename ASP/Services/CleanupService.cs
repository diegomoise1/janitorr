using JanitorAspNet.Configuration;
using JanitorAspNet.Models;
using JanitorAspNet.Clients;
using JanitorAspNet.Webhooks;
using Microsoft.Extensions.Options;
using System.Linq;
using System.IO;

namespace JanitorAspNet.Services;

/// <summary>
/// Media cleanup service
/// Transpiled from AbstractCleanupSchedule.kt and related cleanup classes
/// </summary>
public interface ICleanupService
{
    Task<CleanupStatus> GetStatusAsync();
    Task<int> RunCleanupAsync(CleanupType type, bool dryRun = true);
    Task<List<MediaItem>> GetItemsForCleanupAsync(CleanupType type);
}

public class CleanupService : ICleanupService
{
    private readonly ApplicationOptions _options;
    private readonly IJellyseerrClient _jellyseerrClient;
    private readonly IRadarrClient _radarrClient;
    private readonly ISonarrClient _sonarrClient;
    private readonly IWebhookService _webhookService;
    private readonly ILogger<CleanupService> _logger;

    private static CleanupStatus _currentStatus = new()
    {
        IsRunning = false,
        LastRun = null,
        NextRun = null,
        ItemsDeleted = 0,
        SpaceFreed = 0
    };

    public CleanupService(
        IOptions<ApplicationOptions> options,
        IJellyseerrClient jellyseerrClient,
        IRadarrClient radarrClient,
        ISonarrClient sonarrClient,
        IWebhookService webhookService,
        ILogger<CleanupService> logger)
    {
        _options = options.Value;
        _jellyseerrClient = jellyseerrClient;
        _radarrClient = radarrClient;
        _sonarrClient = sonarrClient;
        _webhookService = webhookService;
        _logger = logger;
    }

    public Task<CleanupStatus> GetStatusAsync()
    {
        return Task.FromResult(_currentStatus);
    }

    public async Task<int> RunCleanupAsync(CleanupType type, bool dryRun = true)
    {
        if (_currentStatus.IsRunning)
        {
            _logger.LogWarning("Cleanup is already running");
            return 0;
        }

        _currentStatus = _currentStatus with { IsRunning = true };

        try
        {
            _logger.LogInformation("Starting {Type} cleanup (DryRun: {DryRun})", type, dryRun);
            await _webhookService.SendCleanupStartedAsync(type);

            var itemsToDelete = await GetItemsForCleanupAsync(type);
            
            if (!itemsToDelete.Any())
            {
                _logger.LogInformation("No items found for cleanup");
                return 0;
            }

            _logger.LogInformation("Found {Count} items for cleanup", itemsToDelete.Count);

            // Send webhook before deletion
            await _webhookService.SendWebhookAsync(
                WebhookEvent.MediaMarkedForDeletion, 
                itemsToDelete, 
                type);

            var deletedCount = 0;
            var spaceFreed = 0L;

            if (!dryRun)
            {
                foreach (var item in itemsToDelete)
                {
                    try
                    {
                        await DeleteMediaItemAsync(item, type);
                        deletedCount++;

                        // Calculate approximate space freed (simplified)
                        if (Directory.Exists(item.ParentPath))
                        {
                            var dirInfo = new DirectoryInfo(item.ParentPath);
                            spaceFreed += GetDirectorySize(dirInfo);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Failed to delete media item {Title}", item.Title);
                    }
                }

                // Send webhook after deletion
                if (deletedCount > 0)
                {
                    await _webhookService.SendWebhookAsync(
                        WebhookEvent.MediaDeleted, 
                        itemsToDelete.Take(deletedCount).ToList(), 
                        type);
                }
            }
            else
            {
                _logger.LogInformation("DRY RUN: Would delete {Count} items", itemsToDelete.Count);
                deletedCount = itemsToDelete.Count; // For dry run reporting
            }

            await _webhookService.SendCleanupCompletedAsync(type, deletedCount, spaceFreed);

            _currentStatus = _currentStatus with 
            { 
                LastRun = DateTime.UtcNow,
                ItemsDeleted = _currentStatus.ItemsDeleted + deletedCount,
                SpaceFreed = _currentStatus.SpaceFreed + spaceFreed
            };

            _logger.LogInformation("Cleanup completed. Deleted: {Count}, Space freed: {Space} bytes", 
                deletedCount, spaceFreed);

            return deletedCount;
        }
        finally
        {
            _currentStatus = _currentStatus with { IsRunning = false };
        }
    }

    public async Task<List<MediaItem>> GetItemsForCleanupAsync(CleanupType type)
    {
        return type switch
        {
            CleanupType.Movie => await GetMoviesForCleanupAsync(),
            CleanupType.Season => await GetSeasonsForCleanupAsync(),
            CleanupType.WeeklyEpisode => await GetEpisodesForCleanupAsync(),
            CleanupType.TagBased => await GetTagBasedItemsForCleanupAsync(),
            _ => new List<MediaItem>()
        };
    }

    private async Task<List<MediaItem>> GetMoviesForCleanupAsync()
    {
        if (!_options.MediaDeletion.Enabled)
            return new List<MediaItem>();

        var movies = await _radarrClient.GetMoviesAsync();
        var diskUsagePercent = GetDiskUsagePercent();
        var expirationTime = GetExpirationTime(_options.MediaDeletion.MovieExpiration, diskUsagePercent);
        
        if (expirationTime == null)
            return new List<MediaItem>(); // No deletion threshold reached

        var maxAge = ParseDuration(expirationTime);
        var cutoffDate = DateTime.UtcNow.AddDays(-maxAge);

        return movies
            .Where(m => m.Added < cutoffDate)
            .Where(m => ShouldIncludeByTags(m.Tags, _options.Radarr.TagsToInclude, _options.Radarr.TagsToExclude))
            .Select(m => new MediaItem
            {
                Id = m.Id,
                Title = m.Title,
                LibraryType = LibraryType.Movies,
                ImdbId = m.ImdbId,
                TmdbId = m.TmdbId,
                ParentPath = m.Path,
                OriginalPath = m.Path,
                ImportedDate = m.Added,
                Tags = m.Tags.Select(t => t.ToString()).ToList()
            })
            .ToList();
    }

    private async Task<List<MediaItem>> GetSeasonsForCleanupAsync()
    {
        if (!_options.MediaDeletion.Enabled)
            return new List<MediaItem>();

        var series = await _sonarrClient.GetSeriesAsync();
        var diskUsagePercent = GetDiskUsagePercent();
        var expirationTime = GetExpirationTime(_options.MediaDeletion.SeasonExpiration, diskUsagePercent);
        
        if (expirationTime == null)
            return new List<MediaItem>(); // No deletion threshold reached

        var maxAge = ParseDuration(expirationTime);
        var cutoffDate = DateTime.UtcNow.AddDays(-maxAge);

        return series
            .Where(s => s.Added < cutoffDate)
            .Where(s => ShouldIncludeByTags(s.Tags, _options.Sonarr.TagsToInclude, _options.Sonarr.TagsToExclude))
            .Select(s => new MediaItem
            {
                Id = s.Id,
                Title = s.Title,
                LibraryType = LibraryType.Shows,
                ImdbId = s.ImdbId,
                TmdbId = s.TvdbId,
                ParentPath = s.Path,
                OriginalPath = s.Path,
                ImportedDate = s.Added,
                Tags = s.Tags.Select(t => t.ToString()).ToList()
            })
            .ToList();
    }

    private async Task<List<MediaItem>> GetEpisodesForCleanupAsync()
    {
        if (!_options.EpisodeDeletion.Enabled)
            return new List<MediaItem>();

        var episodes = await _sonarrClient.GetEpisodesAsync();
        var maxAge = ParseDuration(_options.EpisodeDeletion.MaxAge);
        var cutoffDate = DateTime.UtcNow.AddDays(-maxAge);

        return episodes
            .Where(e => e.AirDate < cutoffDate)
            .Where(e => e.HasFile)
            .Select(e => new MediaItem
            {
                Id = e.Id,
                Title = e.Title,
                LibraryType = LibraryType.Episodes,
                ParentPath = "", // Would need to get from series
                OriginalPath = "", // Would need to get from episode file
                Season = e.SeasonNumber,
                ImportedDate = e.AirDate,
                Tags = new List<string>()
            })
            .ToList();
    }

    private async Task<List<MediaItem>> GetTagBasedItemsForCleanupAsync()
    {
        if (!_options.TagBasedDeletion.Enabled)
            return new List<MediaItem>();

        // Combine movies and shows based on tag criteria
        var movies = await GetMoviesForCleanupAsync();
        var shows = await GetSeasonsForCleanupAsync();

        return movies.Concat(shows).ToList();
    }

    private async Task DeleteMediaItemAsync(MediaItem item, CleanupType type)
    {
        _logger.LogInformation("Deleting {Type} item: {Title}", type, item.Title);

        switch (item.LibraryType)
        {
            case LibraryType.Movies:
                await _radarrClient.DeleteMovieAsync(item.Id, deleteFiles: true);
                break;
            case LibraryType.Shows:
                await _sonarrClient.DeleteSeriesAsync(item.Id, deleteFiles: true);
                break;
            case LibraryType.Episodes:
                // Would need episode-specific deletion logic
                break;
        }

        // Delete from filesystem if it still exists
        if (Directory.Exists(item.ParentPath))
        {
            Directory.Delete(item.ParentPath, recursive: true);
        }
    }

    private int GetDiskUsagePercent()
    {
        try
        {
            var driveInfo = new DriveInfo(_options.FileSystem.FreeSpaceCheckDir);
            var usedSpace = driveInfo.TotalSize - driveInfo.AvailableFreeSpace;
            var usagePercent = (int)((double)usedSpace / driveInfo.TotalSize * 100);
            return usagePercent;
        }
        catch
        {
            // If we can't determine disk usage, return a high value to trigger cleanup
            return 95;
        }
    }

    private string? GetExpirationTime(Dictionary<int, string> expirationMap, int diskUsagePercent)
    {
        // Find the highest threshold that the current disk usage exceeds
        var applicableThreshold = expirationMap.Keys
            .Where(threshold => diskUsagePercent >= threshold)
            .OrderByDescending(threshold => threshold)
            .FirstOrDefault();

        return applicableThreshold > 0 ? expirationMap[applicableThreshold] : null;
    }

    private double ParseDuration(string duration)
    {
        if (string.IsNullOrWhiteSpace(duration))
            return 30; // Default to 30 days

        // Simple parser for durations like "30d", "2w", "1M"
        var number = new string(duration.TakeWhile(char.IsDigit).ToArray());
        var unit = duration.Substring(number.Length);

        if (!double.TryParse(number, out var value))
            return 30;

        return unit.ToLowerInvariant() switch
        {
            "d" or "day" or "days" => value,
            "w" or "week" or "weeks" => value * 7,
            "m" or "month" or "months" => value * 30,
            "y" or "year" or "years" => value * 365,
            _ => value // Assume days if no unit
        };
    }

    private static bool ShouldIncludeByTags(IEnumerable<object> itemTags, List<string> includeTags, List<string> excludeTags)
    {
        var tags = itemTags.Select(t => t?.ToString()).Where(t => !string.IsNullOrWhiteSpace(t)).ToList();
        
        // If include tags are specified, item must have at least one
        if (includeTags.Any() && !tags.Any(tag => includeTags.Contains(tag!)))
            return false;

        // If exclude tags are specified, item must not have any
        if (excludeTags.Any() && tags.Any(tag => excludeTags.Contains(tag!)))
            return false;

        return true;
    }

    private static bool ShouldIncludeByTags(List<int> itemTags, List<int> tagsToInclude, List<int> tagsToExclude)
    {
        // If include tags are specified, item must have at least one
        if (tagsToInclude.Any() && !itemTags.Any(tag => tagsToInclude.Contains(tag)))
            return false;

        // If exclude tags are specified, item must not have any
        if (tagsToExclude.Any() && itemTags.Any(tag => tagsToExclude.Contains(tag)))
            return false;

        return true;
    }

    private static long GetDirectorySize(DirectoryInfo dirInfo)
    {
        try
        {
            return dirInfo.GetFiles("*", SearchOption.AllDirectories).Sum(file => file.Length);
        }
        catch
        {
            return 0;
        }
    }
}
