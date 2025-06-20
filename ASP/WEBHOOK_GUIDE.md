# Webhook Configuration Guide

The Janitor ASP.NET application supports multiple webhook endpoints with flexible event and cleanup type filtering. This allows you to send different notifications to different services based on your needs.

## Configuration

Webhooks are configured in the `appsettings.json` file under the `Application.Webhooks` section:

```json
{
  "Application": {
    "Webhooks": {
      "Enabled": true,
      "RetryAttempts": 3,
      "TimeoutSeconds": 30,
      "Endpoints": [
        // Webhook endpoint configurations
      ]
    }
  }
}
```

### Global Webhook Settings

- **Enabled**: Master switch to enable/disable all webhooks
- **RetryAttempts**: Number of retry attempts for failed webhook deliveries (default: 3)
- **TimeoutSeconds**: HTTP timeout for webhook requests (default: 30)

## Webhook Endpoints

Each webhook endpoint can be configured with the following properties:

### Basic Configuration

- **Name**: A friendly name for the endpoint (used in logs and API responses)
- **Url**: The webhook URL to send requests to
- **Enabled**: Whether this specific endpoint is enabled

### Event Filtering

- **Events**: Array of webhook events this endpoint should receive
  - `MediaMarkedForDeletion`: When media is marked for deletion
  - `MediaDeleted`: When media has been successfully deleted
  - `CleanupStarted`: When a cleanup process begins
  - `CleanupCompleted`: When a cleanup process finishes
  - `MediaMoved`: When media is moved (future feature)
  - `MediaUnmonitored`: When media is unmonitored (future feature)
  - `HealthCheck`: For endpoint testing and health monitoring

### Cleanup Type Filtering

- **CleanupTypes**: Array of cleanup types this endpoint should receive (empty means all types)
  - `Movie`: Movie cleanup events
  - `Season`: TV season cleanup events
  - `WeeklyEpisode`: Weekly episode cleanup events
  - `TagBased`: Tag-based cleanup events

### Authentication

Multiple authentication methods are supported:

#### None (Default)
```json
{
  "AuthType": "None"
}
```

#### Bearer Token
```json
{
  "AuthType": "Bearer",
  "AuthToken": "your-bearer-token"
}
```

#### Basic Authentication
```json
{
  "AuthType": "Basic",
  "AuthUsername": "username",
  "AuthPassword": "password"
}
```

#### API Key
```json
{
  "AuthType": "ApiKey",
  "AuthToken": "your-api-key"
}
```
*Note: API Key auth adds an `X-API-Key` header*

### Security

- **Secret**: HMAC-SHA256 secret for webhook signature verification
  - When provided, adds `X-Janitor-Signature` header with payload signature
- **Headers**: Custom headers to include in webhook requests

## Example Configurations

### Discord Webhook
```json
{
  "Name": "Discord",
  "Url": "https://discord.com/api/webhooks/YOUR_WEBHOOK_ID/YOUR_WEBHOOK_TOKEN",
  "Enabled": true,
  "Events": ["MediaMarkedForDeletion", "MediaDeleted", "CleanupCompleted"],
  "CleanupTypes": ["Movie", "Season"],
  "AuthType": "None"
}
```

### Slack Webhook
```json
{
  "Name": "Slack",
  "Url": "https://hooks.slack.com/services/YOUR/SLACK/WEBHOOK",
  "Enabled": true,
  "Events": ["CleanupCompleted"],
  "CleanupTypes": [],
  "AuthType": "None"
}
```

### Custom API with Authentication
```json
{
  "Name": "Custom API",
  "Url": "https://your-api.example.com/webhooks/janitor",
  "Enabled": true,
  "Events": ["MediaMarkedForDeletion", "MediaDeleted", "CleanupStarted", "CleanupCompleted"],
  "CleanupTypes": ["Movie", "Season", "WeeklyEpisode", "TagBased"],
  "Headers": {
    "X-Custom-Header": "janitor-webhook"
  },
  "AuthType": "Bearer",
  "AuthToken": "your-bearer-token-here",
  "Secret": "your-hmac-secret-for-signature-verification"
}
```

### Home Assistant Webhook
```json
{
  "Name": "Home Assistant",
  "Url": "http://homeassistant.local:8123/api/webhook/janitor_media_cleanup",
  "Enabled": true,
  "Events": ["CleanupStarted", "CleanupCompleted"],
  "CleanupTypes": [],
  "AuthType": "ApiKey",
  "AuthToken": "your-home-assistant-long-lived-token"
}
```

## Webhook Payload

All webhooks receive a JSON payload with the following structure:

```json
{
  "event": "MediaDeleted",
  "timestamp": "2025-06-20T10:30:00Z",
  "cleanupType": "Movie",
  "items": [
    {
      "id": 123,
      "title": "Movie Title",
      "libraryType": "Movies",
      "imdbId": "tt1234567",
      "tmdbId": 12345,
      "parentPath": "/data/movies/Movie Title (2023)",
      "originalPath": "/data/movies/Movie Title (2023)",
      "season": null,
      "tags": ["action", "adventure"],
      "importedDate": "2024-01-15T08:00:00Z",
      "lastSeen": "2024-06-15T20:30:00Z",
      "historyAge": "2024-06-10T15:45:00Z",
      "seeding": false
    }
  ],
  "itemsDeleted": 5,
  "spaceFreed": 52428800,
  "testMessage": null
}
```

### Standard Headers

All webhook requests include these headers:
- `Content-Type: application/json`
- `User-Agent: Janitor-ASP/1.0`
- `X-Janitor-Event: {EventType}`
- `X-Janitor-Cleanup-Type: {CleanupType}` (when applicable)
- `X-Janitor-Signature: {HMAC-SHA256}` (when secret is configured)

## API Endpoints

The webhook system provides several API endpoints for management and testing:

### Get Webhook Configuration
```
GET /api/webhook/config
```
Returns the current webhook configuration and status.

### Get Endpoint Status
```
GET /api/webhook/endpoints
```
Returns the status of all configured webhook endpoints.

### Test Specific Endpoint
```
POST /api/webhook/endpoints/{endpointName}/test
```
Sends a test webhook to the specified endpoint.

### Send Health Check
```
POST /api/webhook/health-check
```
Sends a health check webhook to all enabled endpoints.

### Simulate Events
```
POST /api/webhook/simulate/{eventType}
```
Simulates webhook events for testing purposes.

Example:
```bash
curl -X POST "http://localhost:5000/api/webhook/simulate/MediaDeleted" \
  -H "Content-Type: application/json" \
  -d '{"cleanupType": "Movie", "itemCount": 3}'
```

## Best Practices

1. **Use specific event filtering**: Only subscribe to events you need to reduce noise
2. **Filter by cleanup type**: Use cleanup type filtering for different notification strategies
3. **Implement signature verification**: Use the HMAC secret for security in production
4. **Handle retries gracefully**: The system will retry failed deliveries, ensure your endpoint is idempotent
5. **Monitor endpoint health**: Use the health check events to monitor webhook delivery
6. **Test your endpoints**: Use the API testing endpoints before going live

## Troubleshooting

### Webhook Not Received
1. Check that the webhook endpoint is enabled
2. Verify the event is in the endpoint's event list
3. Check cleanup type filtering if applicable
4. Review logs for delivery errors

### Authentication Issues
1. Verify the auth type matches your API requirements
2. Check that tokens/credentials are correct
3. Ensure headers are properly formatted

### Delivery Failures
1. Check endpoint URL accessibility
2. Verify SSL/TLS configuration
3. Review timeout settings
4. Check endpoint response codes in logs

The webhook system provides comprehensive logging to help diagnose issues. Check the application logs for detailed information about webhook delivery attempts and failures.
