# Janitorr Webhook Implementation Summary

## What Was Added

I've successfully implemented a comprehensive webhook system for Janitorr that notifies external systems when media is marked for deletion or actually deleted. Here's what was added:

### Core Components

1. **Configuration Classes** (`/src/main/kotlin/com/github/schaka/janitorr/config/`)
   - `WebhookProperties.kt` - Main webhook configuration
   - `WebhookEndpoint` data class for endpoint configuration
   - `WebhookEvent` enum for different event types

2. **Webhook Service** (`/src/main/kotlin/com/github/schaka/janitorr/webhook/`)
   - `WebhookService.kt` - Core service for sending webhooks
   - `WebhookPayload.kt` - Data structures for webhook payloads
   - `WebhookMediaItem.kt` - Media item representation for webhooks
   - `WebhookConfiguration.kt` - Spring configuration for webhook beans

3. **Integration Points**
   - Updated `AbstractCleanupSchedule.kt` to send webhooks during deletion
   - Updated `WeeklyEpisodeCleanupSchedule.kt` for episode-specific webhooks
   - Updated `ApplicationProperties.kt` to include webhook configuration

4. **Testing & Examples**
   - Unit tests for webhook functionality
   - Python webhook receiver example
   - Docker Compose setup with webhook integration
   - Complete configuration examples

## Webhook Events

The system supports four types of webhook events:

- `MEDIA_MARKED_FOR_DELETION` - Before media is deleted
- `MEDIA_DELETED` - After successful deletion
- `CLEANUP_STARTED` - When cleanup process begins  
- `CLEANUP_COMPLETED` - When cleanup process finishes

## Key Features

### Security
- **HMAC SHA-256 signature verification** with configurable secrets
- **Custom headers** support for authentication tokens
- **Event filtering** per endpoint

### Reliability  
- **Asynchronous delivery** to avoid blocking cleanup operations
- **Retry logic** with exponential backoff (1s, 2s, 4s delays)
- **Timeout configuration** (default 30 seconds)
- **Comprehensive error handling** with detailed logging

### Flexibility
- **Multiple endpoints** with different event subscriptions
- **Custom headers** per endpoint
- **Configurable retry attempts** and timeouts
- **Event-specific filtering**

## Configuration Example

```yaml
application:
  webhooks:
    enabled: true
    retry-attempts: 3
    timeout-seconds: 30
    endpoints:
      - url: "https://your-webhook-endpoint.com/janitorr"
        events:
          - MEDIA_MARKED_FOR_DELETION
          - MEDIA_DELETED
        headers:
          Authorization: "Bearer your-token"
        secret: "your-webhook-secret"
```

## Webhook Payload Structure

```json
{
  "event": "MEDIA_MARKED_FOR_DELETION",
  "timestamp": "2024-01-15T10:30:00",
  "cleanupType": "MEDIA",
  "items": [
    {
      "id": 123,
      "title": "Example Movie",
      "libraryType": "MOVIES",
      "imdbId": "tt1234567",
      "tmdbId": 12345,
      "parentPath": "/data/movies/Example Movie (2023)",
      "originalPath": "/data/movies/Example Movie (2023)/movie.mkv",
      "season": null,
      "tags": ["4k", "action"],
      "importedDate": "2023-12-01T15:45:00",
      "lastSeen": "2024-01-01T20:00:00",
      "historyAge": "2023-12-01T15:45:00",
      "seeding": false
    }
  ]
}
```

## Integration Examples

### Discord Bot Integration
- Send formatted messages to Discord channels
- Include media details and cleanup statistics
- Support for rich embeds and reactions

### Home Assistant Integration  
- Trigger automations based on cleanup events
- Send mobile notifications
- Update dashboard statistics

### Monitoring Systems
- Forward webhook data to Grafana/Prometheus
- Track deletion patterns and storage cleanup
- Alert on unusual cleanup activity

### Custom Applications
- Backup media before deletion
- Update external databases
- Trigger additional cleanup workflows

## Files Created/Modified

### New Files
- `src/main/kotlin/com/github/schaka/janitorr/config/WebhookProperties.kt`
- `src/main/kotlin/com/github/schaka/janitorr/webhook/WebhookService.kt`
- `src/main/kotlin/com/github/schaka/janitorr/webhook/WebhookPayload.kt`
- `src/main/kotlin/com/github/schaka/janitorr/webhook/WebhookConfiguration.kt`
- `src/test/kotlin/com/github/schaka/janitorr/webhook/WebhookServiceTest.kt`
- `examples/webhook_receiver.py`
- `examples/docker-compose-webhooks.yml`
- `examples/webhook-receiver.Dockerfile`
- `examples/application-with-webhooks.yml`
- `WEBHOOK_DOCUMENTATION.md`

### Modified Files
- `src/main/kotlin/com/github/schaka/janitorr/config/ApplicationProperties.kt`
- `src/main/kotlin/com/github/schaka/janitorr/cleanup/AbstractCleanupSchedule.kt`
- `src/main/kotlin/com/github/schaka/janitorr/cleanup/MediaCleanupSchedule.kt`
- `src/main/kotlin/com/github/schaka/janitorr/cleanup/TagBasedCleanupSchedule.kt`
- `src/main/kotlin/com/github/schaka/janitorr/cleanup/WeeklyEpisodeCleanupSchedule.kt`
- `src/main/kotlin/com/github/schaka/janitorr/cleanup/CleanupType.kt`
- `src/main/resources/application-template.yml`

## Next Steps

1. **Test the implementation** with your existing Janitorr setup
2. **Configure webhook endpoints** in your application.yml
3. **Set up webhook receivers** using the provided examples
4. **Monitor webhook delivery** through Janitorr logs
5. **Customize webhook handling** for your specific use cases

The webhook system is designed to be robust, secure, and easy to integrate with existing systems. It provides comprehensive information about media cleanup operations while maintaining the performance and reliability of Janitorr's core functionality.
