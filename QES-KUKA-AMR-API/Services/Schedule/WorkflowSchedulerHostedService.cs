using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using QES_KUKA_AMR_API.Data;
using QES_KUKA_AMR_API.Data.Entities;
using QES_KUKA_AMR_API.Models.Missions;
using QES_KUKA_AMR_API.Services.Queue;
using QES_KUKA_AMR_API.Services.SavedCustomMissions;

namespace QES_KUKA_AMR_API.Services.Schedule;

/// <summary>
/// Background service that checks for and executes due workflow schedules
/// </summary>
public class WorkflowSchedulerHostedService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<WorkflowSchedulerHostedService> _logger;

    // Check for due schedules every 30 seconds
    private readonly TimeSpan _checkInterval = TimeSpan.FromSeconds(30);

    // Initial delay before starting (allow app to fully initialize)
    private readonly TimeSpan _initialDelay = TimeSpan.FromSeconds(15);

    public WorkflowSchedulerHostedService(
        IServiceProvider serviceProvider,
        ILogger<WorkflowSchedulerHostedService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Workflow Scheduler starting...");

        // Wait for application to fully start
        await Task.Delay(_initialDelay, stoppingToken);

        _logger.LogInformation("Workflow Scheduler started. Checking for due schedules every {Interval} seconds.",
            _checkInterval.TotalSeconds);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessDueSchedulesAsync(stoppingToken);
            }
            catch (OperationCanceledException)
            {
                // Expected when stopping
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in Workflow Scheduler cycle");
            }

            await Task.Delay(_checkInterval, stoppingToken);
        }

        _logger.LogInformation("Workflow Scheduler stopped.");
    }

    private async Task ProcessDueSchedulesAsync(CancellationToken ct)
    {
        using var scope = _serviceProvider.CreateScope();

        var scheduleService = scope.ServiceProvider.GetRequiredService<IWorkflowScheduleService>();
        var queueService = scope.ServiceProvider.GetRequiredService<IMissionQueueService>();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var timeProvider = scope.ServiceProvider.GetRequiredService<TimeProvider>();

        var now = timeProvider.GetUtcNow().UtcDateTime;
        var dueSchedules = await scheduleService.GetDueSchedulesAsync(now, ct);
        var scheduleList = dueSchedules.ToList();

        if (scheduleList.Count == 0)
        {
            return;
        }

        _logger.LogInformation("Found {Count} due workflow schedule(s) to execute", scheduleList.Count);

        foreach (var schedule in scheduleList)
        {
            await ExecuteScheduleAsync(schedule, scheduleService, queueService, dbContext, timeProvider, ct);
        }
    }

    private async Task ExecuteScheduleAsync(
        WorkflowSchedule schedule,
        IWorkflowScheduleService scheduleService,
        IMissionQueueService queueService,
        ApplicationDbContext dbContext,
        TimeProvider timeProvider,
        CancellationToken ct)
    {
        try
        {
            _logger.LogInformation(
                "Adding scheduled workflow to queue: Schedule '{ScheduleName}' (ID: {ScheduleId}) -> Mission '{MissionName}' (ID: {MissionId})",
                schedule.ScheduleName, schedule.Id,
                schedule.SavedMission?.MissionName ?? "Unknown", schedule.SavedMissionId);

            // Get the saved mission
            var savedMission = await dbContext.SavedCustomMissions
                .AsNoTracking()
                .FirstOrDefaultAsync(m => m.Id == schedule.SavedMissionId, ct);

            if (savedMission == null)
            {
                _logger.LogWarning("SavedMission {Id} not found for schedule {ScheduleId}",
                    schedule.SavedMissionId, schedule.Id);
                await scheduleService.UpdateAfterExecutionAsync(schedule.Id, false, "Saved mission not found", ct);
                return;
            }

            // Check if SkipIfRunning is enabled and there are active instances
            if (schedule.SkipIfRunning)
            {
                var activeCount = await queueService.GetActiveInstanceCountAsync(schedule.SavedMissionId, ct);
                if (activeCount > 0)
                {
                    _logger.LogInformation(
                        "Skipping scheduled workflow: Schedule '{ScheduleName}' (ID: {ScheduleId}) - " +
                        "{ActiveCount} active instance(s) of mission '{MissionName}' already running",
                        schedule.ScheduleName, schedule.Id, activeCount,
                        schedule.SavedMission?.MissionName ?? "Unknown");

                    await scheduleService.UpdateAfterSkippedAsync(schedule.Id, activeCount, ct);
                    return;
                }
            }

            // Generate unique IDs
            var timestamp = timeProvider.GetUtcNow().UtcDateTime.ToString("yyyyMMddHHmmss");
            var requestId = $"sched{schedule.Id}_{timestamp}";
            var missionCode = $"sched{schedule.Id}_{timestamp}";

            // Parse mission steps from JSON
            List<MissionDataItem>? missionData = null;
            if (!string.IsNullOrWhiteSpace(savedMission.MissionStepsJson))
            {
                try
                {
                    missionData = JsonSerializer.Deserialize<List<MissionDataItem>>(
                        savedMission.MissionStepsJson,
                        new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                }
                catch (JsonException ex)
                {
                    _logger.LogError(ex, "Failed to deserialize mission steps JSON for saved mission {Id}", savedMission.Id);
                    await scheduleService.UpdateAfterExecutionAsync(schedule.Id, false, "Invalid mission steps JSON", ct);
                    return;
                }
            }

            // Parse comma-separated robot models and IDs
            List<string>? robotModels = null;
            List<string>? robotIds = null;

            if (!string.IsNullOrWhiteSpace(savedMission.RobotModels))
            {
                robotModels = savedMission.RobotModels.Split(',')
                    .Select(s => s.Trim())
                    .Where(s => !string.IsNullOrEmpty(s))
                    .ToList();
            }

            if (!string.IsNullOrWhiteSpace(savedMission.RobotIds))
            {
                robotIds = savedMission.RobotIds.Split(',')
                    .Select(s => s.Trim())
                    .Where(s => !string.IsNullOrEmpty(s))
                    .ToList();
            }

            // Build the submission request
            var submitRequest = new SubmitMissionRequest
            {
                OrgId = savedMission.OrgId ?? string.Empty,
                RequestId = requestId,
                MissionCode = missionCode,
                MissionType = savedMission.MissionType,
                ViewBoardType = savedMission.ViewBoardType ?? string.Empty,
                RobotModels = (IReadOnlyList<string>?)robotModels ?? Array.Empty<string>(),
                RobotIds = (IReadOnlyList<string>?)robotIds ?? Array.Empty<string>(),
                RobotType = savedMission.RobotType,
                Priority = savedMission.Priority,
                ContainerModelCode = savedMission.ContainerModelCode ?? string.Empty,
                ContainerCode = savedMission.ContainerCode ?? string.Empty,
                TemplateCode = savedMission.TemplateCode ?? string.Empty,
                LockRobotAfterFinish = savedMission.LockRobotAfterFinish,
                UnlockRobotId = savedMission.UnlockRobotId ?? string.Empty,
                UnlockMissionCode = savedMission.UnlockMissionCode ?? string.Empty,
                IdleNode = savedMission.IdleNode ?? string.Empty,
                MissionData = missionData
            };

            // Create queue item
            var queueItem = new MissionQueue
            {
                MissionCode = missionCode,
                RequestId = requestId,
                SavedMissionId = savedMission.Id,
                MissionName = $"[Scheduled] {savedMission.MissionName}",
                MissionRequestJson = JsonSerializer.Serialize(submitRequest),
                Status = MissionQueueStatus.Queued,
                Priority = savedMission.Priority,
                CreatedBy = $"Scheduler:{schedule.ScheduleName}",
                RobotTypeFilter = savedMission.RobotType,
                PreferredRobotIds = savedMission.RobotIds
            };

            // Add to queue
            await queueService.AddToQueueAsync(queueItem, ct);

            _logger.LogInformation(
                "Scheduled workflow added to queue: {ScheduleName} -> MissionCode: {MissionCode}, QueueId: {QueueId}",
                schedule.ScheduleName, missionCode, queueItem.Id);

            await scheduleService.UpdateAfterExecutionAsync(schedule.Id, true, null, ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Failed to queue scheduled workflow: Schedule '{ScheduleName}' (ID: {ScheduleId})",
                schedule.ScheduleName, schedule.Id);

            try
            {
                await scheduleService.UpdateAfterExecutionAsync(schedule.Id, false, ex.Message, ct);
            }
            catch (Exception updateEx)
            {
                _logger.LogError(updateEx,
                    "Failed to update schedule {ScheduleId} after execution error",
                    schedule.Id);
            }
        }
    }
}
