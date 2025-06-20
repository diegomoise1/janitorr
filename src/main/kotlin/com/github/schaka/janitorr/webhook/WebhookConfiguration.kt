package com.github.schaka.janitorr.webhook

import com.github.schaka.janitorr.config.WebhookProperties
import org.springframework.boot.context.properties.EnableConfigurationProperties
import org.springframework.boot.web.client.RestTemplateBuilder
import org.springframework.context.annotation.Bean
import org.springframework.context.annotation.Configuration
import org.springframework.web.client.RestTemplate
import java.time.Duration

@Configuration
@EnableConfigurationProperties(WebhookProperties::class)
class WebhookConfiguration {

    @Bean("webhookRestTemplate")
    fun webhookRestTemplate(
            restTemplateBuilder: RestTemplateBuilder,
            webhookProperties: WebhookProperties
    ): RestTemplate {
        return restTemplateBuilder
                .setConnectTimeout(Duration.ofSeconds(webhookProperties.timeoutSeconds.toLong()))
                .setReadTimeout(Duration.ofSeconds(webhookProperties.timeoutSeconds.toLong()))
                .build()
    }
}
