using JanitorAspNet.Configuration;
using JanitorAspNet.Models;
using Microsoft.Extensions.Options;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace JanitorAspNet.Webhooks;

/// <summary>
/// Webhook service for sending notifications
/// Transpiled from WebhookService.kt with enhanced multi-endpoint support
/// </summary>
public interface IWebhookService
{
    Task SendWebhookAsync(WebhookEvent eventType, List<MediaItem> items, CleanupType cleanupType);
    Task SendCleanupStartedAsync(CleanupType cleanupType);
    Task SendCleanupCompletedAsync(CleanupType cleanupType, int itemsDeleted, long spaceFreed);
    Task SendHealthCheckAsync();
    Task<List<WebhookEndpointStatus>> GetEndpointStatusAsync();
    Task TestEndpointAsync(string endpointName);
}

public class WebhookService : IWebhookService
{
    private readonly WebhookOptions _options;
    private readonly HttpClient _httpClient;
    private readonly ILogger<WebhookService> _logger;

    public WebhookService(
        IOptions<WebhookOptions> options,
        HttpClient httpClient,
        ILogger<WebhookService> logger)
    {
        _options = options.Value;
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task SendWebhookAsync(WebhookEvent eventType, List<MediaItem> items, CleanupType cleanupType)
    {
        if (!_options.Enabled || !items.Any())
            return;

        var payload = new WebhookPayload
        {
            Event = eventType,
            Timestamp = DateTime.UtcNow,
            CleanupType = cleanupType,
            Items = items.Select(MapToWebhookMediaItem).ToList()
        };

        await SendToEndpointsAsync(payload, eventType);
    }

    public async Task SendCleanupStartedAsync(CleanupType cleanupType)
    {
        if (!_options.Enabled)
            return;

        var payload = new WebhookPayload
        {
            Event = WebhookEvent.CleanupStarted,
            Timestamp = DateTime.UtcNow,
            CleanupType = cleanupType,
            Items = new List<WebhookMediaItem>()
        };

        await SendToEndpointsAsync(payload, WebhookEvent.CleanupStarted);
    }

    public async Task SendCleanupCompletedAsync(CleanupType cleanupType, int itemsDeleted, long spaceFreed)
    {
        if (!_options.Enabled)
            return;

        var payload = new WebhookPayload
        {
            Event = WebhookEvent.CleanupCompleted,
            Timestamp = DateTime.UtcNow,
            CleanupType = cleanupType,
            Items = new List<WebhookMediaItem>(),
            ItemsDeleted = itemsDeleted,
            SpaceFreed = spaceFreed
        };

        await SendToEndpointsAsync(payload, WebhookEvent.CleanupCompleted);
    }

    public async Task SendHealthCheckAsync()
    {
        if (!_options.Enabled)
            return;

        var payload = new WebhookPayload
        {
            Event = WebhookEvent.HealthCheck,
            Timestamp = DateTime.UtcNow,
            CleanupType = null,
            Items = new List<WebhookMediaItem>()
        };

        await SendToEndpointsAsync(payload, WebhookEvent.HealthCheck);
    }

    public async Task<List<WebhookEndpointStatus>> GetEndpointStatusAsync()
    {
        var statuses = new List<WebhookEndpointStatus>();

        foreach (var endpoint in _options.Endpoints)
        {
            var status = new WebhookEndpointStatus
            {
                Name = endpoint.Name,
                Url = endpoint.Url,
                Enabled = endpoint.Enabled,
                Events = endpoint.Events,
                CleanupTypes = endpoint.CleanupTypes,
                LastTestResult = "Unknown"
            };

            statuses.Add(status);
        }

        return statuses;
    }

    public async Task TestEndpointAsync(string endpointName)
    {
        var endpoint = _options.Endpoints.FirstOrDefault(e => e.Name == endpointName);
        if (endpoint == null)
        {
            _logger.LogWarning("Endpoint {Name} not found", endpointName);
            return;
        }

        var testPayload = new WebhookPayload
        {
            Event = WebhookEvent.HealthCheck,
            Timestamp = DateTime.UtcNow,
            CleanupType = null,
            Items = new List<WebhookMediaItem>(),
            TestMessage = "This is a test webhook from Janitor"
        };

        await SendToEndpointAsync(endpoint, testPayload);
    }

    private async Task SendToEndpointsAsync(WebhookPayload payload, WebhookEvent eventType)
    {
        var applicableEndpoints = _options.Endpoints
            .Where(endpoint => endpoint.Enabled)
            .Where(endpoint => endpoint.Events.Contains(eventType))
            .Where(endpoint => payload.CleanupType == null || !endpoint.CleanupTypes.Any() || endpoint.CleanupTypes.Contains(payload.CleanupType.Value));

        var tasks = applicableEndpoints.Select(endpoint => SendToEndpointAsync(endpoint, payload));

        await Task.WhenAll(tasks);
    }

    private async Task SendToEndpointAsync(WebhookEndpoint endpoint, WebhookPayload payload)
    {
        try
        {
            _logger.LogDebug("Sending webhook to {Name} ({Url}) for event {Event}", 
                endpoint.Name, endpoint.Url, payload.Event);

            var json = JsonSerializer.Serialize(payload, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });

            using var request = new HttpRequestMessage(HttpMethod.Post, endpoint.Url)
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json")
            };

            // Add authentication
            AddAuthenticationToRequest(request, endpoint);

            // Add custom headers
            foreach (var header in endpoint.Headers)
            {
                request.Headers.Add(header.Key, header.Value);
            }

            // Add HMAC signature if secret is provided
            if (!string.IsNullOrEmpty(endpoint.Secret))
            {
                var signature = GenerateHmacSignature(json, endpoint.Secret);
                request.Headers.Add("X-Janitor-Signature", signature);
            }

            // Add identifying headers
            request.Headers.Add("User-Agent", "Janitor-ASP/1.0");
            request.Headers.Add("X-Janitor-Event", payload.Event.ToString());
            if (payload.CleanupType.HasValue)
            {
                request.Headers.Add("X-Janitor-Cleanup-Type", payload.CleanupType.Value.ToString());
            }

            await SendWithRetryAsync(request, endpoint);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send webhook to {Name} ({Url})", endpoint.Name, endpoint.Url);
        }
    }

    private async Task SendWithRetryAsync(HttpRequestMessage request, WebhookEndpoint endpoint)
    {
        var retryCount = 0;
        var delays = new[] { 1000, 2000, 4000 }; // Exponential backoff

        while (retryCount <= _options.RetryAttempts)
        {
            try
            {
                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(_options.TimeoutSeconds));
                using var response = await _httpClient.SendAsync(request, cts.Token);

                if (response.IsSuccessStatusCode)
                {
                    _logger.LogInformation("Webhook sent successfully to {Name} ({Url})", endpoint.Name, endpoint.Url);
                    return;
                }

                _logger.LogWarning("Webhook failed with status {StatusCode} to {Name} ({Url})", 
                    response.StatusCode, endpoint.Name, endpoint.Url);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Webhook attempt {Attempt} failed to {Name} ({Url})", 
                    retryCount + 1, endpoint.Name, endpoint.Url);
            }

            if (retryCount < _options.RetryAttempts)
            {
                await Task.Delay(delays[Math.Min(retryCount, delays.Length - 1)]);
                retryCount++;
            }
            else
            {
                break;
            }
        }

        _logger.LogError("All webhook attempts failed to {Name} ({Url})", endpoint.Name, endpoint.Url);
    }

    private void AddAuthenticationToRequest(HttpRequestMessage request, WebhookEndpoint endpoint)
    {
        switch (endpoint.AuthType)
        {
            case WebhookAuthType.Bearer:
                if (!string.IsNullOrEmpty(endpoint.AuthToken))
                {
                    request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", endpoint.AuthToken);
                }
                break;

            case WebhookAuthType.Basic:
                if (!string.IsNullOrEmpty(endpoint.AuthUsername) && !string.IsNullOrEmpty(endpoint.AuthPassword))
                {
                    var credentials = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{endpoint.AuthUsername}:{endpoint.AuthPassword}"));
                    request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", credentials);
                }
                break;

            case WebhookAuthType.ApiKey:
                if (!string.IsNullOrEmpty(endpoint.AuthToken))
                {
                    request.Headers.Add("X-API-Key", endpoint.AuthToken);
                }
                break;

            case WebhookAuthType.None:
            default:
                // No authentication
                break;
        }
    }

    private static string GenerateHmacSignature(string payload, string secret)
    {
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secret));
        var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(payload));
        return Convert.ToHexString(hash).ToLowerInvariant();
    }

    private static WebhookMediaItem MapToWebhookMediaItem(MediaItem item)
    {
        return new WebhookMediaItem
        {
            Id = item.Id,
            Title = item.Title,
            LibraryType = item.LibraryType,
            ImdbId = item.ImdbId,
            TmdbId = item.TmdbId,
            ParentPath = item.ParentPath,
            OriginalPath = item.OriginalPath,
            Season = item.Season,
            Tags = item.Tags,
            ImportedDate = item.ImportedDate,
            LastSeen = item.LastSeen,
            HistoryAge = item.HistoryAge,
            Seeding = item.Seeding
        };
    }
}

public record WebhookPayload
{
    public WebhookEvent Event { get; init; }
    public DateTime Timestamp { get; init; }
    public CleanupType? CleanupType { get; init; }
    public List<WebhookMediaItem> Items { get; init; } = new();
    public int? ItemsDeleted { get; init; }
    public long? SpaceFreed { get; init; }
    public string? TestMessage { get; init; }
}

public record WebhookEndpointStatus
{
    public string Name { get; init; } = "";
    public string Url { get; init; } = "";
    public bool Enabled { get; init; }
    public List<WebhookEvent> Events { get; init; } = new();
    public List<CleanupType> CleanupTypes { get; init; } = new();
    public string LastTestResult { get; init; } = "";
    public DateTime? LastTestTime { get; init; }
}

public record WebhookMediaItem
{
    public int Id { get; init; }
    public string Title { get; init; } = "";
    public LibraryType LibraryType { get; init; }
    public string? ImdbId { get; init; }
    public int? TmdbId { get; init; }
    public string ParentPath { get; init; } = "";
    public string OriginalPath { get; init; } = "";
    public int? Season { get; init; }
    public List<string> Tags { get; init; } = new();
    public DateTime? ImportedDate { get; init; }
    public DateTime? LastSeen { get; init; }
    public DateTime? HistoryAge { get; init; }
    public bool Seeding { get; init; }
}
