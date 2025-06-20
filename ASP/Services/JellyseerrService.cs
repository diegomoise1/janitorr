using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using JanitorAspNet.Clients;
using JanitorAspNet.Configuration;
using JanitorAspNet.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace JanitorAspNet.Services
{
    public interface IJellyseerrService
    {
        Task CleanupRequestsAsync(List<LibraryItem> items);
        Task<List<RequestResponse>> GetRequestsForItemsAsync(List<LibraryItem> items);
        Task<bool> IsJellyseerrEnabledAsync();
    }

    public class JellyseerrService : IJellyseerrService
    {
        private readonly ILogger<JellyseerrService> _logger;
        private readonly IJellyseerrClient _jellyseerrClient;
        private readonly ApplicationOptions _options;

        public JellyseerrService(
            ILogger<JellyseerrService> logger,
            IJellyseerrClient jellyseerrClient,
            IOptions<ApplicationOptions> options)
        {
            _logger = logger;
            _jellyseerrClient = jellyseerrClient;
            _options = options.Value;
        }

        public Task<bool> IsJellyseerrEnabledAsync()
        {
            var isEnabled = !string.IsNullOrEmpty(_options.Jellyseerr?.Url) && 
                           !string.IsNullOrEmpty(_options.Jellyseerr?.ApiKey);
            return Task.FromResult(isEnabled);
        }

        public async Task CleanupRequestsAsync(List<LibraryItem> items)
        {
            if (!await IsJellyseerrEnabledAsync())
            {
                _logger.LogDebug("Jellyseerr not configured, skipping request cleanup");
                return;
            }

            _logger.LogInformation("Cleaning up Jellyseerr requests for {Count} items", items.Count);

            try
            {
                var requests = await GetRequestsForItemsAsync(items);
                
                foreach (var request in requests)
                {
                    try
                    {
                        // Only delete completed requests for items being cleaned up
                        if (request.Status.Equals("available", StringComparison.OrdinalIgnoreCase) ||
                            request.Status.Equals("completed", StringComparison.OrdinalIgnoreCase))
                        {
                            await _jellyseerrClient.DeleteRequestAsync(request.Id);
                            _logger.LogInformation("Deleted Jellyseerr request {RequestId} for {Title}", 
                                request.Id, request.Media.Title);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Failed to delete Jellyseerr request {RequestId}", request.Id);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during Jellyseerr request cleanup");
            }
        }

        public async Task<List<RequestResponse>> GetRequestsForItemsAsync(List<LibraryItem> items)
        {
            if (!await IsJellyseerrEnabledAsync())
            {
                return new List<RequestResponse>();
            }

            var matchingRequests = new List<RequestResponse>();

            try
            {
                // Get all requests from Jellyseerr (paginated)
                var allRequests = new List<RequestResponse>();
                var pageSize = 100;
                var offset = 0;
                bool hasMorePages = true;

                while (hasMorePages)
                {
                    var page = await _jellyseerrClient.GetRequestsAsync(pageSize, offset);
                    if (page?.Results?.Any() == true)
                    {
                        allRequests.AddRange(page.Results);
                        offset += pageSize;
                        hasMorePages = page.Results.Count == pageSize;
                    }
                    else
                    {
                        hasMorePages = false;
                    }
                }

                // Match requests to library items by TMDB/IMDB ID
                foreach (var item in items)
                {
                    var matchedRequests = allRequests.Where(request =>
                        MatchesItem(request.Media, item)).ToList();

                    matchingRequests.AddRange(matchedRequests);
                }

                _logger.LogInformation("Found {Count} matching Jellyseerr requests for {ItemCount} items", 
                    matchingRequests.Count, items.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting Jellyseerr requests");
            }

            return matchingRequests;
        }

        private static bool MatchesItem(MediaInfo mediaInfo, LibraryItem item)
        {
            // Match by TMDB ID
            if (mediaInfo.TmdbId.HasValue && item.TmdbId.HasValue && 
                mediaInfo.TmdbId.Value == item.TmdbId.Value)
            {
                return true;
            }

            // Match by IMDB ID
            if (!string.IsNullOrEmpty(mediaInfo.ImdbId) && !string.IsNullOrEmpty(item.ImdbId) &&
                mediaInfo.ImdbId.Equals(item.ImdbId, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            // Match by TVDB ID (for TV shows)
            if (mediaInfo.TvdbId.HasValue && item.TvdbId.HasValue && 
                mediaInfo.TvdbId.Value == item.TvdbId.Value)
            {
                return true;
            }

            // Fallback: fuzzy title matching
            if (!string.IsNullOrEmpty(mediaInfo.Title) && !string.IsNullOrEmpty(item.Title))
            {
                var normalizedMediaTitle = NormalizeTitle(mediaInfo.Title);
                var normalizedItemTitle = NormalizeTitle(item.Title);
                
                if (normalizedMediaTitle.Equals(normalizedItemTitle, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }

        private static string NormalizeTitle(string title)
        {
            if (string.IsNullOrEmpty(title))
                return "";

            // Remove common words and characters that might differ between sources
            var normalized = title
                .Replace("The ", "", StringComparison.OrdinalIgnoreCase)
                .Replace("A ", "", StringComparison.OrdinalIgnoreCase)
                .Replace("An ", "", StringComparison.OrdinalIgnoreCase)
                .Replace(":", "")
                .Replace("-", "")
                .Replace("'", "")
                .Replace("\"", "")
                .Replace("(", "")
                .Replace(")", "")
                .Replace(" ", "");

            return normalized;
        }
    }
}
