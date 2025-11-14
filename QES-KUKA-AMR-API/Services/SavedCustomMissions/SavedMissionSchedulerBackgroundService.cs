using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace QES_KUKA_AMR_API.Services.SavedCustomMissions;

public class SavedMissionSchedulerBackgroundService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<SavedMissionSchedulerBackgroundService> _logger;
    private readonly TimeProvider _timeProvider;
    private readonly TimeSpan _pollInterval = TimeSpan.FromSeconds(60);
    private const int BatchSize = 5;  // Reduced from 10 to minimize lock contention

    public SavedMissionSchedulerBackgroundService(
        IServiceProvider serviceProvider,
        ILogger<SavedMissionSchedulerBackgroundService> logger,
        TimeProvider timeProvider)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
        _timeProvider = timeProvider;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Saved mission scheduler background service started.");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessDueSchedulesAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                // Graceful shutdown
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error while processing saved mission schedules.");
            }

            try
            {
                await Task.Delay(_pollInterval, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }

        _logger.LogInformation("Saved mission scheduler background service stopped.");
    }

    private async Task ProcessDueSchedulesAsync(CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var scheduleService = scope.ServiceProvider.GetRequiredService<ISavedMissionScheduleService>();

        while (true)
        {
            var utcNow = _timeProvider.GetUtcNow().UtcDateTime;
            var dueSchedules = await scheduleService.GetDueSchedulesAsync(utcNow, BatchSize, cancellationToken);

            if (dueSchedules.Count == 0)
            {
                break;
            }

            foreach (var schedule in dueSchedules)
            {
                cancellationToken.ThrowIfCancellationRequested();

                try
                {
                    await scheduleService.ProcessScheduleAsync(schedule, "Scheduler", cancellationToken);
                }
                catch (OperationCanceledException)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to process saved mission schedule {ScheduleId} for mission {MissionId}",
                        schedule.Id, schedule.SavedMissionId);
                }
            }
        }
    }
}
