<!-- Use this file to provide workspace-specific custom instructions to Copilot. For more details, visit https://code.visualstudio.com/docs/copilot/copilot-customization#_use-a-githubcopilotinstructionsmd-file -->

# Janitor ASP.NET Project Instructions

This is an ASP.NET Core Web API project that transpiles the Janitorr Kotlin/Spring Boot application to C#.

## Project Context
- This is a media management application that integrates with Jellyseerr, Radarr, Sonarr, and media servers
- The application performs automated cleanup of media files based on various criteria
- Core functionality includes scheduled cleanup tasks, webhook notifications, and REST API endpoints
- The project uses HTTP clients to communicate with external APIs (Jellyseerr, *arr services, media servers)

## Code Style Guidelines
- Use modern C# features (records, nullable reference types, async/await)
- Follow ASP.NET Core best practices for dependency injection
- Use the Options pattern for configuration
- Implement proper error handling and logging
- Use Refit for HTTP client interfaces where applicable
- Follow clean architecture principles with separate layers for controllers, services, and data models

## Key Components to Implement
- Configuration classes using IOptions<T> pattern
- HTTP client services using Refit or typed HttpClient
- Background services for cleanup scheduling
- Webhook service for notifications
- Controllers for REST API endpoints
- Data models and DTOs for API responses

## External Integrations
- Jellyseerr API for request management
- Radarr/Sonarr APIs for media management
- Jellyfin/Emby/Plex media server APIs
- File system operations for media cleanup
- Webhook endpoints for notifications
