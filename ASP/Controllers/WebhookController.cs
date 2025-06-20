using JanitorAspNet.Configuration;
using JanitorAspNet.Models;
using JanitorAspNet.Webhooks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace JanitorAspNet.Controllers;

/// <summary>
/// Webhook management controller
/// Provides endpoints for managing and testing webhook configurations
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class WebhookController : ControllerBase
{
    private readonly IWebhookService _webhookService;
    private readonly ApplicationOptions _options;
    private readonly ILogger<WebhookController> _logger;

    public WebhookController(
        IWebhookService webhookService,
        IOptions<ApplicationOptions> options,
        ILogger<WebhookController> logger)
    {
        _webhookService = webhookService;
        _options = options.Value;
        _logger = logger;
    }

    /// <summary>
    /// Get all configured webhook endpoints with their status
    /// </summary>
    [HttpGet("endpoints")]
    public async Task<ActionResult<List<WebhookEndpointStatus>>> GetEndpoints()
    {
        try
        {
            var endpoints = await _webhookService.GetEndpointStatusAsync();
            return Ok(endpoints);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get webhook endpoints");
            return StatusCode(500, "Failed to get webhook endpoints");
        }
    }

    /// <summary>
    /// Test a specific webhook endpoint
    /// </summary>
    [HttpPost("endpoints/{endpointName}/test")]
    public async Task<ActionResult> TestEndpoint(string endpointName)
    {
        try
        {
            await _webhookService.TestEndpointAsync(endpointName);
            return Ok(new { message = $"Test webhook sent to {endpointName}" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to test webhook endpoint {EndpointName}", endpointName);
            return BadRequest(new { error = $"Failed to test webhook endpoint: {ex.Message}" });
        }
    }

    /// <summary>
    /// Send a health check to all enabled webhook endpoints
    /// </summary>
    [HttpPost("health-check")]
    public async Task<ActionResult> SendHealthCheck()
    {
        try
        {
            await _webhookService.SendHealthCheckAsync();
            return Ok(new { message = "Health check sent to all enabled webhooks" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send health check webhooks");
            return StatusCode(500, "Failed to send health check webhooks");
        }
    }

    /// <summary>
    /// Get webhook configuration details
    /// </summary>
    [HttpGet("config")]
    public ActionResult<WebhookConfigurationResponse> GetConfiguration()
    {
        var config = new WebhookConfigurationResponse
        {
            Enabled = _options.Webhooks.Enabled,
            RetryAttempts = _options.Webhooks.RetryAttempts,
            TimeoutSeconds = _options.Webhooks.TimeoutSeconds,
            EndpointCount = _options.Webhooks.Endpoints.Count,
            AvailableEvents = Enum.GetValues<WebhookEvent>().ToList(),
            AvailableCleanupTypes = Enum.GetValues<CleanupType>().ToList(),
            AvailableAuthTypes = Enum.GetValues<WebhookAuthType>().ToList(),
            Endpoints = _options.Webhooks.Endpoints.Select(e => new WebhookEndpointInfo
            {
                Name = e.Name,
                Url = e.Url,
                Enabled = e.Enabled,
                Events = e.Events,
                CleanupTypes = e.CleanupTypes,
                AuthType = e.AuthType,
                HasSecret = !string.IsNullOrEmpty(e.Secret),
                CustomHeaderCount = e.Headers.Count
            }).ToList()
        };

        return Ok(config);
    }

    /// <summary>
    /// Simulate webhook events for testing
    /// </summary>
    [HttpPost("simulate/{eventType}")]
    public async Task<ActionResult> SimulateEvent(WebhookEvent eventType, [FromBody] SimulateWebhookRequest? request = null)
    {
        try
        {
            var cleanupType = request?.CleanupType ?? CleanupType.Movie;
            var mockItems = CreateMockMediaItems(request?.ItemCount ?? 1);

            switch (eventType)
            {
                case WebhookEvent.MediaMarkedForDeletion:
                case WebhookEvent.MediaDeleted:
                    await _webhookService.SendWebhookAsync(eventType, mockItems, cleanupType);
                    break;
                case WebhookEvent.CleanupStarted:
                    await _webhookService.SendCleanupStartedAsync(cleanupType);
                    break;
                case WebhookEvent.CleanupCompleted:
                    await _webhookService.SendCleanupCompletedAsync(cleanupType, mockItems.Count, 1024 * 1024 * 500); // 500 MB
                    break;
                case WebhookEvent.HealthCheck:
                    await _webhookService.SendHealthCheckAsync();
                    break;
                default:
                    return BadRequest(new { error = "Unsupported event type for simulation" });
            }

            return Ok(new { message = $"Simulated {eventType} event sent to applicable webhooks" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to simulate webhook event {EventType}", eventType);
            return StatusCode(500, $"Failed to simulate webhook event: {ex.Message}");
        }
    }

    private List<MediaItem> CreateMockMediaItems(int count)
    {
        var items = new List<MediaItem>();
        var random = new Random();
        var movieTitles = new[] { "The Matrix", "Inception", "Interstellar", "The Dark Knight", "Pulp Fiction" };
        var showTitles = new[] { "Breaking Bad", "Game of Thrones", "The Office", "Friends", "Stranger Things" };

        for (int i = 0; i < count; i++)
        {
            var isMovie = random.Next(2) == 0;
            var titles = isMovie ? movieTitles : showTitles;

            items.Add(new MediaItem
            {
                Id = random.Next(1, 1000),
                Title = titles[random.Next(titles.Length)],
                LibraryType = isMovie ? LibraryType.Movies : LibraryType.Shows,
                ImdbId = $"tt{random.Next(1000000, 9999999)}",
                TmdbId = random.Next(1000, 99999),
                ParentPath = $"/data/media/{(isMovie ? "movies" : "tv")}/Test Movie {i}",
                OriginalPath = $"/data/media/{(isMovie ? "movies" : "tv")}/Test Movie {i}",
                ImportedDate = DateTime.UtcNow.AddDays(-random.Next(1, 365)),
                Tags = new List<string> { "test", "simulation" }
            });
        }

        return items;
    }
}

public record WebhookConfigurationResponse
{
    public bool Enabled { get; init; }
    public int RetryAttempts { get; init; }
    public int TimeoutSeconds { get; init; }
    public int EndpointCount { get; init; }
    public List<WebhookEvent> AvailableEvents { get; init; } = new();
    public List<CleanupType> AvailableCleanupTypes { get; init; } = new();
    public List<WebhookAuthType> AvailableAuthTypes { get; init; } = new();
    public List<WebhookEndpointInfo> Endpoints { get; init; } = new();
}

public record WebhookEndpointInfo
{
    public string Name { get; init; } = "";
    public string Url { get; init; } = "";
    public bool Enabled { get; init; }
    public List<WebhookEvent> Events { get; init; } = new();
    public List<CleanupType> CleanupTypes { get; init; } = new();
    public WebhookAuthType AuthType { get; init; }
    public bool HasSecret { get; init; }
    public int CustomHeaderCount { get; init; }
}

public record SimulateWebhookRequest
{
    public CleanupType CleanupType { get; init; } = CleanupType.Movie;
    public int ItemCount { get; init; } = 1;
}
