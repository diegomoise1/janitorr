# Janitor ASP.NET

A C# ASP.NET Core transpilation of the Janitorr Kotlin/Spring Boot application for automated media cleanup and management.

## Overview

This project transpiles the core functionality of Janitorr from Kotlin/Spring Boot to C# ASP.NET Core, providing:

- **Media Management APIs**: Integration with Jellyseerr, Radarr, Sonarr, and media servers
- **Automated Cleanup**: Scheduled cleanup tasks for movies, TV shows, and episodes
- **Webhook Notifications**: Configurable webhooks for cleanup events
- **REST API**: Web API endpoints for manual cleanup operations and status monitoring

## Features

### Core Functionality
- Movie cleanup based on age and tags
- Season cleanup for TV shows
- Weekly episode cleanup
- Tag-based cleanup operations
- File system space management

### API Integrations
- **Jellyseerr**: Request management and approval workflow
- **Radarr**: Movie library management
- **Sonarr**: TV show and episode management
- **Media Servers**: Jellyfin/Emby/Plex integration

### Webhooks
- Pre-deletion notifications
- Post-deletion confirmations
- Cleanup start/completion events
- HMAC signature verification
- Retry logic with exponential backoff

## Quick Start

### Prerequisites
- .NET 8.0 SDK
- Access to Jellyseerr, Radarr, Sonarr instances
- Media server (Jellyfin/Emby/Plex)

### Configuration

Update `appsettings.json` with your service URLs and API keys:

```json
{
  "Application": {
    "DryRun": true,
    "Jellyseerr": {
      "Url": "http://jellyseerr:5055",
      "ApiKey": "your-jellyseerr-api-key"
    },
    "Radarr": {
      "Url": "http://radarr:7878",
      "ApiKey": "your-radarr-api-key"
    },
    "Sonarr": {
      "Url": "http://sonarr:8989",
      "ApiKey": "your-sonarr-api-key"
    },
    "Cleanup": {
      "EnableMovieCleanup": true,
      "MovieDeletionDelayDays": 7
    }
  }
}
```

### Running the Application

```bash
# Development
dotnet run

# Production
dotnet publish -c Release
```

The API will be available at `https://localhost:7041` (HTTPS) or `http://localhost:5114` (HTTP).

## API Endpoints

### Status Dashboard
- **`GET /`** - Redirects to the status dashboard
- **`GET /dashboard`** - Redirects to the status dashboard
- **`GET /index.html`** - Status dashboard UI

### Status Management
- **`GET /api/status`** - Get comprehensive server status
- **`GET /api/status/config`** - Get current configuration (with sensitive data masked)
- **`POST /api/status/config`** - Update configuration (requires restart)
- **`GET /api/status/logs`** - Get recent application logs

### Cleanup Operations
- `GET /api/cleanup/status` - Get current cleanup status
- `POST /api/cleanup/run` - Trigger manual cleanup
- `GET /api/cleanup/preview/{type}` - Preview items for cleanup
- `POST /api/cleanup/movies` - Run movie cleanup
- `POST /api/cleanup/seasons` - Run season cleanup
- `POST /api/cleanup/episodes` - Run episode cleanup
- `POST /api/cleanup/tag-based` - Run tag-based cleanup

## Status Dashboard

The application includes a built-in web dashboard that provides:

### 📊 **Real-time Status Monitoring**
- System status and uptime
- Cleanup operation status and history
- Configuration summary
- System resource information

### ⚙️ **Configuration Management**
- View current configuration with sensitive data masked
- Update configuration through web interface
- Configuration changes require application restart
- Real-time validation of configuration changes

### 🎛️ **Manual Operations**
- Preview cleanup operations (dry-run mode)
- Trigger manual cleanup tasks
- View recent application logs
- Quick access to common operations

### 🔄 **Auto-refresh Features**
- Status updates every 30 seconds
- Real-time cleanup progress
- Live system metrics
- Responsive design for mobile devices

Access the dashboard at: `https://localhost:7041` or `http://localhost:5114`

### Example Requests

```bash
# Get server status
curl -X GET "https://localhost:7041/api/status"

# Get current configuration
curl -X GET "https://localhost:7041/api/status/config"

# Update configuration
curl -X POST "https://localhost:7041/api/status/config" \
  -H "Content-Type: application/json" \
  -d @new-config.json

# Get cleanup status
curl -X GET "https://localhost:7041/api/cleanup/status"

# Run dry-run movie cleanup
curl -X POST "https://localhost:7041/api/cleanup/movies?dryRun=true"

# Preview items for cleanup
curl -X GET "https://localhost:7041/api/cleanup/preview/Movie"
```

## Dashboard Features

### Quick Actions
- **Preview Cleanups**: Test cleanup operations without deleting files
- **Manual Triggers**: Run specific cleanup types on demand
- **Configuration Toggle**: Easy access to configuration management
- **Real-time Refresh**: Update status and logs without page reload

### Configuration Management
- **Secure Editing**: API keys and passwords are masked in the UI
- **Validation**: Client-side validation for required fields
- **Restart Warnings**: Clear indication when restart is required
- **Backup**: Configuration changes are written to `appsettings.json`

### Monitoring
- **System Metrics**: CPU, memory, and disk usage information
- **Cleanup History**: Track deletion counts and space freed
- **Service Status**: Monitor external service connectivity
- **Live Logs**: View recent application logs with auto-scroll

## Project Structure

```
├── Configuration/          # Application configuration classes
│   └── ApplicationOptions.cs
├── Controllers/            # API controllers
│   └── CleanupController.cs
├── Clients/               # HTTP client interfaces (Refit)
│   └── ApiClients.cs
├── Models/                # Data models and DTOs
│   └── MediaModels.cs
├── Services/              # Business logic services
│   └── CleanupService.cs
├── Webhooks/              # Webhook notification service
│   └── WebhookService.cs
└── Program.cs             # Application entry point
```

## Key Differences from Kotlin Version

1. **Configuration**: Uses ASP.NET Core's `IOptions<T>` pattern instead of Spring Boot's `@ConfigurationProperties`
2. **HTTP Clients**: Uses Refit for type-safe HTTP clients instead of Feign
3. **Dependency Injection**: Uses ASP.NET Core's built-in DI container
4. **Logging**: Uses Serilog with ASP.NET Core integration
5. **Scheduling**: Background services with hosted services instead of Spring's `@Scheduled`

## Development

### Building
```bash
dotnet build
```

### Running Tests
```bash
dotnet test
```

### Watching for Changes
```bash
dotnet watch run
```

## Docker Support

The project includes Docker support for containerized deployment:

```bash
# Build image
docker build -t janitor-aspnet .

# Run container
docker run -p 8080:8080 janitor-aspnet
```

## Contributing

This project follows the transpilation principles from the original Janitorr codebase while adapting to C# and ASP.NET Core best practices.

## License

Same as the original Janitorr project.
