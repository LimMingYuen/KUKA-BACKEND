using Microsoft.Extensions.Options;
using QES_KUKA_AMR_API.Options;

namespace QES_KUKA_AMR_API.Services;

/// <summary>
/// Background service that automatically processes queued missions at regular intervals
/// </summary>
public class QueueProcessorBackgroundService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<QueueProcessorBackgroundService> _logger;
    private readonly MissionQueueSettings _settings;
    private readonly TimeSpan _processingInterval;

    public QueueProcessorBackgroundService(
        IServiceProvider serviceProvider,
        ILogger<QueueProcessorBackgroundService> logger,
        IOptions<MissionQueueSettings> settings)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
        _settings = settings.Value;
        _processingInterval = TimeSpan.FromSeconds(_settings.AutoProcessIntervalSeconds);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("=== QueueProcessorBackgroundService Starting ===");
        _logger.LogInformation("Processing interval: {Interval} seconds", _settings.AutoProcessIntervalSeconds);
        _logger.LogInformation("Global concurrency limit: {Limit}", _settings.GlobalConcurrencyLimit);

        // Wait a bit before starting to ensure all services are ready
        await Task.Delay(TimeSpan.FromSeconds(2), stoppingToken);

        // CRITICAL: Initialize semaphore state from database before processing
        try
        {
            using var scope = _serviceProvider.CreateScope();
            var queueService = scope.ServiceProvider.GetRequiredService<IQueueService>();
            await queueService.InitializeAsync(stoppingToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "✗ FATAL: Failed to initialize QueueService. Background service will not start.");
            return; // Exit early if initialization fails
        }

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessQueueAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in queue processor background service");
            }

            // Wait for the next processing cycle
            await Task.Delay(_processingInterval, stoppingToken);
        }

        _logger.LogInformation("=== QueueProcessorBackgroundService Stopping ===");
    }

    private async Task ProcessQueueAsync(CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var queueService = scope.ServiceProvider.GetRequiredService<IQueueService>();

        try
        {
            // Get current stats
            var stats = await queueService.GetQueueStatsAsync(cancellationToken);

            // Log current status (only if there are queued or processing missions)
            if (stats.QueuedCount > 0 || stats.ProcessingCount > 0)
            {
                _logger.LogInformation("Queue Status - Queued: {Queued}, Processing: {Processing}/{Limit}, Available: {Available}",
                    stats.QueuedCount, stats.ProcessingCount, stats.TotalSlots, stats.AvailableSlots);
            }

            // Try to process as many queued missions as we have available slots
            var processedCount = 0;
            while (stats.AvailableSlots > 0 && stats.QueuedCount > 0)
            {
                var processed = await queueService.ProcessNextAsync(cancellationToken);
                if (!processed)
                {
                    break; // No more missions to process
                }

                processedCount++;

                // Update stats for next iteration
                stats = await queueService.GetQueueStatsAsync(cancellationToken);
            }

            if (processedCount > 0)
            {
                _logger.LogInformation("✓ Processed {Count} queued mission(s) to execution", processedCount);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing queue");
        }
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("QueueProcessorBackgroundService is stopping");
        await base.StopAsync(cancellationToken);
    }
}
