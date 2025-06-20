package com.github.schaka.janitorr.webhook

import com.fasterxml.jackson.databind.ObjectMapper
import com.github.schaka.janitorr.config.WebhookEndpoint
import com.github.schaka.janitorr.config.WebhookEvent
import com.github.schaka.janitorr.config.WebhookProperties
import org.slf4j.LoggerFactory
import org.springframework.beans.factory.annotation.Qualifier
import org.springframework.boot.context.properties.ConfigurationProperties
import org.springframework.http.HttpEntity
import org.springframework.http.HttpHeaders
import org.springframework.http.HttpMethod
import org.springframework.http.MediaType
import org.springframework.stereotype.Service
import org.springframework.web.client.RestTemplate
import org.springframework.web.client.ResourceAccessException
import org.springframework.web.client.HttpClientErrorException
import org.springframework.web.client.HttpServerErrorException
import java.time.Duration
import java.util.concurrent.CompletableFuture
import javax.crypto.Mac
import javax.crypto.spec.SecretKeySpec
import java.nio.charset.StandardCharsets

@Service
class WebhookService(
        @Qualifier("webhookRestTemplate") private val restTemplate: RestTemplate,
        private val objectMapper: ObjectMapper,
        private val webhookProperties: WebhookProperties
) {

    companion object {
        private val log = LoggerFactory.getLogger(this::class.java.enclosingClass)
        private const val HMAC_SHA256 = "HmacSHA256"
        private const val SIGNATURE_HEADER = "X-Janitorr-Signature"
        private const val TIMESTAMP_HEADER = "X-Janitorr-Timestamp"
        private const val EVENT_HEADER = "X-Janitorr-Event"
    }

    fun sendWebhook(payload: WebhookPayload) {
        if (!webhookProperties.enabled) {
            log.debug("Webhooks are disabled, skipping webhook for event: {}", payload.event)
            return
        }

        val relevantEndpoints = webhookProperties.endpoints.filter { 
            it.events.contains(payload.event) 
        }

        if (relevantEndpoints.isEmpty()) {
            log.debug("No endpoints configured for event: {}", payload.event)
            return
        }

        val jsonPayload = objectMapper.writeValueAsString(payload)
        log.debug("Sending webhook for event: {} to {} endpoints", payload.event, relevantEndpoints.size)

        relevantEndpoints.forEach { endpoint ->
            // Send webhooks asynchronously to avoid blocking cleanup operations
            CompletableFuture.runAsync {
                sendWebhookToEndpoint(endpoint, payload.event, jsonPayload)
            }
        }
    }

    private fun sendWebhookToEndpoint(endpoint: WebhookEndpoint, event: WebhookEvent, jsonPayload: String) {
        var attempt = 0
        while (attempt < webhookProperties.retryAttempts) {
            try {
                val headers = HttpHeaders().apply {
                    contentType = MediaType.APPLICATION_JSON
                    set(EVENT_HEADER, event.name)
                    set(TIMESTAMP_HEADER, System.currentTimeMillis().toString())
                    
                    // Add custom headers from configuration
                    endpoint.headers.forEach { (key, value) ->
                        set(key, value)
                    }
                    
                    // Add HMAC signature if secret is configured
                    endpoint.secret?.let { secret ->
                        val signature = generateHmacSignature(jsonPayload, secret)
                        set(SIGNATURE_HEADER, "sha256=$signature")
                    }
                }

                val entity = HttpEntity(jsonPayload, headers)
                
                log.debug("Sending webhook to {} (attempt {}/{})", endpoint.url, attempt + 1, webhookProperties.retryAttempts)
                
                val response = restTemplate.exchange(
                        endpoint.url,
                        HttpMethod.POST,
                        entity,
                        String::class.java
                )

                if (response.statusCode.is2xxSuccessful) {
                    log.info("Successfully sent webhook to {} for event {}", endpoint.url, event)
                    return
                } else {
                    log.warn("Webhook endpoint {} returned status {}", endpoint.url, response.statusCode)
                }

            } catch (e: HttpClientErrorException) {
                log.error("Client error sending webhook to {} (status: {}): {}", 
                        endpoint.url, e.statusCode, e.message)
                return // Don't retry client errors (4xx)
                
            } catch (e: HttpServerErrorException) {
                log.warn("Server error sending webhook to {} (attempt {}/{}): {}", 
                        endpoint.url, attempt + 1, webhookProperties.retryAttempts, e.message)
                        
            } catch (e: ResourceAccessException) {
                log.warn("Connection error sending webhook to {} (attempt {}/{}): {}", 
                        endpoint.url, attempt + 1, webhookProperties.retryAttempts, e.message)
                        
            } catch (e: Exception) {
                log.error("Unexpected error sending webhook to {} (attempt {}/{}): {}", 
                        endpoint.url, attempt + 1, webhookProperties.retryAttempts, e.message, e)
            }

            attempt++
            
            if (attempt < webhookProperties.retryAttempts) {
                try {
                    // Exponential backoff: 1s, 2s, 4s, etc.
                    Thread.sleep(Duration.ofSeconds((1L shl (attempt - 1))).toMillis())
                } catch (ie: InterruptedException) {
                    Thread.currentThread().interrupt()
                    log.warn("Webhook retry interrupted for endpoint {}", endpoint.url)
                    return
                }
            }
        }
        
        log.error("Failed to send webhook to {} after {} attempts", endpoint.url, webhookProperties.retryAttempts)
    }

    private fun generateHmacSignature(payload: String, secret: String): String {
        val mac = Mac.getInstance(HMAC_SHA256)
        val secretKeySpec = SecretKeySpec(secret.toByteArray(StandardCharsets.UTF_8), HMAC_SHA256)
        mac.init(secretKeySpec)
        val hash = mac.doFinal(payload.toByteArray(StandardCharsets.UTF_8))
        return hash.joinToString("") { "%02x".format(it) }
    }
}
