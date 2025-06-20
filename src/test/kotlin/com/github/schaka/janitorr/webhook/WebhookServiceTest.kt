package com.github.schaka.janitorr.webhook

import com.fasterxml.jackson.databind.ObjectMapper
import com.github.schaka.janitorr.cleanup.CleanupType
import com.github.schaka.janitorr.config.WebhookEndpoint
import com.github.schaka.janitorr.config.WebhookEvent
import com.github.schaka.janitorr.config.WebhookProperties
import com.github.schaka.janitorr.mediaserver.library.LibraryType
import org.junit.jupiter.api.BeforeEach
import org.junit.jupiter.api.Test
import org.mockito.ArgumentCaptor
import org.mockito.kotlin.*
import org.springframework.http.HttpEntity
import org.springframework.http.HttpMethod
import org.springframework.http.HttpStatus
import org.springframework.http.ResponseEntity
import org.springframework.web.client.RestTemplate
import java.time.LocalDateTime
import kotlin.test.assertEquals
import kotlin.test.assertTrue

class WebhookServiceTest {

    private lateinit var restTemplate: RestTemplate
    private lateinit var objectMapper: ObjectMapper
    private lateinit var webhookProperties: WebhookProperties
    private lateinit var webhookService: WebhookService

    @BeforeEach
    fun setUp() {
        restTemplate = mock()
        objectMapper = ObjectMapper()
        webhookProperties = WebhookProperties(
            enabled = true,
            endpoints = listOf(
                WebhookEndpoint(
                    url = "https://example.com/webhook",
                    events = listOf(WebhookEvent.MEDIA_MARKED_FOR_DELETION),
                    headers = mapOf("Authorization" to "Bearer test-token"),
                    secret = "test-secret"
                )
            ),
            retryAttempts = 2,
            timeoutSeconds = 5
        )
        webhookService = WebhookService(restTemplate, objectMapper, webhookProperties)
    }

    @Test
    fun `should send webhook when enabled`() {
        // Given
        val payload = WebhookPayload(
            event = WebhookEvent.MEDIA_MARKED_FOR_DELETION,
            timestamp = LocalDateTime.now(),
            cleanupType = CleanupType.MEDIA,
            items = listOf(
                WebhookMediaItem(
                    id = 1,
                    title = "Test Movie",
                    libraryType = LibraryType.MOVIES,
                    imdbId = "tt1234567",
                    tmdbId = 12345,
                    parentPath = "/movies/test",
                    originalPath = "/movies/test/movie.mkv",
                    season = null,
                    tags = listOf("action"),
                    importedDate = LocalDateTime.now().minusDays(30),
                    lastSeen = LocalDateTime.now().minusDays(10),
                    historyAge = LocalDateTime.now().minusDays(30),
                    seeding = false
                )
            )
        )

        whenever(restTemplate.exchange(any<String>(), any<HttpMethod>(), any<HttpEntity<String>>(), eq(String::class.java)))
            .thenReturn(ResponseEntity("OK", HttpStatus.OK))

        // When
        webhookService.sendWebhook(payload)

        // Then
        Thread.sleep(100) // Allow async execution to complete
        verify(restTemplate, timeout(1000)).exchange(
            eq("https://example.com/webhook"),
            eq(HttpMethod.POST),
            any<HttpEntity<String>>(),
            eq(String::class.java)
        )
    }

    @Test
    fun `should not send webhook when disabled`() {
        // Given
        val disabledProperties = webhookProperties.copy(enabled = false)
        val disabledService = WebhookService(restTemplate, objectMapper, disabledProperties)
        val payload = WebhookPayload(
            event = WebhookEvent.MEDIA_MARKED_FOR_DELETION,
            timestamp = LocalDateTime.now(),
            cleanupType = CleanupType.MEDIA,
            items = emptyList()
        )

        // When
        disabledService.sendWebhook(payload)

        // Then
        Thread.sleep(100) // Allow potential async execution
        verify(restTemplate, never()).exchange(any<String>(), any<HttpMethod>(), any<HttpEntity<String>>(), any<Class<String>>())
    }

    @Test
    fun `should include HMAC signature when secret is provided`() {
        // Given
        val payload = WebhookPayload(
            event = WebhookEvent.MEDIA_MARKED_FOR_DELETION,
            timestamp = LocalDateTime.now(),
            cleanupType = CleanupType.MEDIA,
            items = emptyList()
        )

        val httpEntityCaptor = ArgumentCaptor.forClass(HttpEntity::class.java)
        whenever(restTemplate.exchange(any<String>(), any<HttpMethod>(), httpEntityCaptor.capture(), eq(String::class.java)))
            .thenReturn(ResponseEntity("OK", HttpStatus.OK))

        // When
        webhookService.sendWebhook(payload)

        // Then
        Thread.sleep(100) // Allow async execution to complete
        verify(restTemplate, timeout(1000)).exchange(any<String>(), any<HttpMethod>(), any<HttpEntity<String>>(), eq(String::class.java))
        
        val capturedEntity = httpEntityCaptor.value as HttpEntity<String>
        val headers = capturedEntity.headers
        assertTrue(headers.containsKey("X-Janitorr-Signature"))
        assertTrue(headers["X-Janitorr-Signature"]!![0].startsWith("sha256="))
    }

    @Test
    fun `should filter endpoints by event type`() {
        // Given
        val wrongEventProperties = webhookProperties.copy(
            endpoints = listOf(
                WebhookEndpoint(
                    url = "https://example.com/webhook",
                    events = listOf(WebhookEvent.CLEANUP_STARTED), // Different event
                    headers = mapOf(),
                    secret = null
                )
            )
        )
        val filteringService = WebhookService(restTemplate, objectMapper, wrongEventProperties)
        val payload = WebhookPayload(
            event = WebhookEvent.MEDIA_MARKED_FOR_DELETION,
            timestamp = LocalDateTime.now(),
            cleanupType = CleanupType.MEDIA,
            items = emptyList()
        )

        // When
        filteringService.sendWebhook(payload)

        // Then
        Thread.sleep(100) // Allow potential async execution
        verify(restTemplate, never()).exchange(any<String>(), any<HttpMethod>(), any<HttpEntity<String>>(), any<Class<String>>())
    }
}
