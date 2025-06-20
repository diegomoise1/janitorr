# Janitor ASP.NET Implementation Guide

This document provides comprehensive instructions for implementing all missing features to complete the ASP.NET Core transpilation of the Janitorr media management application.

## Current Implementation Status

### ✅ **Completed Components**
- Basic project structure and dependency injection setup
- Configuration models (`ApplicationOptions.cs`)
- Basic controllers (`StatusController`, `CleanupController`, `WebhookController`)
- Partial API client interfaces (`ApiClients.cs`)
- Webhook service foundation (`WebhookService.cs`)
- Basic models and DTOs (`MediaModels.cs`)
- Background service skeleton (`CleanupBackgroundService.cs`)
- Cleanup service interface (`CleanupService.cs`)

### ❌ **Missing Critical Features**

## 1. Complete API Client Implementations

### 1.1 Jellyfin/Emby Media Server Clients
**Files to create:** `Clients/MediaServerClients.cs`

```csharp
/// <summary>
/// Jellyfin API client for media server operations
/// Transpiled from JellyfinClient.kt
/// </summary>
public interface IJellyfinClient
{
    // Library Management
    [Get("/Library/VirtualFolders")]
    Task<List<VirtualFolder>> GetVirtualFoldersAsync();
    
    [Post("/Library/VirtualFolders")]
    Task<VirtualFolder> CreateVirtualFolderAsync([Body] CreateVirtualFolderRequest request);
    
    [Post("/Library/VirtualFolders/Paths")]
    Task AddPathToLibraryAsync([Body] AddPathRequest request);
    
    // Item Management
    [Get("/Items")]
    Task<ItemQueryResult> GetItemsAsync([Query] string? searchTerm = null, [Query] string? includeItemTypes = null);
    
    [Delete("/Items/{itemId}")]
    Task DeleteItemAsync(string itemId);
    
    [Get("/Items/{itemId}")]
    Task<BaseItemDto> GetItemAsync(string itemId);
    
    // Collection Management
    [Post("/Collections")]
    Task<CollectionCreationResult> CreateCollectionAsync([Body] CreateCollectionRequest request);
    
    [Post("/Collections/{collectionId}/Items")]
    Task AddToCollectionAsync(string collectionId, [Query] string ids);
    
    // Authentication
    [Post("/Users/AuthenticateByName")]
    Task<AuthenticationResult> AuthenticateAsync([Body] AuthenticateUserByName request);
}
```

### 1.2 Bazarr Subtitle Client
**Files to create:** `Clients/BazarrClient.cs`

```csharp
public interface IBazarrClient
{
    [Get("/api/movies")]
    Task<List<BazarrMovie>> GetMoviesAsync();
    
    [Get("/api/series")]
    Task<List<BazarrSeries>> GetSeriesAsync();
    
    [Get("/api/movies/{movieId}/subtitles")]
    Task<List<BazarrSubtitle>> GetMovieSubtitlesAsync(int movieId);
    
    [Get("/api/series/{seriesId}/subtitles")]
    Task<List<BazarrSubtitle>> GetSeriesSubtitlesAsync(int seriesId);
}
```

### 1.3 Complete Radarr/Sonarr Client Methods
**Update:** `Clients/ApiClients.cs`

Add missing endpoints:
- Quality profiles
- Root folders
- File management
- Series/Movie monitoring
- Episode file operations
- History with filtering
- Custom formats (Radarr)

## 2. Complete Service Layer Implementation

### 2.1 Full Cleanup Service Implementation
**Update:** `Services/CleanupService.cs`

Implement all missing methods:

```csharp
public class CleanupService : ICleanupService
{
    // Implement media age determination logic
    private async Task<List<LibraryItem>> GetMoviesForCleanupAsync(TimeSpan expiration)
    {
        // Get movies from Radarr
        // Filter by age based on history
        // Apply tag filters
        // Check file system seeding status
        // Apply exclusion rules
    }
    
    private async Task<List<LibraryItem>> GetSeasonsForCleanupAsync(TimeSpan expiration)
    {
        // Get series from Sonarr
        // Filter by season age
        // Handle whole show vs season logic
        // Apply seeding checks
    }
    
    // Implement disk space checking
    private double GetDiskSpacePercentage()
    {
        // Check free disk space percentage
        // Handle different mount points
    }
    
    // Implement file system operations
    private async Task DeleteMediaFilesAsync(List<LibraryItem> items)
    {
        // Delete from *arr services
        // Remove from media server
        // Clean up Jellyseerr requests
        // Handle file system cleanup
        // Update leaving soon collections
    }
}
```

### 2.2 Media Server Service Implementation
**Files to create:** `Services/MediaServerService.cs`

```csharp
public interface IMediaServerService
{
    Task CleanupMoviesAsync(List<LibraryItem> items);
    Task CleanupTvShowsAsync(List<LibraryItem> items);
    Task UpdateLeavingSoonAsync(CleanupType cleanupType, LibraryType libraryType, List<LibraryItem> items, bool onlyAddLinks = false);
    Task CreateLeavingSoonCollectionAsync(string collectionName, LibraryType libraryType);
    Task CreateSymbolicLinksAsync(List<LibraryItem> items, string targetPath);
}

public class JellyfinMediaServerService : IMediaServerService
{
    // Implement Jellyfin-specific operations
    // Collection management
    // Library operations
    // Item deletion
    // Symlink creation for "Leaving Soon"
}
```

### 2.3 Statistics Service Implementation
**Files to create:** `Services/StatsService.cs`

```csharp
public interface IStatsService
{
    Task PopulateWatchHistoryAsync(List<LibraryItem> items, LibraryType libraryType);
    Task<List<WatchHistoryItem>> GetWatchHistoryAsync(string itemId);
}

public class JellystatStatsService : IStatsService
{
    // Implement Jellystat watch history integration
    // Populate last watched dates
    // Handle whole show vs episode logic
}
```

### 2.4 Jellyseerr Service Implementation
**Files to create:** `Services/JellyseerrService.cs`

```csharp
public interface IJellyseerrService
{
    Task CleanupRequestsAsync(List<LibraryItem> items);
    Task<List<RequestResponse>> GetRequestsForItemsAsync(List<LibraryItem> items);
}

public class JellyseerrService : IJellyseerrService
{
    // Match library items to Jellyseerr requests
    // Clean up completed/outdated requests
    // Handle TMDB/IMDB ID matching
}
```

## 3. Background Services and Scheduling

### 3.1 Complete Background Service
**Update:** `Services/CleanupBackgroundService.cs`

```csharp
public class CleanupBackgroundService : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                // Media cleanup schedule (every hour)
                await RunMediaCleanupAsync();
                
                // Tag-based cleanup schedule (every hour)
                await RunTagBasedCleanupAsync();
                
                // Episode cleanup schedule (every hour)
                await RunEpisodeCleanupAsync();
                
                // Health check webhooks (daily)
                await SendHealthCheckWebhooksAsync();
                
                await Task.Delay(TimeSpan.FromHours(1), stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in cleanup background service");
                await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);
            }
        }
    }
    
    private async Task RunMediaCleanupAsync()
    {
        // Implement media cleanup logic based on disk space and age
        // Send webhooks for cleanup events
    }
    
    private async Task RunTagBasedCleanupAsync()
    {
        // Implement tag-based cleanup schedules
        // Handle multiple tag schedules with different expirations
    }
    
    private async Task RunEpisodeCleanupAsync()
    {
        // Implement weekly episode cleanup
        // Delete episodes from latest season based on count and age
    }
}
```

## 4. File System Operations

### 4.1 File System Service
**Files to create:** `Services/FileSystemService.cs`

```csharp
public interface IFileSystemService
{
    Task<bool> ValidateSeedingAsync(LibraryItem item);
    Task CreateSymbolicLinksAsync(List<LibraryItem> items, string targetDirectory, LibraryType libraryType);
    Task CleanupDirectoryAsync(string directory);
    double GetDiskSpacePercentage(string path);
    Task<long> GetDirectorySizeAsync(string path);
}

public class FileSystemService : IFileSystemService
{
    // Implement file system operations
    // Symlink creation for "Leaving Soon" collections
    // Seeding validation by checking file existence
    // Directory cleanup and management
    // Disk space calculations
}
```

## 5. Enhanced Configuration Support

### 5.1 Configuration Validation
**Update:** `Configuration/ApplicationOptions.cs`

Add validation attributes and implement configuration validation:

```csharp
public class ApplicationOptions : IValidatableObject
{
    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        // Validate URL formats
        // Ensure required API keys are present when services are enabled
        // Validate disk space percentages
        // Check webhook endpoint configurations
    }
}
```

### 5.2 Configuration Hot Reload
**Files to create:** `Services/ConfigurationService.cs`

```csharp
public interface IConfigurationService
{
    Task<bool> UpdateConfigurationAsync(ApplicationOptions newConfig);
    Task<ApplicationOptions> GetMaskedConfigurationAsync();
    event EventHandler<ApplicationOptions> ConfigurationChanged;
}
```

## 6. Enhanced Webhook System

### 6.1 Complete Webhook Implementation
**Update:** `Webhooks/WebhookService.cs`

```csharp
public class WebhookService : IWebhookService
{
    // Implement HMAC signature verification
    private string GenerateHmacSignature(string payload, string secret)
    {
        // Generate HMAC-SHA256 signature for webhook security
    }
    
    // Implement retry logic with exponential backoff
    private async Task SendWithRetryAsync(WebhookEndpoint endpoint, string payload, int maxAttempts)
    {
        // Retry failed webhooks with backoff
    }
    
    // Implement webhook endpoint health checking
    public async Task<List<WebhookEndpointStatus>> GetEndpointStatusAsync()
    {
        // Test each endpoint and return status
    }
    
    // Enhanced payload templates
    private WebhookPayload CreatePayload(WebhookEvent eventType, object data)
    {
        // Create structured payloads for different event types
    }
}
```

## 7. Enhanced Data Models

### 7.1 Complete Media Models
**Update:** `Models/MediaModels.cs`

Add missing model classes:

```csharp
// Jellyfin/Emby Models
public record VirtualFolder
{
    public string Name { get; init; } = "";
    public List<string> Locations { get; init; } = new();
    public string CollectionType { get; init; } = "";
}

// Bazarr Models
public record BazarrMovie
{
    public int RadarrId { get; init; }
    public string Title { get; init; } = "";
    public List<BazarrSubtitle> Subtitles { get; init; } = new();
}

// Statistics Models
public record WatchHistoryItem
{
    public string ItemId { get; init; } = "";
    public DateTime LastWatched { get; init; }
    public double PlaybackPositionTicks { get; init; }
    public string UserId { get; init; } = "";
}

// Enhanced LibraryItem
public record LibraryItem
{
    // Add all missing properties from Kotlin version
    public bool Seeding { get; init; }
    public DateTime HistoryAge { get; init; }
    public DateTime? LastSeen { get; init; }
    public List<string> ExtraFiles { get; init; } = new();
    public int? Season { get; init; }
    public string RootFolderPath { get; init; } = "";
    public string LibraryPath { get; init; } = "";
}
```

## 8. Error Handling and Logging

### 8.1 Structured Logging
**Update:** All service classes

Implement comprehensive logging:

```csharp
// Use structured logging with semantic properties
_logger.LogInformation("Starting cleanup for {CleanupType} with {ItemCount} items", 
    cleanupType, items.Count);

_logger.LogWarning("Failed to delete {ItemPath} due to seeding status", item.FilePath);

_logger.LogError(ex, "API call failed for {ServiceName} at {Url}", 
    serviceName, endpoint);
```

### 8.2 Global Exception Handling
**Files to create:** `Middleware/ExceptionHandlingMiddleware.cs`

```csharp
public class ExceptionHandlingMiddleware
{
    // Implement global exception handling
    // Log exceptions with context
    // Return appropriate HTTP status codes
    // Mask sensitive information in responses
}
```

## 9. Health Checks and Monitoring

### 9.1 Health Check Implementation
**Files to create:** `HealthChecks/ServiceHealthChecks.cs`

```csharp
public class RadarrHealthCheck : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        // Test Radarr API connectivity
        // Return health status with details
    }
}

// Similar implementations for:
// - SonarrHealthCheck
// - JellyfinHealthCheck
// - JellyseerrHealthCheck
// - FileSystemHealthCheck
```

## 10. Testing Infrastructure

### 10.1 Unit Tests
**Files to create:** `Tests/` directory structure

```
Tests/
├── Controllers/
│   ├── CleanupControllerTests.cs
│   ├── StatusControllerTests.cs
│   └── WebhookControllerTests.cs
├── Services/
│   ├── CleanupServiceTests.cs
│   ├── MediaServerServiceTests.cs
│   └── WebhookServiceTests.cs
└── Integration/
    ├── ApiIntegrationTests.cs
    └── BackgroundServiceTests.cs
```

### 10.2 Mock Data and Test Fixtures
Create comprehensive test data that matches the Kotlin test structures.

## 11. Docker and Deployment

### 11.1 Dockerfile Updates
**Update:** `Dockerfile`

```dockerfile
# Multi-stage build for optimized image size
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 5114
EXPOSE 7041

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["JanitorAspNet.csproj", "."]
RUN dotnet restore "./JanitorAspNet.csproj"
COPY . .
WORKDIR "/src/."
RUN dotnet build "JanitorAspNet.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "JanitorAspNet.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "JanitorAspNet.dll"]
```

## 12. Performance Optimizations

### 12.1 Caching Implementation
**Files to create:** `Services/CacheService.cs`

```csharp
public interface ICacheService
{
    Task<T> GetOrSetAsync<T>(string key, Func<Task<T>> factory, TimeSpan expiration);
    Task RemoveAsync(string key);
    Task RemoveByPatternAsync(string pattern);
}

// Implement caching for:
// - API responses from *arr services
// - Media server library data
// - File system information
// - Webhook endpoint status
```

### 12.2 Background Processing
Implement queue-based processing for long-running operations:

```csharp
public interface IBackgroundTaskQueue
{
    Task QueueBackgroundWorkItemAsync(Func<CancellationToken, Task> workItem);
    Task<Func<CancellationToken, Task>> DequeueAsync(CancellationToken cancellationToken);
}
```

## 13. Security Enhancements

### 13.1 API Key Management
- Implement secure storage for API keys
- Add API key rotation support
- Mask sensitive data in logs and responses

### 13.2 Authentication and Authorization
- Add optional API authentication
- Implement role-based access control
- Secure webhook endpoints

## 14. Documentation and OpenAPI

### 14.1 Complete API Documentation
Update Swagger documentation with:
- Comprehensive endpoint descriptions
- Request/response examples
- Error code documentation
- Authentication requirements

### 14.2 Configuration Documentation
Create comprehensive configuration guide matching the Kotlin version.

## Implementation Priority

### Phase 1 (Critical)
1. Complete API client implementations
2. Full cleanup service logic
3. Background service scheduling
4. File system operations

### Phase 2 (Important)
1. Media server service implementation
2. Enhanced webhook system
3. Statistics service integration
4. Configuration validation

### Phase 3 (Enhancement)
1. Health checks and monitoring
2. Performance optimizations
3. Comprehensive testing
4. Security enhancements

## Testing Strategy

For each implemented feature:
1. Create unit tests with mock dependencies
2. Integration tests with real API endpoints (when available)
3. End-to-end tests for complete workflows
4. Performance tests for cleanup operations
5. Error scenario testing

## Migration Notes

When implementing features, ensure:
- Maintain exact compatibility with Kotlin configuration format
- Use same default values and behaviors
- Preserve all existing webhook payload structures
- Match logging patterns and levels
- Maintain API endpoint compatibility

This implementation guide provides the roadmap for completing the full-featured ASP.NET Core version of Janitorr that matches all capabilities of the original Kotlin/Spring Boot application.
