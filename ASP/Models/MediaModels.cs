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
    public MediaType MediaType { get; init; }
    public RequestStatus Status { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime UpdatedAt { get; init; }
    public MediaDetails Media { get; init; } = new();
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
    Movies,
    Shows,
    Episodes
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
