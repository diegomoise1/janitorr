using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using JanitorAspNet.Clients;
using JanitorAspNet.Configuration;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace JanitorAspNet.HealthChecks
{
    public class RadarrHealthCheck : IHealthCheck
    {
        private readonly IRadarrClient _radarrClient;
        private readonly ApplicationOptions _options;
        private readonly ILogger<RadarrHealthCheck> _logger;

        public RadarrHealthCheck(
            IRadarrClient radarrClient,
            IOptions<ApplicationOptions> options,
            ILogger<RadarrHealthCheck> logger)
        {
            _radarrClient = radarrClient;
            _options = options.Value;
            _logger = logger;
        }

        public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
        {
            try
            {
                if (string.IsNullOrEmpty(_options.Radarr?.Url))
                {
                    return HealthCheckResult.Healthy("Radarr not configured");
                }

                var diskSpaces = await _radarrClient.GetDiskSpaceAsync();
                return HealthCheckResult.Healthy($"Radarr is healthy. Found {diskSpaces.Count} disk spaces.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Radarr health check failed");
                return HealthCheckResult.Unhealthy("Radarr health check failed", ex);
            }
        }
    }

    public class SonarrHealthCheck : IHealthCheck
    {
        private readonly ISonarrClient _sonarrClient;
        private readonly ApplicationOptions _options;
        private readonly ILogger<SonarrHealthCheck> _logger;

        public SonarrHealthCheck(
            ISonarrClient sonarrClient,
            IOptions<ApplicationOptions> options,
            ILogger<SonarrHealthCheck> logger)
        {
            _sonarrClient = sonarrClient;
            _options = options.Value;
            _logger = logger;
        }

        public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
        {
            try
            {
                if (string.IsNullOrEmpty(_options.Sonarr?.Url))
                {
                    return HealthCheckResult.Healthy("Sonarr not configured");
                }

                var diskSpaces = await _sonarrClient.GetDiskSpaceAsync();
                return HealthCheckResult.Healthy($"Sonarr is healthy. Found {diskSpaces.Count} disk spaces.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Sonarr health check failed");
                return HealthCheckResult.Unhealthy("Sonarr health check failed", ex);
            }
        }
    }

    public class JellyfinHealthCheck : IHealthCheck
    {
        private readonly IJellyfinClient _jellyfinClient;
        private readonly ApplicationOptions _options;
        private readonly ILogger<JellyfinHealthCheck> _logger;

        public JellyfinHealthCheck(
            IJellyfinClient jellyfinClient,
            IOptions<ApplicationOptions> options,
            ILogger<JellyfinHealthCheck> logger)
        {
            _jellyfinClient = jellyfinClient;
            _options = options.Value;
            _logger = logger;
        }

        public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
        {
            try
            {
                if (string.IsNullOrEmpty(_options.Jellyfin?.Url))
                {
                    return HealthCheckResult.Healthy("Jellyfin not configured");
                }

                var virtualFolders = await _jellyfinClient.GetVirtualFoldersAsync();
                return HealthCheckResult.Healthy($"Jellyfin is healthy. Found {virtualFolders.Count} virtual folders.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Jellyfin health check failed");
                return HealthCheckResult.Unhealthy("Jellyfin health check failed", ex);
            }
        }
    }

    public class JellyseerrHealthCheck : IHealthCheck
    {
        private readonly IJellyseerrClient _jellyseerrClient;
        private readonly ApplicationOptions _options;
        private readonly ILogger<JellyseerrHealthCheck> _logger;

        public JellyseerrHealthCheck(
            IJellyseerrClient jellyseerrClient,
            IOptions<ApplicationOptions> options,
            ILogger<JellyseerrHealthCheck> logger)
        {
            _jellyseerrClient = jellyseerrClient;
            _options = options.Value;
            _logger = logger;
        }

        public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
        {
            try
            {
                if (string.IsNullOrEmpty(_options.Jellyseerr?.Url))
                {
                    return HealthCheckResult.Healthy("Jellyseerr not configured");
                }

                var requestCount = await _jellyseerrClient.GetRequestCountAsync();
                return HealthCheckResult.Healthy($"Jellyseerr is healthy. Found {requestCount} requests.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Jellyseerr health check failed");
                return HealthCheckResult.Unhealthy("Jellyseerr health check failed", ex);
            }
        }
    }

    public class FileSystemHealthCheck : IHealthCheck
    {
        private readonly ILogger<FileSystemHealthCheck> _logger;
        private readonly ApplicationOptions _options;

        public FileSystemHealthCheck(
            ILogger<FileSystemHealthCheck> logger,
            IOptions<ApplicationOptions> options)
        {
            _logger = logger;
            _options = options.Value;
        }

        public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
        {
            try
            {
                var data = new
                {
                    TotalPaths = 0,
                    AccessiblePaths = 0,
                    InaccessiblePaths = 0
                };

                // Check if configured paths are accessible
                var pathsToCheck = new List<string>();
                
                if (!string.IsNullOrEmpty(_options.Radarr?.Url))
                    pathsToCheck.Add("/"); // Default root path
                
                if (!string.IsNullOrEmpty(_options.Sonarr?.Url))
                    pathsToCheck.Add("/"); // Default root path

                var accessibleCount = 0;
                foreach (var path in pathsToCheck)
                {
                    try
                    {
                        if (System.IO.Directory.Exists(path))
                        {
                            accessibleCount++;
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Cannot access path: {Path}", path);
                    }
                }

                var dataDictionary = new Dictionary<string, object>
                {
                    ["TotalPaths"] = pathsToCheck.Count,
                    ["AccessiblePaths"] = accessibleCount,
                    ["InaccessiblePaths"] = pathsToCheck.Count - accessibleCount
                };

                await Task.CompletedTask;

                if (pathsToCheck.Count - accessibleCount > 0)
                {
                    return HealthCheckResult.Degraded("Some file system paths are inaccessible", null, dataDictionary);
                }

                return HealthCheckResult.Healthy("File system is accessible", dataDictionary);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "File system health check failed");
                return HealthCheckResult.Unhealthy("File system health check failed", ex);
            }
        }
    }

    public class WebhookHealthCheck : IHealthCheck
    {
        private readonly HttpClient _httpClient;
        private readonly ApplicationOptions _options;
        private readonly ILogger<WebhookHealthCheck> _logger;

        public WebhookHealthCheck(
            HttpClient httpClient,
            IOptions<ApplicationOptions> options,
            ILogger<WebhookHealthCheck> logger)
        {
            _httpClient = httpClient;
            _options = options.Value;
            _logger = logger;
        }

        public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
        {
            try
            {
                if (!_options.Webhooks.Enabled || !_options.Webhooks.Endpoints.Any())
                {
                    return HealthCheckResult.Healthy("Webhooks not configured");
                }

                var healthyEndpoints = 0;
                var totalEndpoints = _options.Webhooks.Endpoints.Count;

                foreach (var endpoint in _options.Webhooks.Endpoints.Where(e => e.Enabled))
                {
                    try
                    {
                        // Simple HEAD request to check if endpoint is reachable
                        using var request = new HttpRequestMessage(HttpMethod.Head, endpoint.Url);
                        using var response = await _httpClient.SendAsync(request, cancellationToken);
                        
                        if (response.IsSuccessStatusCode || response.StatusCode == System.Net.HttpStatusCode.MethodNotAllowed)
                        {
                            healthyEndpoints++;
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Webhook endpoint {Url} is not reachable", endpoint.Url);
                    }
                }

                var dataDictionary = new Dictionary<string, object>
                {
                    ["TotalEndpoints"] = totalEndpoints,
                    ["HealthyEndpoints"] = healthyEndpoints,
                    ["UnhealthyEndpoints"] = totalEndpoints - healthyEndpoints
                };

                if (healthyEndpoints == 0 && totalEndpoints > 0)
                {
                    return HealthCheckResult.Unhealthy("All webhook endpoints are unreachable", null, dataDictionary);
                }

                if (healthyEndpoints < totalEndpoints)
                {
                    return HealthCheckResult.Degraded("Some webhook endpoints are unreachable", null, dataDictionary);
                }

                return HealthCheckResult.Healthy("All webhook endpoints are reachable", dataDictionary);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Webhook health check failed");
                return HealthCheckResult.Unhealthy("Webhook health check failed", ex);
            }
        }
    }
}
