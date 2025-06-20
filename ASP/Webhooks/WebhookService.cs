using JanitorAspNet.Configuration;
using JanitorAspNet.Models;
using Microsoft.Extensions.Options;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using WebhookEvent = JanitorAspNet.Configuration.WebhookEvent;
using WebhookEndpoint = JanitorAspNet.Configuration.WebhookEndpoint;

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

    // Add missing webhook methods for background service integration
    Task SendCleanupWebhookAsync(CleanupResult result);
    Task SendTagBasedCleanupWebhookAsync(int itemCount);
    Task SendErrorWebhookAsync(string errorType, string message);
    Task SendHealthCheckWebhookAsync(object healthData);
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
            Event = eventType.ToString(),
            Timestamp = DateTime.UtcNow,
            CleanupType = cleanupType,
            Items = items
        };

        await SendToEndpointsAsync(payload, eventType);
    }

    public async Task SendCleanupStartedAsync(CleanupType cleanupType)
    {
        if (!_options.Enabled)
            return;

        var payload = new WebhookPayload
        {
            Event = WebhookEvent.CleanupStarted.ToString(),
            Timestamp = DateTime.UtcNow,
            CleanupType = cleanupType,
            Items = new List<MediaItem>()
        };

        await SendToEndpointsAsync(payload, WebhookEvent.CleanupStarted);
    }

    public async Task SendCleanupCompletedAsync(CleanupType cleanupType, int itemsDeleted, long spaceFreed)
    {
        if (!_options.Enabled)
            return;

        var payload = new WebhookPayload
        {
            Event = WebhookEvent.CleanupCompleted.ToString(),
            Timestamp = DateTime.UtcNow,
            CleanupType = cleanupType,
            Items = new List<MediaItem>(),
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
            Event = WebhookEvent.HealthCheck.ToString(),
            Timestamp = DateTime.UtcNow,
            CleanupType = null,
            Items = new List<MediaItem>()
        };

        await SendToEndpointsAsync(payload, WebhookEvent.HealthCheck);
    }

    public Task<List<WebhookEndpointStatus>> GetEndpointStatusAsync()
    {
        var statuses = new List<WebhookEndpointStatus>();

        foreach (var endpoint in _options.Endpoints)
        {
            var status = new WebhookEndpointStatus
            {
                Name = endpoint.Name,
                Url = endpoint.Url,
                Enabled = endpoint.Enabled,
                Events = endpoint.Events.Select(e => e.ToString()).ToList(),
                CleanupTypes = endpoint.CleanupTypes,
                LastTestResult = "Unknown"
            };

            statuses.Add(status);
        }

        return Task.FromResult(statuses);
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
            Event = WebhookEvent.HealthCheck.ToString(),
            Timestamp = DateTime.UtcNow,
            CleanupType = null,
            Items = new List<MediaItem>(),
            TestMessage = "This is a test webhook from Janitor"
        };

        await SendToEndpointAsync(endpoint, testPayload);
    }

    // Add missing webhook methods for background service integration
    public async Task SendCleanupWebhookAsync(CleanupResult result)
    {
        if (!_options.Enabled)
            return;

        var payload = new
        {
            Event = "cleanup_completed",
            Timestamp = DateTime.UtcNow,
            CleanupType = result.CleanupType.ToString(),
            Duration = result.Duration.TotalMinutes,
            ItemsFound = result.ItemsFound,
            ItemsDeleted = result.ItemsDeleted,
            Success = result.Success,
            Error = result.Error
        };

        await SendToAllEndpointsAsync("cleanup_completed", payload);
    }

    public async Task SendTagBasedCleanupWebhookAsync(int itemCount)
    {
        if (!_options.Enabled)
            return;

        var payload = new
        {
            Event = "tag_based_cleanup",
            Timestamp = DateTime.UtcNow,
            ItemsProcessed = itemCount
        };

        await SendToAllEndpointsAsync("tag_based_cleanup", payload);
    }

    public async Task SendErrorWebhookAsync(string errorType, string message)
    {
        if (!_options.Enabled)
            return;

        var payload = new
        {
            Event = "error",
            Timestamp = DateTime.UtcNow,
            ErrorType = errorType,
            Message = message
        };

        await SendToAllEndpointsAsync("error", payload);
    }

    public async Task SendHealthCheckWebhookAsync(object healthData)
    {
        if (!_options.Enabled)
            return;

        await SendToAllEndpointsAsync("health_check", healthData);
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

            // Add HMAC signature if secret is provided
            if (!string.IsNullOrEmpty(endpoint.Secret))
            {
                var signature = GenerateHmacSignature(json, endpoint.Secret);
                payload = payload with { Signature = signature };
                json = JsonSerializer.Serialize(payload, new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                });
            }

            await SendWithRetryAsync(endpoint, json, 3);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send webhook to {Name} ({Url})", endpoint.Name, endpoint.Url);
        }
    }

    private string GenerateHmacSignature(string payload, string secret)
    {
        using var hmac = new System.Security.Cryptography.HMACSHA256(System.Text.Encoding.UTF8.GetBytes(secret));
        var hash = hmac.ComputeHash(System.Text.Encoding.UTF8.GetBytes(payload));
        return Convert.ToBase64String(hash);
    }

    private async Task SendWithRetryAsync(WebhookEndpoint endpoint, string payload, int maxAttempts)
    {
        int attempt = 0;
        while (attempt < maxAttempts)
        {
            try
            {
                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(_options.TimeoutSeconds));
                using var response = await _httpClient.PostAsync(endpoint.Url, new StringContent(payload, Encoding.UTF8, "application/json"), cts.Token);

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
                    attempt + 1, endpoint.Name, endpoint.Url);
            }

            await Task.Delay(TimeSpan.FromSeconds(Math.Pow(2, attempt)));
            attempt++;
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

    private async Task SendToAllEndpointsAsync(string eventType, object payload)
    {
        var endpoints = GetEnabledEndpointsForEvent(eventType);
        
        foreach (var endpoint in endpoints)
        {
            try
            {
                await SendWithRetryAsync(endpoint, JsonSerializer.Serialize(payload), 3);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send webhook to {Url}", endpoint.Url);
            }
        }
    }

    private List<WebhookEndpoint> GetEnabledEndpointsForEvent(string eventType)
    {
        if (Enum.TryParse<WebhookEvent>(eventType, out var webhookEvent))
        {
            return _options.Endpoints
                .Where(e => e.Enabled && e.Events.Contains(webhookEvent))
                .ToList();
        }
        
        return new List<WebhookEndpoint>();
    }
}
