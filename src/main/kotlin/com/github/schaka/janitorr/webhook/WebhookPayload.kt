package com.github.schaka.janitorr.webhook

import com.github.schaka.janitorr.cleanup.CleanupType
import com.github.schaka.janitorr.config.WebhookEvent
import com.github.schaka.janitorr.mediaserver.library.LibraryType
import com.github.schaka.janitorr.servarr.LibraryItem
import java.time.LocalDateTime

data class WebhookPayload(
        val event: WebhookEvent,
        val timestamp: LocalDateTime,
        val cleanupType: CleanupType,
        val items: List<WebhookMediaItem>
)

data class WebhookMediaItem(
        val id: Int,
        val title: String,
        val libraryType: LibraryType,
        val imdbId: String?,
        val tmdbId: Int?,
        val parentPath: String,
        val originalPath: String,
        val season: Int?,
        val tags: List<String>,
        val importedDate: LocalDateTime?,
        val lastSeen: LocalDateTime?,
        val historyAge: LocalDateTime,
        val seeding: Boolean
) {
    companion object {
        fun fromLibraryItem(item: LibraryItem): WebhookMediaItem {
            return WebhookMediaItem(
                    id = item.id,
                    title = item.title,
                    libraryType = item.libraryType,
                    imdbId = item.imdbId,
                    tmdbId = item.tmdbId,
                    parentPath = item.parentPath,
                    originalPath = item.originalPath,
                    season = item.season,
                    tags = item.tags,
                    importedDate = item.importedDate,
                    lastSeen = item.lastSeen,
                    historyAge = item.historyAge,
                    seeding = item.seeding
            )
        }
    }
}
