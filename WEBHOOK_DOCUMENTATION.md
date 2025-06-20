# Webhook Feature Documentation

## Overview

Janitorr now supports webhooks that can notify external systems when media is marked for deletion or actually deleted. This feature is useful for integrating with notification systems, Discord bots, monitoring tools, or any custom automation.

## Configuration

Add the following configuration to your `application.yml`:

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
          Authorization: "Bearer your-secret-token"
        secret: "your-webhook-secret"
```

## Webhook Events

The webhook system supports four types of events:

- `MEDIA_MARKED_FOR_DELETION`: Sent when media is identified for deletion but before actual deletion occurs
- `MEDIA_DELETED`: Sent after media has been successfully deleted
- `CLEANUP_STARTED`: Sent when a cleanup process begins
- `CLEANUP_COMPLETED`: Sent when a cleanup process finishes

## Webhook Payload

Each webhook call includes a JSON payload with the following structure:

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
      "originalPath": "/data/movies/Example Movie (2023)/Example.Movie.2023.mkv",
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

## Security

### HMAC Signature Verification

When a `secret` is configured for an endpoint, Janitorr will include an HMAC SHA-256 signature in the `X-Janitorr-Signature` header. This allows you to verify that the webhook came from your Janitorr instance.

Example verification in Python:
```python
import hmac
import hashlib

def verify_webhook(payload, signature, secret):
    expected_signature = hmac.new(
        secret.encode('utf-8'),
        payload.encode('utf-8'),
        hashlib.sha256
    ).hexdigest()
    return hmac.compare_digest(f"sha256={expected_signature}", signature)
```

### Custom Headers

You can add custom headers to webhook requests, such as authorization tokens or API keys.

## Example Integrations

### Discord Notification
```python
import json
import requests
from flask import Flask, request

app = Flask(__name__)

@app.route('/janitorr-webhook', methods=['POST'])
def handle_janitorr_webhook():
    payload = request.get_json()
    
    if payload['event'] == 'MEDIA_MARKED_FOR_DELETION':
        items = payload['items']
        message = f"🗑️ Janitorr is about to delete {len(items)} item(s):\\n"
        
        for item in items[:5]:  # Limit to first 5 items
            message += f"• {item['title']} ({item['libraryType']})\n"
        
        if len(items) > 5:
            message += f"• ... and {len(items) - 5} more items"
        
        # Send to Discord webhook
        discord_webhook_url = "YOUR_DISCORD_WEBHOOK_URL"
        requests.post(discord_webhook_url, json={"content": message})
    
    return "OK"
```

### Home Assistant Integration
```yaml
# configuration.yaml
automation:
  - alias: "Janitorr Deletion Notification"
    trigger:
      platform: webhook
      webhook_id: janitorr_webhook
    action:
      - service: notify.mobile_app_your_phone
        data:
          title: "Media Cleanup"
          message: >
            Janitorr {{ trigger.json.event | replace('_', ' ') | lower }}:
            {{ trigger.json.items | length }} items
```

## Troubleshooting

### Failed Webhook Delivery

Janitorr will retry failed webhook deliveries using exponential backoff:
- 1st retry: 1 second delay
- 2nd retry: 2 second delay  
- 3rd retry: 4 second delay

Check your Janitorr logs for webhook delivery errors:

```
log.error("Failed to send webhook to {} after {} attempts", endpoint.url, webhookProperties.retryAttempts)
```

### Testing Webhooks

You can use tools like [webhook.site](https://webhook.site) or [ngrok](https://ngrok.com) to test your webhook configuration during development.

## Performance Considerations

- Webhooks are sent asynchronously to avoid blocking cleanup operations
- Failed webhooks are retried with exponential backoff
- Client errors (4xx) are not retried, only server errors (5xx) and connection issues
- Each endpoint can be configured with different events to reduce unnecessary calls
