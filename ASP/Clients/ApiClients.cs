using JanitorAspNet.Models;
using Refit;

namespace JanitorAspNet.Clients;

/// <summary>
/// Jellyseerr API client interface
/// Transpiled from JellyseerrClient.kt
/// </summary>
public interface IJellyseerrClient
{
    [Get("/request?take={pageSize}&skip={offset}")]
    Task<JellyseerrPage<RequestResponse>> GetRequestsAsync(int pageSize, int offset);

    [Get("/request/{id}")]
    Task<RequestResponse> GetRequestAsync(int id);

    [Get("/request/count")]
    Task<int> GetRequestCountAsync();

    [Delete("/request/{id}")]
    Task DeleteRequestAsync(int id);

    [Post("/request/{id}/approve")]
    Task ApproveRequestAsync(int id);

    [Post("/request/{id}/decline")]
    Task DeclineRequestAsync(int id);
}

/// <summary>
/// Radarr API client interface
/// Transpiled from RadarrClient.kt
/// </summary>
public interface IRadarrClient
{
    [Get("/movie")]
    Task<List<RadarrMovie>> GetMoviesAsync();

    [Get("/movie/{id}")]
    Task<RadarrMovie> GetMovieAsync(int id);

    [Delete("/movie/{id}")]
    Task DeleteMovieAsync(int id, [Query] bool deleteFiles = false, [Query] bool addImportExclusion = false);

    [Get("/qualityprofile")]
    Task<List<RadarrQualityProfile>> GetQualityProfilesAsync();

    [Get("/rootfolder")]
    Task<List<RadarrRootFolder>> GetRootFoldersAsync();

    [Get("/moviefile/{id}")]
    Task<RadarrMovieFile> GetMovieFileAsync(int id);

    [Post("/movie/{id}/monitor")]
    Task SetMovieMonitoredAsync(int id, [Body] bool monitored);

    [Delete("/moviefile/{id}")]
    Task DeleteMovieFileAsync(int id);

    [Get("/history")]
    Task<RadarrPage<RadarrHistory>> GetHistoryAsync([Query] int page = 1, [Query] int pageSize = 20, [Query] string? eventType = null);

    [Get("/customformat")]
    Task<List<RadarrCustomFormat>> GetCustomFormatsAsync();

    [Get("/tag")]
    Task<List<RadarrTag>> GetTagsAsync();

    [Get("/diskspace")]
    Task<List<DiskSpace>> GetDiskSpaceAsync();
}

/// <summary>
/// Sonarr API client interface
/// Transpiled from SonarrClient.kt
/// </summary>
public interface ISonarrClient
{
    [Get("/series")]
    Task<List<SonarrSeries>> GetSeriesAsync();

    [Get("/series/{id}")]
    Task<SonarrSeries> GetSeriesAsync(int id);

    [Delete("/series/{id}")]
    Task DeleteSeriesAsync(int id, [Query] bool deleteFiles = false);

    [Get("/episode")]
    Task<List<SonarrEpisode>> GetEpisodesAsync([Query] int? seriesId = null);

    [Get("/qualityprofile")]
    Task<List<SonarrQualityProfile>> GetQualityProfilesAsync();

    [Get("/rootfolder")]
    Task<List<SonarrRootFolder>> GetRootFoldersAsync();

    [Get("/episodefile/{id}")]
    Task<SonarrEpisodeFile> GetEpisodeFileAsync(int id);

    [Post("/series/{id}/monitor")]
    Task SetSeriesMonitoredAsync(int id, [Body] bool monitored);

    [Delete("/episodefile/{id}")]
    Task DeleteEpisodeFileAsync(int id);

    [Get("/history")]
    Task<SonarrPage<SonarrHistory>> GetHistoryAsync([Query] int page = 1, [Query] int pageSize = 20, [Query] string? eventType = null);

    [Get("/tag")]
    Task<List<SonarrTag>> GetTagsAsync();

    [Get("/diskspace")]
    Task<List<DiskSpace>> GetDiskSpaceAsync();
}

// Supporting model classes for the API clients
public record RadarrMovie
{
    public int Id { get; init; }
    public string Title { get; init; } = "";
    public string ImdbId { get; init; } = "";
    public int TmdbId { get; init; }
    public string Path { get; init; } = "";
    public List<int> Tags { get; init; } = new();
    public bool HasFile { get; init; }
    public DateTime Added { get; init; }
}

public record SonarrSeries
{
    public int Id { get; init; }
    public string Title { get; init; } = "";
    public string ImdbId { get; init; } = "";
    public int TvdbId { get; init; }
    public string Path { get; init; } = "";
    public List<int> Tags { get; init; } = new();
    public DateTime Added { get; init; }
}

public record SonarrEpisode
{
    public int Id { get; init; }
    public int SeriesId { get; init; }
    public string Title { get; init; } = "";
    public int SeasonNumber { get; init; }
    public int EpisodeNumber { get; init; }
    public bool HasFile { get; init; }
    public DateTime AirDate { get; init; }
}

public record RadarrHistory
{
    public int Id { get; init; }
    public int MovieId { get; init; }
    public string EventType { get; init; } = "";
    public DateTime Date { get; init; }
}

public record SonarrHistory
{
    public int Id { get; init; }
    public int EpisodeId { get; init; }
    public string EventType { get; init; } = "";
    public DateTime Date { get; init; }
}

public record RadarrTag
{
    public int Id { get; init; }
    public string Label { get; init; } = "";
}

public record SonarrTag
{
    public int Id { get; init; }
    public string Label { get; init; } = "";
}

public record DiskSpace
{
    public string Path { get; init; } = "";
    public string Label { get; init; } = "";
    public long FreeSpace { get; init; }
    public long TotalSpace { get; init; }
}

public record RadarrPage<T>
{
    public List<T> Records { get; init; } = new();
    public int Page { get; init; }
    public int PageSize { get; init; }
    public int TotalRecords { get; init; }
}

public record SonarrPage<T>
{
    public List<T> Records { get; init; } = new();
    public int Page { get; init; }
    public int PageSize { get; init; }
    public int TotalRecords { get; init; }
}

public record RadarrQualityProfile
{
    public int Id { get; init; }
    public string Name { get; init; } = "";
}

public record RadarrRootFolder
{
    public string Path { get; init; } = "";
    public long FreeSpace { get; init; }
}

public record RadarrMovieFile
{
    public int Id { get; init; }
    public string RelativePath { get; init; } = "";
    public long Size { get; init; }
}

public record RadarrCustomFormat
{
    public int Id { get; init; }
    public string Name { get; init; } = "";
}

public record SonarrQualityProfile
{
    public int Id { get; init; }
    public string Name { get; init; } = "";
}

public record SonarrRootFolder
{
    public string Path { get; init; } = "";
    public long FreeSpace { get; init; }
}

public record SonarrEpisodeFile
{
    public int Id { get; init; }
    public string RelativePath { get; init; } = "";
    public long Size { get; init; }
}
