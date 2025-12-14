using Cronos;
using Microsoft.EntityFrameworkCore;
using QES_KUKA_AMR_API.Data;
using QES_KUKA_AMR_API.Data.Entities;
using QES_KUKA_AMR_API.Models.Missions;
using QES_KUKA_AMR_API.Models.Schedule;
using QES_KUKA_AMR_API.Services.Queue;
using QES_KUKA_AMR_API.Services.SavedCustomMissions;
using System.Text.Json;

namespace QES_KUKA_AMR_API.Services.Schedule;

/// <summary>
/// Service implementation for managing workflow schedules
/// </summary>
public class WorkflowScheduleService : IWorkflowScheduleService
{
    private readonly ApplicationDbContext _dbContext;
    private readonly TimeProvider _timeProvider;
    private readonly ILogger<WorkflowScheduleService> _logger;
    private readonly IMissionQueueService _queueService;

    public WorkflowScheduleService(
        ApplicationDbContext dbContext,
        TimeProvider timeProvider,
        ILogger<WorkflowScheduleService> logger,
        IMissionQueueService queueService)
    {
        _dbContext = dbContext;
        _timeProvider = timeProvider;
        _logger = logger;
        _queueService = queueService;
    }

    public async Task<IEnumerable<WorkflowScheduleDto>> GetAllAsync(CancellationToken ct = default)
    {
        var schedules = await _dbContext.WorkflowSchedules
            .AsNoTracking()
            .Include(s => s.SavedMission)
            .OrderBy(s => s.ScheduleName)
            .ToListAsync(ct);

        return schedules.Select(MapToDto);
    }

    public async Task<WorkflowScheduleDto?> GetByIdAsync(int id, CancellationToken ct = default)
    {
        var schedule = await _dbContext.WorkflowSchedules
            .AsNoTracking()
            .Include(s => s.SavedMission)
            .FirstOrDefaultAsync(s => s.Id == id, ct);

        return schedule != null ? MapToDto(schedule) : null;
    }

    public async Task<IEnumerable<WorkflowScheduleDto>> GetByMissionIdAsync(int missionId, CancellationToken ct = default)
    {
        var schedules = await _dbContext.WorkflowSchedules
            .AsNoTracking()
            .Include(s => s.SavedMission)
            .Where(s => s.SavedMissionId == missionId)
            .OrderBy(s => s.ScheduleName)
            .ToListAsync(ct);

        return schedules.Select(MapToDto);
    }

    public async Task<WorkflowScheduleDto> CreateAsync(
        CreateWorkflowScheduleRequest request,
        string createdBy,
        CancellationToken ct = default)
    {
        // Validate SavedMission exists
        var savedMission = await _dbContext.SavedCustomMissions
            .AsNoTracking()
            .FirstOrDefaultAsync(m => m.Id == request.SavedMissionId, ct);

        if (savedMission == null)
        {
            throw new InvalidOperationException($"SavedCustomMission with ID {request.SavedMissionId} not found.");
        }

        // Validate schedule type parameters
        ValidateScheduleParameters(request.ScheduleType, request.OneTimeUtc, request.IntervalMinutes, request.CronExpression);

        var now = _timeProvider.GetUtcNow().UtcDateTime;
        var schedule = new WorkflowSchedule
        {
            SavedMissionId = request.SavedMissionId,
            ScheduleName = request.ScheduleName,
            Description = request.Description,
            ScheduleType = request.ScheduleType,
            OneTimeUtc = request.OneTimeUtc,
            IntervalMinutes = request.IntervalMinutes,
            CronExpression = request.CronExpression,
            IsEnabled = request.IsEnabled,
            MaxExecutions = request.MaxExecutions,
            SkipIfRunning = request.SkipIfRunning,
            CreatedBy = createdBy,
            CreatedUtc = now,
            NextRunUtc = request.IsEnabled ? CalculateNextRun(request.ScheduleType, request.OneTimeUtc, request.IntervalMinutes, request.CronExpression, now) : null
        };

        _dbContext.WorkflowSchedules.Add(schedule);
        await _dbContext.SaveChangesAsync(ct);

        // Reload with navigation property
        schedule.SavedMission = savedMission;

        _logger.LogInformation("Created workflow schedule {ScheduleId} '{ScheduleName}' for mission {MissionId}",
            schedule.Id, schedule.ScheduleName, schedule.SavedMissionId);

        return MapToDto(schedule);
    }

    public async Task<WorkflowScheduleDto> UpdateAsync(
        int id,
        UpdateWorkflowScheduleRequest request,
        CancellationToken ct = default)
    {
        var schedule = await _dbContext.WorkflowSchedules
            .Include(s => s.SavedMission)
            .FirstOrDefaultAsync(s => s.Id == id, ct);

        if (schedule == null)
        {
            throw new InvalidOperationException($"WorkflowSchedule with ID {id} not found.");
        }

        var now = _timeProvider.GetUtcNow().UtcDateTime;

        // Update only provided fields
        if (request.ScheduleName != null) schedule.ScheduleName = request.ScheduleName;
        if (request.Description != null) schedule.Description = request.Description;
        if (request.ScheduleType != null) schedule.ScheduleType = request.ScheduleType;
        if (request.OneTimeUtc.HasValue) schedule.OneTimeUtc = request.OneTimeUtc;
        if (request.IntervalMinutes.HasValue) schedule.IntervalMinutes = request.IntervalMinutes;
        if (request.CronExpression != null) schedule.CronExpression = request.CronExpression;
        if (request.MaxExecutions.HasValue) schedule.MaxExecutions = request.MaxExecutions;
        if (request.SkipIfRunning.HasValue) schedule.SkipIfRunning = request.SkipIfRunning.Value;

        // Handle enabled state change
        if (request.IsEnabled.HasValue)
        {
            schedule.IsEnabled = request.IsEnabled.Value;
        }

        // Validate and recalculate next run
        ValidateScheduleParameters(schedule.ScheduleType, schedule.OneTimeUtc, schedule.IntervalMinutes, schedule.CronExpression);

        if (schedule.IsEnabled)
        {
            schedule.NextRunUtc = CalculateNextRun(schedule.ScheduleType, schedule.OneTimeUtc, schedule.IntervalMinutes, schedule.CronExpression, now);
        }
        else
        {
            schedule.NextRunUtc = null;
        }

        schedule.UpdatedUtc = now;

        await _dbContext.SaveChangesAsync(ct);

        _logger.LogInformation("Updated workflow schedule {ScheduleId} '{ScheduleName}'",
            schedule.Id, schedule.ScheduleName);

        return MapToDto(schedule);
    }

    public async Task DeleteAsync(int id, CancellationToken ct = default)
    {
        var schedule = await _dbContext.WorkflowSchedules.FindAsync(new object[] { id }, ct);

        if (schedule == null)
        {
            throw new InvalidOperationException($"WorkflowSchedule with ID {id} not found.");
        }

        _dbContext.WorkflowSchedules.Remove(schedule);
        await _dbContext.SaveChangesAsync(ct);

        _logger.LogInformation("Deleted workflow schedule {ScheduleId} '{ScheduleName}'",
            schedule.Id, schedule.ScheduleName);
    }

    public async Task<WorkflowScheduleDto> ToggleEnabledAsync(int id, bool enabled, CancellationToken ct = default)
    {
        var schedule = await _dbContext.WorkflowSchedules
            .Include(s => s.SavedMission)
            .FirstOrDefaultAsync(s => s.Id == id, ct);

        if (schedule == null)
        {
            throw new InvalidOperationException($"WorkflowSchedule with ID {id} not found.");
        }

        var now = _timeProvider.GetUtcNow().UtcDateTime;
        schedule.IsEnabled = enabled;
        schedule.UpdatedUtc = now;

        if (enabled)
        {
            schedule.NextRunUtc = CalculateNextRun(schedule.ScheduleType, schedule.OneTimeUtc, schedule.IntervalMinutes, schedule.CronExpression, now);
        }
        else
        {
            schedule.NextRunUtc = null;
        }

        await _dbContext.SaveChangesAsync(ct);

        _logger.LogInformation("Toggled workflow schedule {ScheduleId} '{ScheduleName}' to {Enabled}",
            schedule.Id, schedule.ScheduleName, enabled ? "enabled" : "disabled");

        return MapToDto(schedule);
    }

    public async Task<ScheduleTriggerResult> TriggerNowAsync(int id, string triggeredBy, CancellationToken ct = default)
    {
        var schedule = await _dbContext.WorkflowSchedules
            .Include(s => s.SavedMission)
            .FirstOrDefaultAsync(s => s.Id == id, ct);

        if (schedule == null)
        {
            return new ScheduleTriggerResult
            {
                Success = false,
                ErrorMessage = $"WorkflowSchedule with ID {id} not found."
            };
        }

        var savedMission = schedule.SavedMission;
        if (savedMission == null)
        {
            return new ScheduleTriggerResult
            {
                Success = false,
                ErrorMessage = $"SavedCustomMission with ID {schedule.SavedMissionId} not found."
            };
        }

        try
        {
            // Generate unique IDs
            var now = _timeProvider.GetUtcNow().UtcDateTime;
            var timestamp = now.ToString("yyyyMMddHHmmss");
            var requestId = $"manual{schedule.Id}_{timestamp}";
            var missionCode = $"manual{schedule.Id}_{timestamp}";

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
                    return new ScheduleTriggerResult
                    {
                        Success = false,
                        ErrorMessage = "Invalid mission steps JSON"
                    };
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
                MissionName = $"[Manual] {savedMission.MissionName}",
                MissionRequestJson = JsonSerializer.Serialize(submitRequest),
                Status = MissionQueueStatus.Queued,
                Priority = savedMission.Priority,
                CreatedBy = triggeredBy,
                RobotTypeFilter = savedMission.RobotType,
                PreferredRobotIds = savedMission.RobotIds
            };

            // Add to queue
            await _queueService.AddToQueueAsync(queueItem, ct);

            _logger.LogInformation(
                "Manually triggered schedule {ScheduleId} '{ScheduleName}' -> Added to queue with MissionCode: {MissionCode}, QueueId: {QueueId}",
                schedule.Id, schedule.ScheduleName, missionCode, queueItem.Id);

            return new ScheduleTriggerResult
            {
                Success = true,
                MissionCode = missionCode,
                RequestId = requestId,
                ErrorMessage = null
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to trigger schedule {ScheduleId} '{ScheduleName}'",
                schedule.Id, schedule.ScheduleName);

            return new ScheduleTriggerResult
            {
                Success = false,
                ErrorMessage = ex.Message
            };
        }
    }

    public async Task<IEnumerable<WorkflowSchedule>> GetDueSchedulesAsync(DateTime now, CancellationToken ct = default)
    {
        return await _dbContext.WorkflowSchedules
            .Include(s => s.SavedMission)
            .Where(s => s.IsEnabled && s.NextRunUtc != null && s.NextRunUtc <= now)
            .ToListAsync(ct);
    }

    public async Task UpdateAfterExecutionAsync(int id, bool success, string? errorMessage, CancellationToken ct = default)
    {
        var schedule = await _dbContext.WorkflowSchedules.FindAsync(new object[] { id }, ct);
        if (schedule == null) return;

        var now = _timeProvider.GetUtcNow().UtcDateTime;

        schedule.LastRunUtc = now;
        schedule.LastRunStatus = success ? "Success" : "Failed";
        schedule.LastErrorMessage = errorMessage;
        schedule.ExecutionCount++;
        schedule.UpdatedUtc = now;

        // Check if max executions reached
        if (schedule.MaxExecutions.HasValue && schedule.ExecutionCount >= schedule.MaxExecutions.Value)
        {
            schedule.IsEnabled = false;
            schedule.NextRunUtc = null;
            _logger.LogInformation("Schedule {ScheduleId} reached max executions ({Count}) and has been disabled",
                schedule.Id, schedule.MaxExecutions.Value);
        }
        else if (schedule.IsEnabled)
        {
            // Calculate next run time
            schedule.NextRunUtc = CalculateNextRun(schedule.ScheduleType, schedule.OneTimeUtc, schedule.IntervalMinutes, schedule.CronExpression, now);

            // For OneTime schedules, disable after execution
            if (schedule.ScheduleType == "OneTime")
            {
                schedule.IsEnabled = false;
                schedule.NextRunUtc = null;
            }
        }

        await _dbContext.SaveChangesAsync(ct);
    }

    public async Task UpdateAfterSkippedAsync(int id, int activeInstanceCount, CancellationToken ct = default)
    {
        var schedule = await _dbContext.WorkflowSchedules.FindAsync(new object[] { id }, ct);
        if (schedule == null) return;

        var now = _timeProvider.GetUtcNow().UtcDateTime;

        schedule.LastRunUtc = now;
        schedule.LastRunStatus = "Skipped";
        schedule.LastErrorMessage = $"Skipped: {activeInstanceCount} active instance(s) already running";
        // Note: ExecutionCount is NOT incremented for skipped executions
        schedule.UpdatedUtc = now;

        // Still calculate next run time (schedule continues normally)
        if (schedule.IsEnabled)
        {
            schedule.NextRunUtc = CalculateNextRun(
                schedule.ScheduleType,
                schedule.OneTimeUtc,
                schedule.IntervalMinutes,
                schedule.CronExpression,
                now);

            // For OneTime schedules, disable after being skipped (as the scheduled time has passed)
            if (schedule.ScheduleType == "OneTime")
            {
                schedule.IsEnabled = false;
                schedule.NextRunUtc = null;
            }
        }

        await _dbContext.SaveChangesAsync(ct);

        _logger.LogInformation(
            "Schedule {ScheduleId} skipped (SkipIfRunning), next run: {NextRun}",
            schedule.Id, schedule.NextRunUtc);
    }

    #region Private Helpers

    private void ValidateScheduleParameters(string scheduleType, DateTime? oneTimeUtc, int? intervalMinutes, string? cronExpression)
    {
        switch (scheduleType)
        {
            case "OneTime":
                if (!oneTimeUtc.HasValue)
                    throw new InvalidOperationException("OneTimeUtc is required for OneTime schedules.");
                break;

            case "Interval":
                if (!intervalMinutes.HasValue || intervalMinutes.Value < 1 || intervalMinutes.Value > 43200)
                    throw new InvalidOperationException("IntervalMinutes (1-43200) is required for Interval schedules.");
                break;

            case "Cron":
                if (string.IsNullOrWhiteSpace(cronExpression))
                    throw new InvalidOperationException("CronExpression is required for Cron schedules.");
                try
                {
                    CronExpression.Parse(cronExpression);
                }
                catch (Exception ex)
                {
                    throw new InvalidOperationException($"Invalid cron expression: {ex.Message}");
                }
                break;

            default:
                throw new InvalidOperationException($"Invalid schedule type: {scheduleType}");
        }
    }

    private DateTime? CalculateNextRun(string scheduleType, DateTime? oneTimeUtc, int? intervalMinutes, string? cronExpression, DateTime fromUtc)
    {
        switch (scheduleType)
        {
            case "OneTime":
                // Only return if the one-time date is in the future
                return oneTimeUtc > fromUtc ? oneTimeUtc : null;

            case "Interval":
                if (!intervalMinutes.HasValue) return null;
                return fromUtc.AddMinutes(intervalMinutes.Value);

            case "Cron":
                if (string.IsNullOrWhiteSpace(cronExpression)) return null;
                try
                {
                    var cron = CronExpression.Parse(cronExpression);
                    return cron.GetNextOccurrence(fromUtc, TimeZoneInfo.Utc);
                }
                catch
                {
                    return null;
                }

            default:
                return null;
        }
    }

    private static WorkflowScheduleDto MapToDto(WorkflowSchedule schedule)
    {
        return new WorkflowScheduleDto
        {
            Id = schedule.Id,
            SavedMissionId = schedule.SavedMissionId,
            SavedMissionName = schedule.SavedMission?.MissionName ?? string.Empty,
            ScheduleName = schedule.ScheduleName,
            Description = schedule.Description,
            ScheduleType = schedule.ScheduleType,
            OneTimeUtc = schedule.OneTimeUtc,
            IntervalMinutes = schedule.IntervalMinutes,
            CronExpression = schedule.CronExpression,
            IsEnabled = schedule.IsEnabled,
            NextRunUtc = schedule.NextRunUtc,
            LastRunUtc = schedule.LastRunUtc,
            LastRunStatus = schedule.LastRunStatus,
            LastErrorMessage = schedule.LastErrorMessage,
            ExecutionCount = schedule.ExecutionCount,
            MaxExecutions = schedule.MaxExecutions,
            SkipIfRunning = schedule.SkipIfRunning,
            CreatedBy = schedule.CreatedBy,
            CreatedUtc = schedule.CreatedUtc,
            UpdatedUtc = schedule.UpdatedUtc
        };
    }

    #endregion
}
