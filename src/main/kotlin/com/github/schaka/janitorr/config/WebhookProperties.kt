package com.github.schaka.janitorr.config

data class WebhookProperties(
        val enabled: Boolean = false,
        val endpoints: List<WebhookEndpoint> = listOf(),
        val retryAttempts: Int = 3,
        val timeoutSeconds: Int = 30
)

data class WebhookEndpoint(
        val url: String,
        val events: List<WebhookEvent> = listOf(WebhookEvent.MEDIA_MARKED_FOR_DELETION),
        val headers: Map<String, String> = mapOf(),
        val secret: String? = null
)

enum class WebhookEvent {
    MEDIA_MARKED_FOR_DELETION,
    MEDIA_DELETED,
    CLEANUP_STARTED,
    CLEANUP_COMPLETED
}
