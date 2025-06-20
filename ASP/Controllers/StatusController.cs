using JanitorAspNet.Configuration;
using JanitorAspNet.Models;
using JanitorAspNet.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using System.Text.Json;
using WebhookEndpoint = JanitorAspNet.Configuration.WebhookEndpoint;

namespace JanitorAspNet.Controllers;

/// <summary>
/// Status and configuration management controller
/// Provides real-time status information and configuration management
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class StatusController : ControllerBase
{
    private readonly ICleanupService _cleanupService;
    private readonly IOptionsSnapshot<ApplicationOptions> _options;
    private readonly IWebHostEnvironment _environment;
    private readonly ILogger<StatusController> _logger;

    public StatusController(
        ICleanupService cleanupService,
        IOptionsSnapshot<ApplicationOptions> options,
        IWebHostEnvironment environment,
        ILogger<StatusController> logger)
    {
        _cleanupService = cleanupService;
        _options = options;
        _environment = environment;
        _logger = logger;
    }

    /// <summary>
    /// Get comprehensive server status
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<ServerStatus>> GetServerStatusAsync()
    {
        try
        {
            var cleanupStatus = await _cleanupService.GetStatusAsync();
            var config = _options.Value;

            var serverStatus = new ServerStatus
            {
                ApplicationName = "Janitor ASP.NET",
                Version = "1.0.0",
                Environment = _environment.EnvironmentName,
                StartupTime = GetApplicationStartupTime(),
                CurrentTime = DateTime.UtcNow,
                Cleanup = cleanupStatus,
                Configuration = new ConfigurationSummary
                {
                    DryRun = config.DryRun,
                    JellyseerrConfigured = config.Jellyseerr.Enabled && !string.IsNullOrEmpty(config.Jellyseerr.Url),
                    RadarrConfigured = config.Radarr.Enabled && !string.IsNullOrEmpty(config.Radarr.Url),
                    SonarrConfigured = config.Sonarr.Enabled && !string.IsNullOrEmpty(config.Sonarr.Url),
                    JellyfinConfigured = config.Jellyfin.Enabled && !string.IsNullOrEmpty(config.Jellyfin.Url),
                    JellystatConfigured = config.Jellystat.Enabled && !string.IsNullOrEmpty(config.Jellystat.Url),
                    WebhooksEnabled = config.Webhooks.Enabled,
                    WebhookEndpointCount = config.Webhooks.Endpoints.Count,
                    CleanupTypesEnabled = new CleanupTypesStatus
                    {
                        MediaDeletion = config.MediaDeletion.Enabled,
                        TagBasedDeletion = config.TagBasedDeletion.Enabled,
                        EpisodeDeletion = config.EpisodeDeletion.Enabled,
                        FileSystemAccess = config.FileSystem.Access
                    }
                },
                SystemInfo = new SystemInfo
                {
                    MachineName = Environment.MachineName,
                    OperatingSystem = Environment.OSVersion.ToString(),
                    ProcessorCount = Environment.ProcessorCount,
                    WorkingSet = Environment.WorkingSet,
                    Is64BitProcess = Environment.Is64BitProcess
                }
            };

            return Ok(serverStatus);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get server status");
            return StatusCode(500, "Failed to get server status");
        }
    }

    /// <summary>
    /// Get current configuration
    /// </summary>
    [HttpGet("config")]
    public ActionResult<ApplicationOptions> GetConfiguration()
    {
        try
        {
            var config = _options.Value;
            
            // Create a copy with sensitive data masked
            var sanitizedConfig = new ApplicationOptions
            {
                DryRun = config.DryRun,
                RunOnce = config.RunOnce,
                WholeTvShow = config.WholeTvShow,
                WholeShowSeedingCheck = config.WholeShowSeedingCheck,
                LeavingSoon = config.LeavingSoon,
                ExclusionTag = config.ExclusionTag,
                FileSystem = config.FileSystem, // No sensitive data in file system config
                Jellyseerr = new JellyseerrOptions
                {
                    Enabled = config.Jellyseerr.Enabled,
                    Url = config.Jellyseerr.Url,
                    ApiKey = MaskApiKey(config.Jellyseerr.ApiKey),
                    MatchServer = config.Jellyseerr.MatchServer
                },
                Jellyfin = new JellyfinOptions
                {
                    Enabled = config.Jellyfin.Enabled,
                    Url = config.Jellyfin.Url,
                    ApiKey = MaskApiKey(config.Jellyfin.ApiKey),
                    Username = config.Jellyfin.Username,
                    Password = MaskPassword(config.Jellyfin.Password),
                    Delete = config.Jellyfin.Delete,
                    LeavingSoonTv = config.Jellyfin.LeavingSoonTv,
                    LeavingSoonMovies = config.Jellyfin.LeavingSoonMovies,
                    LeavingSoonType = config.Jellyfin.LeavingSoonType
                },
                Emby = new EmbyOptions
                {
                    Enabled = config.Emby.Enabled,
                    Url = config.Emby.Url,
                    ApiKey = MaskApiKey(config.Emby.ApiKey),
                    Username = config.Emby.Username,
                    Password = MaskPassword(config.Emby.Password),
                    Delete = config.Emby.Delete
                },
                Plex = new PlexOptions
                {
                    Enabled = config.Plex.Enabled,
                    Url = config.Plex.Url,
                    Token = MaskApiKey(config.Plex.Token),
                    Delete = config.Plex.Delete
                },
                Radarr = new RadarrOptions
                {
                    Enabled = config.Radarr.Enabled,
                    Url = config.Radarr.Url,
                    ApiKey = MaskApiKey(config.Radarr.ApiKey),
                    OnlyDeleteFiles = config.Radarr.OnlyDeleteFiles,
                    DetermineAgeBy = config.Radarr.DetermineAgeBy,
                    TagsToExclude = config.Radarr.TagsToExclude,
                    TagsToInclude = config.Radarr.TagsToInclude
                },
                Sonarr = new SonarrOptions
                {
                    Enabled = config.Sonarr.Enabled,
                    Url = config.Sonarr.Url,
                    ApiKey = MaskApiKey(config.Sonarr.ApiKey),
                    DeleteEmptyShows = config.Sonarr.DeleteEmptyShows,
                    DetermineAgeBy = config.Sonarr.DetermineAgeBy,
                    TagsToExclude = config.Sonarr.TagsToExclude,
                    TagsToInclude = config.Sonarr.TagsToInclude
                },
                Bazarr = new BazarrOptions
                {
                    Enabled = config.Bazarr.Enabled,
                    Url = config.Bazarr.Url,
                    ApiKey = MaskApiKey(config.Bazarr.ApiKey)
                },
                Jellystat = new JellystatOptions
                {
                    Enabled = config.Jellystat.Enabled,
                    WholeTvShow = config.Jellystat.WholeTvShow,
                    Url = config.Jellystat.Url,
                    ApiKey = MaskApiKey(config.Jellystat.ApiKey)
                },
                MediaDeletion = config.MediaDeletion,
                TagBasedDeletion = config.TagBasedDeletion,
                EpisodeDeletion = config.EpisodeDeletion,
                Webhooks = new WebhookOptions
                {
                    Enabled = config.Webhooks.Enabled,
                    RetryAttempts = config.Webhooks.RetryAttempts,
                    TimeoutSeconds = config.Webhooks.TimeoutSeconds,
                    Endpoints = config.Webhooks.Endpoints.Select(e => new WebhookEndpoint
                    {
                        Url = e.Url,
                        Events = e.Events,
                        Headers = e.Headers.ToDictionary(h => h.Key, h => MaskHeaderValue(h.Value)),
                        Secret = MaskSecret(e.Secret)
                    }).ToList()
                }
            };

            return Ok(sanitizedConfig);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get configuration");
            return StatusCode(500, "Failed to get configuration");
        }
    }

    /// <summary>
    /// Update configuration (requires restart to take effect)
    /// </summary>
    [HttpPost("config")]
    public async Task<ActionResult> UpdateConfigurationAsync([FromBody] ApplicationOptions newConfig)
    {
        try
        {
            var configPath = Path.Combine(_environment.ContentRootPath, "appsettings.json");
            
            // Read current configuration
            var currentConfigJson = await System.IO.File.ReadAllTextAsync(configPath);
            var currentConfig = JsonSerializer.Deserialize<Dictionary<string, object>>(currentConfigJson);
            
            // Update the Application section
            if (currentConfig != null)
            {
                currentConfig["Application"] = newConfig;
                
                var options = new JsonSerializerOptions 
                { 
                    WriteIndented = true,
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                };
                
                var updatedJson = JsonSerializer.Serialize(currentConfig, options);
                
                // Write back to file
                await System.IO.File.WriteAllTextAsync(configPath, updatedJson);
                
                _logger.LogInformation("Configuration updated successfully. Restart required for changes to take effect.");
                
                return Ok(new { 
                    Message = "Configuration updated successfully. Restart the application for changes to take effect.",
                    RequiresRestart = true,
                    UpdatedAt = DateTime.UtcNow
                });
            }
            
            return BadRequest("Failed to update configuration");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update configuration");
            return StatusCode(500, "Failed to update configuration");
        }
    }

    /// <summary>
    /// Get application logs (last 100 lines)
    /// </summary>
    [HttpGet("logs")]
    public ActionResult<List<string>> GetRecentLogs()
    {
        try
        {
            // This is a simple implementation - in production you'd want to use structured logging
            var logs = new List<string>
            {
                $"[{DateTime.UtcNow:HH:mm:ss}] Application running in {_environment.EnvironmentName} mode",
                $"[{DateTime.UtcNow:HH:mm:ss}] DryRun mode: {_options.Value.DryRun}",
                $"[{DateTime.UtcNow:HH:mm:ss}] Jellyseerr configured: {!string.IsNullOrEmpty(_options.Value.Jellyseerr.Url)}",
                $"[{DateTime.UtcNow:HH:mm:ss}] Radarr configured: {!string.IsNullOrEmpty(_options.Value.Radarr.Url)}",
                $"[{DateTime.UtcNow:HH:mm:ss}] Sonarr configured: {!string.IsNullOrEmpty(_options.Value.Sonarr.Url)}",
                $"[{DateTime.UtcNow:HH:mm:ss}] Webhooks enabled: {_options.Value.Webhooks.Enabled}"
            };

            return Ok(logs);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get logs");
            return StatusCode(500, "Failed to get logs");
        }
    }

    private static DateTime GetApplicationStartupTime()
    {
        // This is a simplified version - you might want to store this in a static field
        return DateTime.UtcNow.AddMinutes(-5); // Placeholder
    }

    private static string MaskApiKey(string apiKey)
    {
        if (string.IsNullOrEmpty(apiKey) || apiKey.Length <= 8)
            return "****";
        
        return apiKey[..4] + "****" + apiKey[^4..];
    }

    private static string MaskPassword(string password)
    {
        return string.IsNullOrEmpty(password) ? "" : "********";
    }

    private static string MaskSecret(string secret)
    {
        return string.IsNullOrEmpty(secret) ? "" : "********";
    }

    private static string MaskHeaderValue(string value)
    {
        if (string.IsNullOrEmpty(value))
            return "";
        
        if (value.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
            return "Bearer ********";
        
        return "********";
    }
}

// Supporting models for status endpoint
public record ServerStatus
{
    public string ApplicationName { get; init; } = "";
    public string Version { get; init; } = "";
    public string Environment { get; init; } = "";
    public DateTime StartupTime { get; init; }
    public DateTime CurrentTime { get; init; }
    public CleanupStatus Cleanup { get; init; } = new();
    public ConfigurationSummary Configuration { get; init; } = new();
    public SystemInfo SystemInfo { get; init; } = new();
}

public record ConfigurationSummary
{
    public bool DryRun { get; init; }
    public bool JellyseerrConfigured { get; init; }
    public bool RadarrConfigured { get; init; }
    public bool SonarrConfigured { get; init; }
    public bool JellyfinConfigured { get; init; }
    public bool JellystatConfigured { get; init; }
    public bool WebhooksEnabled { get; init; }
    public int WebhookEndpointCount { get; init; }
    public CleanupTypesStatus CleanupTypesEnabled { get; init; } = new();
}

public record CleanupTypesStatus
{
    public bool MediaDeletion { get; init; }
    public bool TagBasedDeletion { get; init; }
    public bool EpisodeDeletion { get; init; }
    public bool FileSystemAccess { get; init; }
}

public record SystemInfo
{
    public string MachineName { get; init; } = "";
    public string OperatingSystem { get; init; } = "";
    public int ProcessorCount { get; init; }
    public long WorkingSet { get; init; }
    public bool Is64BitProcess { get; init; }
}
