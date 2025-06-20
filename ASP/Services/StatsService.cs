using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using JanitorAspNet.Configuration;
using JanitorAspNet.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;

namespace JanitorAspNet.Services
{
    public interface IStatsService
    {
        Task PopulateWatchHistoryAsync(List<LibraryItem> items, LibraryType libraryType);
        Task<List<WatchHistoryItem>> GetWatchHistoryAsync(string itemId);
        Task<bool> IsJellystatEnabledAsync();
    }

    public class JellystatStatsService : IStatsService
    {
        private readonly ILogger<JellystatStatsService> _logger;
        private readonly HttpClient _httpClient;
        private readonly ApplicationOptions _options;

        public JellystatStatsService(
            ILogger<JellystatStatsService> logger,
            HttpClient httpClient,
            IOptions<ApplicationOptions> options)
        {
            _logger = logger;
            _httpClient = httpClient;
            _options = options.Value;
        }

        public Task<bool> IsJellystatEnabledAsync()
        {
            var isEnabled = !string.IsNullOrEmpty(_options.Jellystat?.Url) && 
                           !string.IsNullOrEmpty(_options.Jellystat?.ApiKey);
            return Task.FromResult(isEnabled);
        }

        public async Task PopulateWatchHistoryAsync(List<LibraryItem> items, LibraryType libraryType)
        {
            if (!await IsJellystatEnabledAsync())
            {
                _logger.LogDebug("Jellystat not configured, skipping watch history population");
                return;
            }

            _logger.LogInformation("Populating watch history for {Count} {LibraryType} items", items.Count, libraryType);

            foreach (var item in items)
            {
                try
                {
                    var watchHistory = await GetWatchHistoryAsync(item.Id);
                    if (watchHistory.Any())
                    {
                        var lastWatched = watchHistory.OrderByDescending(w => w.LastWatched).First();
                        
                        // Update the item with the last watched information
                        // Since LibraryItem is a record, we'd need to create a new instance
                        // or use a mutable approach. For now, we'll log the information.
                        _logger.LogDebug("Item {Title} last watched: {LastWatched}", 
                            item.Title, lastWatched.LastWatched);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to get watch history for item: {Title}", item.Title);
                }
            }
        }

        public async Task<List<WatchHistoryItem>> GetWatchHistoryAsync(string itemId)
        {
            if (!await IsJellystatEnabledAsync())
            {
                return new List<WatchHistoryItem>();
            }

            try
            {
                var baseUrl = _options.Jellystat.Url.TrimEnd('/');
                var apiKey = _options.Jellystat.ApiKey;
                
                // Set up HTTP client with API key
                _httpClient.DefaultRequestHeaders.Clear();
                _httpClient.DefaultRequestHeaders.Add("X-API-Token", apiKey);

                // Query Jellystat for watch history
                // This is a simplified example - actual Jellystat API might differ
                var response = await _httpClient.GetAsync($"{baseUrl}/api/stats/plays?item_id={itemId}");

                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var jellystatPlays = JsonConvert.DeserializeObject<List<JellystatPlay>>(content);

                    return jellystatPlays?.Select(play => new WatchHistoryItem
                    {
                        ItemId = play.ItemId,
                        LastWatched = play.LastPlayed,
                        PlaybackPositionTicks = play.PlaybackPositionTicks,
                        UserId = play.UserId
                    }).ToList() ?? new List<WatchHistoryItem>();
                }
                else
                {
                    _logger.LogWarning("Failed to get watch history from Jellystat. Status: {StatusCode}", response.StatusCode);
                    return new List<WatchHistoryItem>();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting watch history for item: {ItemId}", itemId);
                return new List<WatchHistoryItem>();
            }
        }
    }

    // Internal model for Jellystat API response
    internal record JellystatPlay
    {
        public string ItemId { get; init; } = "";
        public DateTime LastPlayed { get; init; }
        public double PlaybackPositionTicks { get; init; }
        public string UserId { get; init; } = "";
        public string UserName { get; init; } = "";
        public int PlayCount { get; init; }
    }
}
