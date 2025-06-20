using JanitorAspNet.Configuration;
using JanitorAspNet.Models;
using Microsoft.Extensions.Options;

namespace JanitorAspNet.Services;

/// <summary>
/// Background service for scheduled cleanup tasks
/// </summary>
public class CleanupBackgroundService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IOptionsMonitor<ApplicationOptions> _optionsMonitor;
    private readonly ILogger<CleanupBackgroundService> _logger;

    public CleanupBackgroundService(
        IServiceProvider serviceProvider,
        IOptionsMonitor<ApplicationOptions> optionsMonitor,
        ILogger<CleanupBackgroundService> logger)
    {
        _serviceProvider = serviceProvider;
        _optionsMonitor = optionsMonitor;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Cleanup background service started");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var options = _optionsMonitor.CurrentValue;
                
                // Skip if scheduling is disabled
                if (!options.Scheduling.Enabled)
                {
                    await Task.Delay(TimeSpan.FromMinutes(10), stoppingToken); // Check every 10 minutes
                    continue;
                }

                var now = DateTime.Now;
                var nextRun = GetNextScheduledRun(options.Scheduling, now);

                if (nextRun <= now)
                {
                    await RunScheduledCleanup(options);
                }

                // Wait until the next minute to check again
                var delay = TimeSpan.FromMinutes(1);
                await Task.Delay(delay, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in cleanup background service");
                await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken); // Wait 5 minutes on error
            }
        }

        _logger.LogInformation("Cleanup background service stopped");
    }

    private async Task RunScheduledCleanup(ApplicationOptions options)
    {
        using var scope = _serviceProvider.CreateScope();
        var cleanupService = scope.ServiceProvider.GetRequiredService<ICleanupService>();

        _logger.LogInformation("Running scheduled cleanup");

        try
        {
            // Run different cleanup types based on configuration
            if (options.MediaDeletion.Enabled)
            {
                await cleanupService.RunCleanupAsync(CleanupType.Movie, dryRun: false);
                await cleanupService.RunCleanupAsync(CleanupType.Season, dryRun: false);
            }

            if (options.EpisodeDeletion.Enabled)
            {
                await cleanupService.RunCleanupAsync(CleanupType.WeeklyEpisode, dryRun: false);
            }

            if (options.TagBasedDeletion.Enabled)
            {
                await cleanupService.RunCleanupAsync(CleanupType.TagBased, dryRun: false);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during scheduled cleanup");
        }
    }

    private DateTime GetNextScheduledRun(SchedulingOptions scheduling, DateTime currentTime)
    {
        var scheduledTime = TimeOnly.Parse(scheduling.DailyRunTime);
        var today = DateOnly.FromDateTime(currentTime);
        var todayAtScheduledTime = today.ToDateTime(scheduledTime);

        // If we've already passed today's scheduled time, schedule for tomorrow
        if (currentTime >= todayAtScheduledTime)
        {
            return today.AddDays(1).ToDateTime(scheduledTime);
        }

        return todayAtScheduledTime;
    }

    private Task RunMediaCleanupAsync()
    {
        // TODO: Implement media cleanup logic
        return Task.CompletedTask;
    }

    private Task RunTagBasedCleanupAsync()
    {
        // TODO: Implement tag-based cleanup logic
        return Task.CompletedTask;
    }

    private Task RunEpisodeCleanupAsync()
    {
        // TODO: Implement episode cleanup logic
        return Task.CompletedTask;
    }

    private Task SendHealthCheckWebhooksAsync()
    {
        // TODO: Implement health check webhook logic
        return Task.CompletedTask;
    }
}
