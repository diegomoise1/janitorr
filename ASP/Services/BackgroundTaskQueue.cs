using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace JanitorAspNet.Services
{
    public interface IBackgroundTaskQueue
    {
        Task QueueBackgroundWorkItemAsync(Func<CancellationToken, Task> workItem);
        Task<Func<CancellationToken, Task>> DequeueAsync(CancellationToken cancellationToken);
        int Count { get; }
    }

    public class BackgroundTaskQueue : IBackgroundTaskQueue
    {
        private readonly ConcurrentQueue<Func<CancellationToken, Task>> _workItems = new();
        private readonly SemaphoreSlim _signal = new(0);
        private readonly ILogger<BackgroundTaskQueue> _logger;

        public BackgroundTaskQueue(ILogger<BackgroundTaskQueue> logger)
        {
            _logger = logger;
        }

        public int Count => _workItems.Count;

        public Task QueueBackgroundWorkItemAsync(Func<CancellationToken, Task> workItem)
        {
            if (workItem == null)
            {
                throw new ArgumentNullException(nameof(workItem));
            }

            _workItems.Enqueue(workItem);
            _signal.Release();
            
            _logger.LogDebug("Queued background work item. Queue count: {Count}", _workItems.Count);
            return Task.CompletedTask;
        }

        public async Task<Func<CancellationToken, Task>> DequeueAsync(CancellationToken cancellationToken)
        {
            await _signal.WaitAsync(cancellationToken);
            
            if (_workItems.TryDequeue(out var workItem))
            {
                _logger.LogDebug("Dequeued background work item. Remaining count: {Count}", _workItems.Count);
                return workItem;
            }

            throw new InvalidOperationException("Failed to dequeue work item");
        }
    }

    /// <summary>
    /// Background service that processes queued work items
    /// </summary>
    public class QueuedHostedService : BackgroundService
    {
        private readonly IBackgroundTaskQueue _taskQueue;
        private readonly ILogger<QueuedHostedService> _logger;

        public QueuedHostedService(
            IBackgroundTaskQueue taskQueue,
            ILogger<QueuedHostedService> logger)
        {
            _taskQueue = taskQueue;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Queued hosted service is starting");

            await BackgroundProcessing(stoppingToken);
        }

        private async Task BackgroundProcessing(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    var workItem = await _taskQueue.DequeueAsync(stoppingToken);

                    try
                    {
                        _logger.LogDebug("Executing background work item");
                        await workItem(stoppingToken);
                        _logger.LogDebug("Background work item completed successfully");
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error occurred executing background work item");
                    }
                }
                catch (OperationCanceledException)
                {
                    // Expected when cancellation token is triggered
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error occurred in background processing loop");
                    
                    // Wait a bit before retrying to avoid tight loop
                    await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
                }
            }
        }

        public override async Task StopAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Queued hosted service is stopping");
            await base.StopAsync(stoppingToken);
        }
    }
}
