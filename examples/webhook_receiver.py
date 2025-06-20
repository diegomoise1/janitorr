#!/usr/bin/env python3
"""
Simple Janitorr Webhook Receiver Example
This script demonstrates how to receive and process webhooks from Janitorr.
"""

import hmac
import hashlib
import json
from flask import Flask, request, abort

app = Flask(__name__)

# Configuration
WEBHOOK_SECRET = "your-webhook-secret"  # Must match the secret in your Janitorr config
WEBHOOK_PATH = "/janitorr-webhook"

def verify_signature(payload, signature, secret):
    """Verify the HMAC signature from Janitorr"""
    if not signature or not secret:
        return True  # No signature verification if secret not configured
    
    expected_signature = hmac.new(
        secret.encode('utf-8'),
        payload,
        hashlib.sha256
    ).hexdigest()
    
    expected_full = f"sha256={expected_signature}"
    return hmac.compare_digest(expected_full, signature)

@app.route(WEBHOOK_PATH, methods=['POST'])
def handle_janitorr_webhook():
    """Handle incoming webhook from Janitorr"""
    
    # Get the signature from headers
    signature = request.headers.get('X-Janitorr-Signature')
    
    # Verify the signature
    if not verify_signature(request.get_data(), signature, WEBHOOK_SECRET):
        print("Invalid webhook signature!")
        abort(403)
    
    # Parse the webhook payload
    try:
        payload = request.get_json()
    except Exception as e:
        print(f"Error parsing JSON: {e}")
        abort(400)
    
    event = payload.get('event')
    cleanup_type = payload.get('cleanupType')
    items = payload.get('items', [])
    timestamp = payload.get('timestamp')
    
    print(f"🔔 Received webhook: {event}")
    print(f"   Cleanup Type: {cleanup_type}")
    print(f"   Timestamp: {timestamp}")
    print(f"   Items: {len(items)}")
    
    # Handle different event types
    if event == 'MEDIA_MARKED_FOR_DELETION':
        handle_media_marked_for_deletion(items, cleanup_type)
    elif event == 'MEDIA_DELETED':
        handle_media_deleted(items, cleanup_type)
    elif event == 'CLEANUP_STARTED':
        handle_cleanup_started(cleanup_type)
    elif event == 'CLEANUP_COMPLETED':
        handle_cleanup_completed(cleanup_type)
    else:
        print(f"Unknown event type: {event}")
    
    return "OK", 200

def handle_media_marked_for_deletion(items, cleanup_type):
    """Handle when media is marked for deletion"""
    print(f"🗑️  {len(items)} items marked for deletion ({cleanup_type})")
    
    for item in items:
        title = item.get('title', 'Unknown')
        library_type = item.get('libraryType', 'Unknown')
        imdb_id = item.get('imdbId', 'N/A')
        
        print(f"   • {title} ({library_type}) - IMDB: {imdb_id}")
        
        # Example integrations you could add here:
        # - Send Discord notification
        # - Update database
        # - Send email alert
        # - Create backup before deletion
        # - Log to external monitoring system

def handle_media_deleted(items, cleanup_type):
    """Handle when media has been deleted"""
    print(f"✅ {len(items)} items successfully deleted ({cleanup_type})")
    
    for item in items:
        title = item.get('title', 'Unknown')
        print(f"   • Deleted: {title}")

def handle_cleanup_started(cleanup_type):
    """Handle when cleanup process starts"""
    print(f"🚀 Cleanup started: {cleanup_type}")

def handle_cleanup_completed(cleanup_type):
    """Handle when cleanup process completes"""
    print(f"🏁 Cleanup completed: {cleanup_type}")

# Example Discord integration function
def send_discord_notification(message):
    """Send notification to Discord (requires discord webhook URL)"""
    import requests
    
    DISCORD_WEBHOOK_URL = "YOUR_DISCORD_WEBHOOK_URL"  # Replace with your Discord webhook URL
    
    if not DISCORD_WEBHOOK_URL or DISCORD_WEBHOOK_URL == "YOUR_DISCORD_WEBHOOK_URL":
        return  # Skip if not configured
    
    try:
        requests.post(DISCORD_WEBHOOK_URL, json={"content": message}, timeout=10)
        print("✅ Discord notification sent")
    except Exception as e:
        print(f"❌ Failed to send Discord notification: {e}")

# Example usage with Discord integration
def handle_media_marked_for_deletion_with_discord(items, cleanup_type):
    """Enhanced handler that also sends Discord notification"""
    handle_media_marked_for_deletion(items, cleanup_type)
    
    if items:
        item_list = "\\n".join([f"• {item.get('title', 'Unknown')}" for item in items[:5]])
        if len(items) > 5:
            item_list += f"\\n• ... and {len(items) - 5} more items"
        
        message = f"🗑️ Janitorr is about to delete {len(items)} item(s):\\n{item_list}"
        send_discord_notification(message)

if __name__ == '__main__':
    print("🚀 Starting Janitorr Webhook Receiver")
    print(f"📡 Listening for webhooks at: {WEBHOOK_PATH}")
    print("🔒 Signature verification:", "enabled" if WEBHOOK_SECRET != "your-webhook-secret" else "disabled")
    
    # Run the Flask app
    app.run(host='0.0.0.0', port=5000, debug=True)
