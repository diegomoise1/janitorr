using JanitorAspNet.Models;

namespace JanitorAspNet.Configuration;

/// <summary>
/// Main application configuration options
/// Transpiled from ApplicationProperties.kt and old_application.yml
/// </summary>
public class ApplicationOptions
{
    public const string SectionName = "Application";

    // Core application settings
    public bool DryRun { get; set; } = true;
    public bool RunOnce { get; set; } = false;
    public bool WholeTvShow { get; set; } = false;
    public bool WholeShowSeedingCheck { get; set; } = false;
    public string LeavingSoon { get; set; } = "14d";
    public string ExclusionTag { get; set; } = "janitorr_keep";

    // Scheduling configuration
    public SchedulingOptions Scheduling { get; set; } = new();

    // Client configurations
    public JellyseerrOptions Jellyseerr { get; set; } = new();
    public JellyfinOptions Jellyfin { get; set; } = new();
    public EmbyOptions Emby { get; set; } = new();
    public PlexOptions Plex { get; set; } = new();
    public RadarrOptions Radarr { get; set; } = new();
    public SonarrOptions Sonarr { get; set; } = new();
    public BazarrOptions Bazarr { get; set; } = new();
    public JellystatOptions Jellystat { get; set; } = new();

    // File system settings
    public FileSystemOptions FileSystem { get; set; } = new();

    // Deletion and cleanup settings
    public MediaDeletionOptions MediaDeletion { get; set; } = new();
    public TagBasedDeletionOptions TagBasedDeletion { get; set; } = new();
    public EpisodeDeletionOptions EpisodeDeletion { get; set; } = new();

    // Webhook settings
    public WebhookOptions Webhooks { get; set; } = new();

    // Legacy properties for backward compatibility
    [Obsolete("Use MediaDeletion.Enabled instead")]
    public bool EnableMovieCleanup => MediaDeletion.Enabled;
    
    [Obsolete("Use MediaDeletion.Enabled instead")]
    public bool EnableSeasonCleanup => MediaDeletion.Enabled;
    
    [Obsolete("Use EpisodeDeletion.Enabled instead")]
    public bool EnableWeeklyEpisodeCleanup => EpisodeDeletion.Enabled;
    
    [Obsolete("Use TagBasedDeletion.Enabled instead")]
    public bool EnableTagBasedCleanup => TagBasedDeletion.Enabled;
}

public class FileSystemOptions
{
    public bool Access { get; set; } = true;
    public bool ValidateSeeding { get; set; } = false;
    public string LeavingSoonDir { get; set; } = "/data/media/leaving-soon";
    public string MediaServerLeavingSoonDir { get; set; } = "/media/leaving-soon";
    public bool FromScratch { get; set; } = true;
    public string FreeSpaceCheckDir { get; set; } = "/";
}

public class MediaDeletionOptions
{
    public bool Enabled { get; set; } = true;
    public Dictionary<int, string> MovieExpiration { get; set; } = new()
    {
        { 80, "15d" },
        { 85, "30d" },
        { 90, "60d" },
        { 95, "90d" }
    };
    public Dictionary<int, string> SeasonExpiration { get; set; } = new()
    {
        { 80, "15d" },
        { 85, "20d" },
        { 90, "60d" },
        { 95, "120d" }
    };
}

public class TagBasedDeletionOptions
{
    public bool Enabled { get; set; } = true;
    public int MinimumFreeDiskPercent { get; set; } = 80;
    public List<TagSchedule> Schedules { get; set; } = new();
}

public class TagSchedule
{
    public string Tag { get; set; } = "";
    public string Expiration { get; set; } = "";
}

public class EpisodeDeletionOptions
{
    public bool Enabled { get; set; } = true;
    public string Tag { get; set; } = "janitorr_daily";
    public int MaxEpisodes { get; set; } = 10;
    public string MaxAge { get; set; } = "30d";
}

public class JellyseerrOptions
{
    public bool Enabled { get; set; } = true;
    public string Url { get; set; } = "";
    public string ApiKey { get; set; } = "";
    public bool MatchServer { get; set; } = false;
}

public class JellyfinOptions
{
    public bool Enabled { get; set; } = false;
    public string Url { get; set; } = "";
    public string ApiKey { get; set; } = "";
    public string Username { get; set; } = "";
    public string Password { get; set; } = "";
    public bool Delete { get; set; } = true;
    public string LeavingSoonTv { get; set; } = "Shows (Leaving Soon)";
    public string LeavingSoonMovies { get; set; } = "Movies (Leaving Soon)";
    public LeavingSoonType LeavingSoonType { get; set; } = LeavingSoonType.MoviesAndTv;
}

public class EmbyOptions
{
    public bool Enabled { get; set; } = false;
    public string Url { get; set; } = "";
    public string ApiKey { get; set; } = "";
    public string Username { get; set; } = "";
    public string Password { get; set; } = "";
    public bool Delete { get; set; } = true;
}

public class PlexOptions
{
    public bool Enabled { get; set; } = false;
    public string Url { get; set; } = "";
    public string Token { get; set; } = "";
    public bool Delete { get; set; } = true;
}

public class RadarrOptions
{
    public bool Enabled { get; set; } = true;
    public string Url { get; set; } = "";
    public string ApiKey { get; set; } = "";
    public bool OnlyDeleteFiles { get; set; } = false;
    public AgeDetectionMethod DetermineAgeBy { get; set; } = AgeDetectionMethod.MostRecent;
    public List<int> TagsToExclude { get; set; } = new();
    public List<int> TagsToInclude { get; set; } = new();
}

public class SonarrOptions
{
    public bool Enabled { get; set; } = true;
    public string Url { get; set; } = "";
    public string ApiKey { get; set; } = "";
    public bool DeleteEmptyShows { get; set; } = true;
    public AgeDetectionMethod DetermineAgeBy { get; set; } = AgeDetectionMethod.MostRecent;
    public List<int> TagsToExclude { get; set; } = new();
    public List<int> TagsToInclude { get; set; } = new();
}

public class BazarrOptions
{
    public bool Enabled { get; set; } = false;
    public string Url { get; set; } = "";
    public string ApiKey { get; set; } = "";
}

public class JellystatOptions
{
    public bool Enabled { get; set; } = false;
    public bool WholeTvShow { get; set; } = true;
    public string Url { get; set; } = "";
    public string ApiKey { get; set; } = "";
}

public class WebhookOptions
{
    public bool Enabled { get; set; } = false;
    public int RetryAttempts { get; set; } = 3;
    public int TimeoutSeconds { get; set; } = 30;
    public List<WebhookEndpoint> Endpoints { get; set; } = new();
}

public class WebhookEndpoint
{
    public string Name { get; set; } = "";
    public string Url { get; set; } = "";
    public List<WebhookEvent> Events { get; set; } = new();
    public List<CleanupType> CleanupTypes { get; set; } = new();
    public Dictionary<string, string> Headers { get; set; } = new();
    public string Secret { get; set; } = "";
    public bool Enabled { get; set; } = true;
    public WebhookAuthType AuthType { get; set; } = WebhookAuthType.None;
    public string AuthToken { get; set; } = "";
    public string AuthUsername { get; set; } = "";
    public string AuthPassword { get; set; } = "";
}

public enum WebhookEvent
{
    MediaMarkedForDeletion,
    MediaDeleted,
    CleanupStarted,
    CleanupCompleted,
    MediaMoved,
    MediaUnmonitored,
    HealthCheck
}

public enum WebhookAuthType
{
    None,
    Bearer,
    Basic,
    ApiKey
}

public enum LeavingSoonType
{
    Movies,
    Tv,
    MoviesAndTv
}

public enum AgeDetectionMethod
{
    MostRecent,
    Oldest
}

/// <summary>
/// Scheduling configuration for automated cleanup tasks
/// </summary>
public class SchedulingOptions
{
    public bool Enabled { get; set; } = true;
    public string DailyRunTime { get; set; } = "02:00"; // 2 AM by default
    public bool RunOnStartup { get; set; } = false;
}

// Legacy classes for backward compatibility
[Obsolete("Use individual client options instead")]
public class MediaServerOptions
{
    public string Type { get; set; } = "";
    public string Url { get; set; } = "";
    public string ApiKey { get; set; } = "";
    public string Username { get; set; } = "";
    public string Password { get; set; } = "";
    public int UserId { get; set; } = 0;
    public bool SkipFreeSpace { get; set; } = false;
    public double FreeSpaceThreshold { get; set; } = 0.05;
}

[Obsolete("Use MediaDeletion, TagBasedDeletion, and EpisodeDeletion options instead")]
public class CleanupOptions
{
    public string Schedule { get; set; } = "0 */4 * * *";
    public bool EnableMovieCleanup { get; set; } = false;
    public bool EnableSeasonCleanup { get; set; } = false;
    public bool EnableWeeklyEpisodeCleanup { get; set; } = false;
    public bool EnableTagBasedCleanup { get; set; } = false;
    public int MinimumFreeSpacePercent { get; set; } = 5;
    public int MovieDeletionDelayDays { get; set; } = 7;
    public int SeasonDeletionDelayDays { get; set; } = 14;
    public int EpisodeDeletionDelayDays { get; set; } = 3;
}
