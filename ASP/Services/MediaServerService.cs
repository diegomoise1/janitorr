using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using JanitorAspNet.Clients;
using JanitorAspNet.Configuration;
using JanitorAspNet.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace JanitorAspNet.Services
{
    public interface IMediaServerService
    {
        Task CleanupMoviesAsync(List<LibraryItem> items);
        Task CleanupTvShowsAsync(List<LibraryItem> items);
        Task UpdateLeavingSoonAsync(CleanupType cleanupType, LibraryType libraryType, List<LibraryItem> items, bool onlyAddLinks = false);
        Task CreateLeavingSoonCollectionAsync(string collectionName, LibraryType libraryType);
        Task CreateSymbolicLinksAsync(List<LibraryItem> items, string targetPath);
        Task<bool> IsMediaServerEnabledAsync();
    }

    public class JellyfinMediaServerService : IMediaServerService
    {
        private readonly ILogger<JellyfinMediaServerService> _logger;
        private readonly IJellyfinClient _jellyfinClient;
        private readonly IFileSystemService _fileSystemService;
        private readonly ApplicationOptions _options;
        private string? _accessToken;

        public JellyfinMediaServerService(
            ILogger<JellyfinMediaServerService> logger,
            IJellyfinClient jellyfinClient,
            IFileSystemService fileSystemService,
            IOptions<ApplicationOptions> options)
        {
            _logger = logger;
            _jellyfinClient = jellyfinClient;
            _fileSystemService = fileSystemService;
            _options = options.Value;
        }

        public Task<bool> IsMediaServerEnabledAsync()
        {
            var hasApiKey = !string.IsNullOrEmpty(_options.Jellyfin?.Url) && 
                           !string.IsNullOrEmpty(_options.Jellyfin?.ApiKey);
            
            var hasUserCredentials = !string.IsNullOrEmpty(_options.Jellyfin?.Url) && 
                                   !string.IsNullOrEmpty(_options.Jellyfin?.Username);
            
            return Task.FromResult(hasApiKey || hasUserCredentials);
        }

        public async Task CleanupMoviesAsync(List<LibraryItem> items)
        {
            if (!await IsMediaServerEnabledAsync())
            {
                _logger.LogDebug("Jellyfin not configured, skipping movie cleanup");
                return;
            }

            _logger.LogInformation("Cleaning up {Count} movies from Jellyfin", items.Count);

            await EnsureAuthenticatedAsync();

            foreach (var item in items)
            {
                try
                {
                    // Find the item in Jellyfin by IMDB/TMDB ID or title
                    var jellyfinItem = await FindJellyfinItemAsync(item);
                    if (jellyfinItem != null)
                    {
                        await _jellyfinClient.DeleteItemAsync(jellyfinItem.Id);
                        _logger.LogInformation("Deleted movie {Title} from Jellyfin", item.Title);
                    }
                    else
                    {
                        _logger.LogWarning("Could not find movie {Title} in Jellyfin", item.Title);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to delete movie {Title} from Jellyfin", item.Title);
                }
            }

            // Refresh library after cleanup
            await _jellyfinClient.RefreshLibraryAsync();
        }

        public async Task CleanupTvShowsAsync(List<LibraryItem> items)
        {
            if (!await IsMediaServerEnabledAsync())
            {
                _logger.LogDebug("Jellyfin not configured, skipping TV show cleanup");
                return;
            }

            _logger.LogInformation("Cleaning up {Count} TV shows from Jellyfin", items.Count);

            await EnsureAuthenticatedAsync();

            foreach (var item in items)
            {
                try
                {
                    var jellyfinItem = await FindJellyfinItemAsync(item);
                    if (jellyfinItem != null)
                    {
                        await _jellyfinClient.DeleteItemAsync(jellyfinItem.Id);
                        _logger.LogInformation("Deleted TV show {Title} from Jellyfin", item.Title);
                    }
                    else
                    {
                        _logger.LogWarning("Could not find TV show {Title} in Jellyfin", item.Title);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to delete TV show {Title} from Jellyfin", item.Title);
                }
            }

            // Refresh library after cleanup
            await _jellyfinClient.RefreshLibraryAsync();
        }

        public async Task UpdateLeavingSoonAsync(CleanupType cleanupType, LibraryType libraryType, List<LibraryItem> items, bool onlyAddLinks = false)
        {
            if (!await IsMediaServerEnabledAsync())
            {
                _logger.LogDebug("Jellyfin not configured, skipping leaving soon update");
                return;
            }

            var collectionName = $"Leaving Soon - {libraryType}";
            
            if (!onlyAddLinks)
            {
                await CreateLeavingSoonCollectionAsync(collectionName, libraryType);
            }

            // Create symbolic links if configured
            var leavingSoonPath = _options.FileSystem?.LeavingSoonDir;
            if (!string.IsNullOrEmpty(leavingSoonPath))
            {
                var targetPath = Path.Combine(leavingSoonPath, libraryType.ToString());
                await _fileSystemService.CreateSymbolicLinksAsync(items, targetPath, libraryType);
            }

            // Add items to the Jellyfin collection
            await AddItemsToCollectionAsync(collectionName, items);
        }

        public async Task CreateLeavingSoonCollectionAsync(string collectionName, LibraryType libraryType)
        {
            if (!await IsMediaServerEnabledAsync())
            {
                return;
            }

            try
            {
                await EnsureAuthenticatedAsync();

                // Check if collection already exists
                var existingItems = await _jellyfinClient.GetItemsAsync(
                    searchTerm: collectionName, 
                    includeItemTypes: "BoxSet");

                if (existingItems.Items.Any(i => i.Name.Equals(collectionName, StringComparison.OrdinalIgnoreCase)))
                {
                    _logger.LogInformation("Collection {CollectionName} already exists", collectionName);
                    return;
                }

                // Create new collection
                var createRequest = new CreateCollectionRequest
                {
                    Name = collectionName,
                    IsLocked = false
                };

                var result = await _jellyfinClient.CreateCollectionAsync(createRequest);
                _logger.LogInformation("Created collection {CollectionName} with ID {CollectionId}", 
                    collectionName, result.Id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to create collection {CollectionName}", collectionName);
            }
        }

        public async Task CreateSymbolicLinksAsync(List<LibraryItem> items, string targetPath)
        {
            // Determine the library type from the first item if available
            var libraryType = items.FirstOrDefault()?.Type ?? LibraryType.Movie;
            await _fileSystemService.CreateSymbolicLinksAsync(items, targetPath, libraryType);
        }

        private async Task EnsureAuthenticatedAsync()
        {
            if (string.IsNullOrEmpty(_accessToken))
            {
                try
                {
                    // If API key is available, use it directly
                    if (!string.IsNullOrEmpty(_options.Jellyfin?.ApiKey))
                    {
                        _accessToken = _options.Jellyfin.ApiKey;
                        _logger.LogInformation("Using API key for Jellyfin authentication");
                        return;
                    }

                    // Otherwise, authenticate with username/password
                    var authRequest = new AuthenticateUserByName
                    {
                        Username = _options.Jellyfin?.Username ?? "admin",
                        Pw = _options.Jellyfin?.Password ?? ""
                    };

                    var authResult = await _jellyfinClient.AuthenticateAsync(authRequest);
                    _accessToken = authResult.AccessToken;
                    
                    _logger.LogInformation("Successfully authenticated with Jellyfin using credentials");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to authenticate with Jellyfin");
                    throw;
                }
            }
        }

        private async Task<BaseItemDto?> FindJellyfinItemAsync(LibraryItem item)
        {
            try
            {
                // Try to find by external IDs first
                var searchResults = await _jellyfinClient.GetItemsAsync(
                    includeItemTypes: item.Type == LibraryType.Movie ? "Movie" : "Series");

                // Match by IMDB ID
                if (!string.IsNullOrEmpty(item.ImdbId))
                {
                    var matchByImdb = searchResults.Items.FirstOrDefault(i => 
                        i.ImdbId?.Equals(item.ImdbId, StringComparison.OrdinalIgnoreCase) == true);
                    if (matchByImdb != null) return matchByImdb;
                }

                // Match by TMDB ID
                if (item.TmdbId.HasValue)
                {
                    var matchByTmdb = searchResults.Items.FirstOrDefault(i => 
                        i.TmdbId == item.TmdbId.Value);
                    if (matchByTmdb != null) return matchByTmdb;
                }

                // Match by TVDB ID (for TV shows)
                if (item.TvdbId.HasValue)
                {
                    var matchByTvdb = searchResults.Items.FirstOrDefault(i => 
                        i.TvdbId == item.TvdbId.Value);
                    if (matchByTvdb != null) return matchByTvdb;
                }

                // Fallback: search by title
                var titleSearchResults = await _jellyfinClient.GetItemsAsync(
                    searchTerm: item.Title,
                    includeItemTypes: item.Type == LibraryType.Movie ? "Movie" : "Series");

                return titleSearchResults.Items.FirstOrDefault();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error finding Jellyfin item for {Title}", item.Title);
                return null;
            }
        }

        private async Task AddItemsToCollectionAsync(string collectionName, List<LibraryItem> items)
        {
            try
            {
                // Find the collection
                var collections = await _jellyfinClient.GetItemsAsync(
                    searchTerm: collectionName,
                    includeItemTypes: "BoxSet");

                var collection = collections.Items.FirstOrDefault(c => 
                    c.Name.Equals(collectionName, StringComparison.OrdinalIgnoreCase));

                if (collection == null)
                {
                    _logger.LogWarning("Collection {CollectionName} not found", collectionName);
                    return;
                }

                // Find Jellyfin items for our library items
                var jellyfinItemIds = new List<string>();
                foreach (var item in items)
                {
                    var jellyfinItem = await FindJellyfinItemAsync(item);
                    if (jellyfinItem != null)
                    {
                        jellyfinItemIds.Add(jellyfinItem.Id);
                    }
                }

                if (jellyfinItemIds.Any())
                {
                    var itemIds = string.Join(",", jellyfinItemIds);
                    await _jellyfinClient.AddToCollectionAsync(collection.Id, itemIds);
                    
                    _logger.LogInformation("Added {Count} items to collection {CollectionName}", 
                        jellyfinItemIds.Count, collectionName);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to add items to collection {CollectionName}", collectionName);
            }
        }
    }
}
