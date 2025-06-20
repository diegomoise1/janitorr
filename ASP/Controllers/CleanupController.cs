using JanitorAspNet.Models;
using JanitorAspNet.Services;
using JanitorAspNet.Configuration;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace JanitorAspNet.Controllers;

/// <summary>
/// Main controller for Janitor media cleanup operations
/// Transpiled from Kotlin controllers and REST endpoints
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class CleanupController : ControllerBase
{    private readonly ICleanupService _cleanupService;
    private readonly ILogger<CleanupController> _logger;
    private readonly IOptionsMonitor<ApplicationOptions> _optionsMonitor;

    public CleanupController(ICleanupService cleanupService, ILogger<CleanupController> logger, IOptionsMonitor<ApplicationOptions> optionsMonitor)
    {
        _cleanupService = cleanupService;
        _logger = logger;
        _optionsMonitor = optionsMonitor;
    }

    /// <summary>
    /// Get current cleanup status
    /// </summary>
    [HttpGet("status")]
    public async Task<ActionResult<CleanupStatus>> GetStatusAsync()
    {
        try
        {
            var status = await _cleanupService.GetStatusAsync();
            return Ok(status);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get cleanup status");
            return StatusCode(500, "Failed to get cleanup status");
        }
    }

    /// <summary>
    /// Trigger a manual cleanup operation
    /// </summary>
    [HttpPost("run")]
    public async Task<ActionResult<int>> RunCleanupAsync([FromBody] CleanupRequest request)
    {
        try
        {
            _logger.LogInformation("Manual cleanup requested: {Type}, DryRun: {DryRun}", request.Type, request.DryRun);
            
            var deletedCount = await _cleanupService.RunCleanupAsync(request.Type, request.DryRun);
            
            return Ok(new { DeletedCount = deletedCount, Message = request.DryRun ? "Dry run completed" : "Cleanup completed" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to run cleanup");
            return StatusCode(500, "Failed to run cleanup");
        }
    }

    /// <summary>
    /// Get items that would be deleted for a specific cleanup type
    /// </summary>
    [HttpGet("preview/{type}")]
    public async Task<ActionResult<List<MediaItem>>> GetCleanupPreviewAsync(CleanupType type)
    {
        try
        {
            var items = await _cleanupService.GetItemsForCleanupAsync(type);
            return Ok(items);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get cleanup preview for {Type}", type);
            return StatusCode(500, "Failed to get cleanup preview");
        }
    }

    /// <summary>
    /// Run movie cleanup
    /// </summary>
    [HttpPost("movies")]
    public async Task<ActionResult<int>> RunMovieCleanupAsync([FromQuery] bool dryRun = true)
    {
        try
        {
            var deletedCount = await _cleanupService.RunCleanupAsync(CleanupType.Movie, dryRun);
            return Ok(new { DeletedCount = deletedCount, Type = "Movie" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to run movie cleanup");
            return StatusCode(500, "Failed to run movie cleanup");
        }
    }

    /// <summary>
    /// Run season cleanup
    /// </summary>
    [HttpPost("seasons")]
    public async Task<ActionResult<int>> RunSeasonCleanupAsync([FromQuery] bool dryRun = true)
    {
        try
        {
            var deletedCount = await _cleanupService.RunCleanupAsync(CleanupType.Season, dryRun);
            return Ok(new { DeletedCount = deletedCount, Type = "Season" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to run season cleanup");
            return StatusCode(500, "Failed to run season cleanup");
        }
    }

    /// <summary>
    /// Run weekly episode cleanup
    /// </summary>
    [HttpPost("episodes")]
    public async Task<ActionResult<int>> RunEpisodeCleanupAsync([FromQuery] bool dryRun = true)
    {
        try
        {
            var deletedCount = await _cleanupService.RunCleanupAsync(CleanupType.WeeklyEpisode, dryRun);
            return Ok(new { DeletedCount = deletedCount, Type = "WeeklyEpisode" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to run episode cleanup");
            return StatusCode(500, "Failed to run episode cleanup");
        }
    }

    /// <summary>
    /// Run tag-based cleanup
    /// </summary>
    [HttpPost("tag-based")]
    public async Task<ActionResult<int>> RunTagBasedCleanupAsync([FromQuery] bool dryRun = true)
    {
        try
        {
            var deletedCount = await _cleanupService.RunCleanupAsync(CleanupType.TagBased, dryRun);
            return Ok(new { DeletedCount = deletedCount, Type = "TagBased" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to run tag-based cleanup");
            return StatusCode(500, "Failed to run tag-based cleanup");
        }
    }

    /// <summary>
    /// Run all enabled cleanup types instantly
    /// </summary>
    [HttpPost("run-all")]
    public async Task<ActionResult<CleanupSummary>> RunAllCleanupAsync([FromQuery] bool dryRun = true)
    {
        try
        {
            _logger.LogInformation("Running all cleanup types (DryRun: {DryRun})", dryRun);
            
            var summary = new CleanupSummary();
            var options = _optionsMonitor.CurrentValue;

            // Run movie cleanup
            if (options.MediaDeletion.Enabled)
            {
                var movieCount = await _cleanupService.RunCleanupAsync(CleanupType.Movie, dryRun);
                summary.MoviesDeleted = movieCount;
                
                var seasonCount = await _cleanupService.RunCleanupAsync(CleanupType.Season, dryRun);
                summary.SeasonsDeleted = seasonCount;
            }

            // Run episode cleanup
            if (options.EpisodeDeletion.Enabled)
            {
                var episodeCount = await _cleanupService.RunCleanupAsync(CleanupType.WeeklyEpisode, dryRun);
                summary.EpisodesDeleted = episodeCount;
            }

            // Run tag-based cleanup
            if (options.TagBasedDeletion.Enabled)
            {
                var tagBasedCount = await _cleanupService.RunCleanupAsync(CleanupType.TagBased, dryRun);
                summary.TagBasedDeleted = tagBasedCount;
            }

            summary.TotalDeleted = summary.MoviesDeleted + summary.SeasonsDeleted + 
                                 summary.EpisodesDeleted + summary.TagBasedDeleted;

            return Ok(summary);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to run all cleanup types");
            return StatusCode(500, "Failed to run cleanup");
        }
    }
}
