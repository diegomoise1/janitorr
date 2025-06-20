package com.github.schaka.janitorr.cleanup

enum class CleanupType(
        val folderName: String
) {
    MEDIA("media"),
    TAG("tag-based"),
    WEEKLY_EPISODE("weekly-episode")
}