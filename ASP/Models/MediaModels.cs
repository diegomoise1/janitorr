using System.Text.Json.Serialization;

namespace JanitorAspNet.Models;

/// <summary>
/// Represents a media item for cleanup operations
/// Transpiled from MediaItem.kt and related classes
/// </summary>
public record MediaItem
{
    public int Id { get; init; }
    public string Title { get; init; } = "";
    public LibraryType LibraryType { get; init; }
    public string? ImdbId { get; init; }
    public int? TmdbId { get; init; }
    public string ParentPath { get; init; } = "";
    public string OriginalPath { get; init; } = "";
    public int? Season { get; init; }
    public List<string> Tags { get; init; } = new();
    public DateTime? ImportedDate { get; init; }
    public DateTime? LastSeen { get; init; }
    public DateTime? HistoryAge { get; init; }
    public bool Seeding { get; init; }
}

public record RequestResponse
{
    public int Id { get; init; }
    public string Type { get; init; } = "";
    public string Status { get; init; } = "";
    public DateTime CreatedAt { get; init; }
    public DateTime UpdatedAt { get; init; }
    public MediaInfo Media { get; init; } = new();
}

public record MediaInfo
{
    public int Id { get; init; }
    public string Title { get; init; } = "";
    public string? ImdbId { get; init; }
    public int? TmdbId { get; init; }
    public int? TvdbId { get; init; }
    public string MediaType { get; init; } = "";
}

public record MediaDetails
{
    public int Id { get; init; }
    public MediaType MediaType { get; init; }
    public string? ImdbId { get; init; }
    public int? TmdbId { get; init; }
    public string Title { get; init; } = "";
    public DateTime? ReleaseDate { get; init; }
    public RequestStatus Status { get; init; }
}

public record JellyseerrPage<T>
{
    public List<T> Results { get; init; } = new();
    public int Page { get; init; }
    public int TotalPages { get; init; }
    public int TotalResults { get; init; }
}

public record CleanupStatus
{
    public bool IsRunning { get; init; }
    public DateTime? LastRun { get; init; }
    public DateTime? NextRun { get; init; }
    public int ItemsDeleted { get; init; }
    public long SpaceFreed { get; init; }
}

public record CleanupRequest
{
    public CleanupType Type { get; init; }
    public bool DryRun { get; init; } = true;
}

public enum LibraryType
{
    Movie,
    TvShow,
    Episode
}

public enum MediaType
{
    Movie,
    Tv
}

public enum RequestStatus
{
    PendingApproval,
    Approved,
    Available,
    PartiallyAvailable,
    Processing,
    Failed,
    Declined
}

public enum CleanupType
{
    Movie,
    Season,
    WeeklyEpisode,
    TagBased
}

/// <summary>
/// Summary of cleanup operations across all types
/// </summary>
public record CleanupSummary
{
    public int MoviesDeleted { get; set; }
    public int SeasonsDeleted { get; set; }
    public int EpisodesDeleted { get; set; }
    public int TagBasedDeleted { get; set; }
    public int TotalDeleted { get; set; }
    public DateTime RunTime { get; init; } = DateTime.UtcNow;
}

// Jellyfin/Emby Models
public record VirtualFolder
{
    public string Name { get; init; } = "";
    public List<string> Locations { get; init; } = new();
    public string CollectionType { get; init; } = "";
}

// Bazarr Models
public record BazarrMovie
{
    public int RadarrId { get; init; }
    public string Title { get; init; } = "";
    public List<BazarrSubtitle> Subtitles { get; init; } = new();
}

public record BazarrSubtitle
{
    public int Id { get; init; }
    public string Language { get; init; } = "";
    public string FileName { get; init; } = "";
    public bool IsOriginal { get; init; }
}

public record BazarrSeries
{
    public int SonarrId { get; init; }
    public string Title { get; init; } = "";
    public List<BazarrSubtitle> Subtitles { get; init; } = new();
}

// Statistics Models
public record WatchHistoryItem
{
    public string ItemId { get; init; } = "";
    public DateTime LastWatched { get; init; }
    public double PlaybackPositionTicks { get; init; }
    public string UserId { get; init; } = "";
}

// Enhanced LibraryItem
public record LibraryItem
{
    public string Id { get; init; } = "";
    public string Title { get; init; } = "";
    public string FilePath { get; init; } = "";
    public LibraryType Type { get; init; }
    public bool Seeding { get; init; }
    public DateTime HistoryAge { get; init; }
    public DateTime? LastSeen { get; init; }
    public List<string> ExtraFiles { get; init; } = new();
    public int? Season { get; init; }
    public string RootFolderPath { get; init; } = "";
    public string LibraryPath { get; init; } = "";
    public string? ImdbId { get; init; }
    public int? TmdbId { get; init; }
    public int? TvdbId { get; init; }
    public long? Size { get; init; }
    public List<int> Tags { get; init; } = new();
}

public record WebhookEndpointStatus
{
    public string Name { get; init; } = "";
    public string Url { get; init; } = "";
    public bool Enabled { get; init; }
    public List<string> Events { get; init; } = new();
    public List<CleanupType> CleanupTypes { get; init; } = new();
    public string LastTestResult { get; init; } = "";
    public DateTime? LastTestTime { get; init; }
}

public record WebhookPayload
{
    public string Event { get; init; } = "";
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;
    public object Data { get; init; } = new();
    public string? Signature { get; init; }
    public CleanupType? CleanupType { get; init; }
    public List<MediaItem> Items { get; init; } = new();
    public int? ItemsDeleted { get; init; }
    public long? SpaceFreed { get; init; }
    public string? TestMessage { get; init; }
}

public record CleanupResult
{
    public CleanupType CleanupType { get; set; }
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public TimeSpan Duration { get; set; }
    public int ItemsFound { get; set; }
    public int ItemsDeleted { get; set; }
    public bool Success { get; set; }
    public string? Error { get; set; }
}
