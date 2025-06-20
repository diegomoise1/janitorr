package com.github.schaka.janitorr.cleanup

import com.github.schaka.janitorr.config.ApplicationProperties
import com.github.schaka.janitorr.config.WebhookEvent
import com.github.schaka.janitorr.servarr.bazarr.BazarrRestService
import com.github.schaka.janitorr.servarr.data_structures.Tag
import com.github.schaka.janitorr.servarr.history.HistoryResponse
import com.github.schaka.janitorr.servarr.radarr.RadarrRestService
import com.github.schaka.janitorr.servarr.sonarr.SonarrClient
import com.github.schaka.janitorr.servarr.sonarr.SonarrProperties
import com.github.schaka.janitorr.servarr.sonarr.SonarrRestService
import com.github.schaka.janitorr.servarr.sonarr.episodes.EpisodeResponse
import com.github.schaka.janitorr.webhook.WebhookPayload
import com.github.schaka.janitorr.webhook.WebhookMediaItem
import com.github.schaka.janitorr.webhook.WebhookService
import com.github.schaka.janitorr.mediaserver.library.LibraryType
import com.github.schaka.janitorr.servarr.LibraryItem
import org.slf4j.LoggerFactory
import org.springframework.aot.hint.annotation.RegisterReflectionForBinding
import org.springframework.cache.annotation.CacheEvict
import org.springframework.scheduling.annotation.Scheduled
import org.springframework.stereotype.Service
import java.time.LocalDate
import java.time.LocalDateTime

/**
 * This class works differently than the other schedules because it covers one special use case.
 * TV shows only (mostly daily episodes), regarding only the latest season or x amount of latest episodes.
 */
@Service
@RegisterReflectionForBinding(classes = [Tag::class,HistoryResponse::class, EpisodeResponse::class])
class WeeklyEpisodeCleanupSchedule(
        val applicationProperties: ApplicationProperties,
        val sonarrProperties: SonarrProperties,
        val sonarrClient: SonarrClient,
        val runOnce: RunOnce,
        val webhookService: WebhookService? = null,

        var episodeTag: Tag = Tag(Integer.MIN_VALUE, "Not_Set")
) {

    companion object {
        private val log = LoggerFactory.getLogger(this::class.java.enclosingClass)
    }

    init {
        if (sonarrProperties.enabled) {
            episodeTag = sonarrClient.getAllTags().firstOrNull { it.label == applicationProperties.episodeDeletion.tag } ?: episodeTag
        }
    }

    // run every hour
    @CacheEvict(cacheNames = [SonarrRestService.CACHE_NAME, RadarrRestService.CACHE_NAME, BazarrRestService.CACHE_NAME_TV, BazarrRestService.CACHE_NAME_MOVIES])
    @Scheduled(fixedDelay = 1000 * 60 * 60)
    fun runSchedule() {

        if (!applicationProperties.episodeDeletion.enabled) {
            log.info("Episode based cleanup disabled, do nothing")
            runOnce.hasWeeklyEpisodeCleanupRun = true
            return
        }

        val today = LocalDateTime.now()
        val series = sonarrClient.getAllSeries().filter { it.tags.contains(episodeTag.id) }
        val allTags = sonarrClient.getAllTags()

        for (show in series) {
            val latestSeason = show.seasons.maxBy { season -> season.seasonNumber }
            val episodes = sonarrClient.getAllEpisodes(show.id, latestSeason.seasonNumber)
                .filter { it.airDate != null }
                .filter { LocalDate.parse(it.airDate!!) <= today.toLocalDate() }
                .toMutableList()

            val episodesToDelete = mutableListOf<EpisodeResponse>()

            val episodesHistory = sonarrClient.getHistory(show.id, latestSeason.seasonNumber)
                    .sortedBy { parseDate(it.date)}
                    .distinctBy { it.episodeId }

            log.info("Deleting single episodes of ${show.title}")

            // Delete by age
            for (episodeHistory in episodesHistory) {
                val episode = episodes.first{ it.seriesId == episodeHistory.seriesId && it.id == episodeHistory.episodeId }
                val grabDate = parseDate(episodeHistory.date)
                if (grabDate + applicationProperties.episodeDeletion.maxAge <= today) {
                    log.trace("Deleting episode ${episode.episodeNumber} of ${show.title} S${latestSeason.seasonNumber} because of its age")

                    if (episode.episodeFileId != null && episode.episodeFileId != 0) {
                        episodesToDelete.add(episode)
                        if (!applicationProperties.dryRun) {
                            sonarrClient.deleteEpisodeFile(episode.episodeFileId)
                            episodes.remove(episode)
                        }
                    }
                }
            }

            // Delete by count
            if (episodes.size > applicationProperties.episodeDeletion.maxEpisodes) {
                val leftoverEpisodes = episodes.sortedByDescending { it.episodeNumber }.take(applicationProperties.episodeDeletion.maxEpisodes)
                episodes.removeAll(leftoverEpisodes) // remove the most recent episodes from the list, as we want to keep those

                for (episode in episodes) {
                    log.trace("Deleting episode ${episode.episodeNumber} of ${show.title} S${latestSeason.seasonNumber} because there are too many episodes")

                    if (episode.episodeFileId != null && episode.episodeFileId != 0) {
                        episodesToDelete.add(episode)
                        if (!applicationProperties.dryRun) {
                            sonarrClient.deleteEpisodeFile(episode.episodeFileId)
                        }
                    }
                }
            }

            // Send webhook for deleted episodes
            if (episodesToDelete.isNotEmpty()) {
                val webhookItems = episodesToDelete.map { episode ->
                    WebhookMediaItem(
                        id = episode.id,
                        title = "${show.title} - S${latestSeason.seasonNumber}E${episode.episodeNumber}: ${episode.title}",
                        libraryType = LibraryType.TV_SHOWS,
                        imdbId = show.imdbId,
                        tmdbId = show.tvdbId,
                        parentPath = show.path,
                        originalPath = show.path,
                        season = latestSeason.seasonNumber,
                        tags = allTags.filter { tag -> show.tags.contains(tag.id) }.map { tag -> tag.label },
                        importedDate = null,
                        lastSeen = null,
                        historyAge = today,
                        seeding = false
                    )
                }

                webhookService?.sendWebhook(
                    WebhookPayload(
                        event = WebhookEvent.MEDIA_MARKED_FOR_DELETION,
                        timestamp = today,
                        cleanupType = CleanupType.WEEKLY_EPISODE,
                        items = webhookItems
                    )
                )
            }
        }

        runOnce.hasWeeklyEpisodeCleanupRun = true
    }

    private fun parseDate(date: String): LocalDateTime {
        return LocalDateTime.parse(date.substring(0, date.length - 1))
    }

}