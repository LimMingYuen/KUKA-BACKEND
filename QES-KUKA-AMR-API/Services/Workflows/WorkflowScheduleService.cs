using Microsoft.EntityFrameworkCore;
using QES_KUKA_AMR_API.Data;
using QES_KUKA_AMR_API.Data.Entities;
using QES_KUKA_AMR_API.Models.Workflows;
using QES_KUKA_AMR_API.Services;

namespace QES_KUKA_AMR_API.Services.Workflows;

public interface IWorkflowScheduleService
{
    Task<List<WorkflowSchedule>> GetSchedulesAsync(int workflowId, CancellationToken cancellationToken = default);
    Task<WorkflowSchedule?> GetScheduleAsync(int workflowId, int scheduleId, CancellationToken cancellationToken = default);
    Task<WorkflowSchedule> CreateAsync(int workflowId, WorkflowScheduleRequest request, CancellationToken cancellationToken = default);
    Task<WorkflowSchedule?> UpdateAsync(int workflowId, int scheduleId, WorkflowScheduleRequest request, CancellationToken cancellationToken = default);
    Task<bool> DeleteAsync(int workflowId, int scheduleId, CancellationToken cancellationToken = default);
    Task<WorkflowTriggerResult> RunNowAsync(int workflowId, int scheduleId, string triggeredBy, CancellationToken cancellationToken = default);
    Task<List<WorkflowScheduleLog>> GetLogsAsync(int workflowId, int? scheduleId, int take, CancellationToken cancellationToken = default);
    Task<List<WorkflowSchedule>> GetDueSchedulesAsync(DateTime utcNow, int take, CancellationToken cancellationToken = default);
    Task ProcessScheduleAsync(WorkflowSchedule schedule, string triggeredBy, CancellationToken cancellationToken = default);
}

public class WorkflowScheduleService : IWorkflowScheduleService
{
    private readonly ApplicationDbContext _dbContext;
    private readonly IQueueService _queueService;
    private readonly ILogger<WorkflowScheduleService> _logger;
    private readonly TimeProvider _timeProvider;

    public WorkflowScheduleService(
        ApplicationDbContext dbContext,
        IQueueService queueService,
        ILogger<WorkflowScheduleService> logger,
        TimeProvider timeProvider)
    {
        _dbContext = dbContext;
        _queueService = queueService;
        _logger = logger;
        _timeProvider = timeProvider;
    }

    public async Task<List<WorkflowSchedule>> GetSchedulesAsync(int workflowId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.WorkflowSchedules
            .AsNoTracking()
            .Where(s => s.WorkflowId == workflowId)
            .OrderBy(s => s.TriggerType)
            .ThenBy(s => s.NextRunUtc)
            .ToListAsync(cancellationToken);
    }

    public async Task<WorkflowSchedule?> GetScheduleAsync(int workflowId, int scheduleId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.WorkflowSchedules
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.WorkflowId == workflowId && s.Id == scheduleId, cancellationToken);
    }

    public async Task<WorkflowSchedule> CreateAsync(int workflowId, WorkflowScheduleRequest request, CancellationToken cancellationToken = default)
    {
        var workflow = await _dbContext.WorkflowDiagrams
            .AsNoTracking()
            .FirstOrDefaultAsync(w => w.Id == workflowId, cancellationToken);

        if (workflow is null)
        {
            throw new WorkflowNotFoundException($"Workflow with ID {workflowId} was not found.");
        }

        var schedule = new WorkflowSchedule
        {
            WorkflowId = workflowId,
            TriggerType = request.TriggerType,
            CronExpression = request.CronExpression,
            TimezoneId = request.TimezoneId,
            IsEnabled = request.IsEnabled,
            CreatedUtc = _timeProvider.GetUtcNow().UtcDateTime,
            UpdatedUtc = _timeProvider.GetUtcNow().UtcDateTime
        };

        ApplyScheduleTiming(schedule, request, true);

        schedule.NextRunUtc = ComputeNextRunUtc(schedule, _timeProvider.GetUtcNow().UtcDateTime);

        if (schedule.IsEnabled && schedule.NextRunUtc is null)
        {
            throw new WorkflowScheduleValidationException("Unable to determine the next run time for the provided schedule.");
        }

        _dbContext.WorkflowSchedules.Add(schedule);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return schedule;
    }

    public async Task<WorkflowSchedule?> UpdateAsync(int workflowId, int scheduleId, WorkflowScheduleRequest request, CancellationToken cancellationToken = default)
    {
        var schedule = await _dbContext.WorkflowSchedules
            .FirstOrDefaultAsync(s => s.WorkflowId == workflowId && s.Id == scheduleId, cancellationToken);

        if (schedule is null)
        {
            return null;
        }

        schedule.TriggerType = request.TriggerType;
        schedule.CronExpression = request.CronExpression;
        schedule.TimezoneId = request.TimezoneId;
        schedule.IsEnabled = request.IsEnabled;
        schedule.UpdatedUtc = _timeProvider.GetUtcNow().UtcDateTime;

        ApplyScheduleTiming(schedule, request, false);
        schedule.NextRunUtc = ComputeNextRunUtc(schedule, _timeProvider.GetUtcNow().UtcDateTime);

        if (schedule.IsEnabled && schedule.NextRunUtc is null)
        {
            throw new WorkflowScheduleValidationException("Unable to determine the next run time for the provided schedule.");
        }

        await _dbContext.SaveChangesAsync(cancellationToken);

        return schedule;
    }

    public async Task<bool> DeleteAsync(int workflowId, int scheduleId, CancellationToken cancellationToken = default)
    {
        var schedule = await _dbContext.WorkflowSchedules
            .FirstOrDefaultAsync(s => s.WorkflowId == workflowId && s.Id == scheduleId, cancellationToken);

        if (schedule is null)
        {
            return false;
        }

        _dbContext.WorkflowSchedules.Remove(schedule);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<WorkflowTriggerResult> RunNowAsync(int workflowId, int scheduleId, string triggeredBy, CancellationToken cancellationToken = default)
    {
        var schedule = await _dbContext.WorkflowSchedules
            .FirstOrDefaultAsync(s => s.WorkflowId == workflowId && s.Id == scheduleId, cancellationToken);

        if (schedule is null)
        {
            throw new WorkflowNotFoundException($"Schedule {scheduleId} for workflow {workflowId} not found.");
        }

        var utcNow = _timeProvider.GetUtcNow().UtcDateTime;
        var trigger = await ProcessScheduleInternalAsync(schedule, utcNow, triggeredBy, cancellationToken);
        return trigger ?? new WorkflowTriggerResult
        {
            Success = false,
            Message = "Scheduled run failed."
        };
    }

    public async Task<List<WorkflowScheduleLog>> GetLogsAsync(int workflowId, int? scheduleId, int take, CancellationToken cancellationToken = default)
    {
        // Query using denormalized WorkflowId field to avoid JOIN for better performance
        var query = _dbContext.WorkflowScheduleLogs
            .AsNoTracking()
            .Where(l => l.WorkflowId == workflowId);

        if (scheduleId.HasValue)
        {
            query = query.Where(l => l.ScheduleId == scheduleId.Value);
        }

        return await query
            .OrderByDescending(l => l.CreatedUtc)
            .Take(take)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<WorkflowSchedule>> GetDueSchedulesAsync(DateTime utcNow, int take, CancellationToken cancellationToken = default)
    {
        return await _dbContext.WorkflowSchedules
            .Where(s => s.IsEnabled
                     && s.NextRunUtc != null
                     && s.NextRunUtc <= utcNow
                     && s.QueueLockToken == null)  // Exclude schedules already being processed
            .OrderBy(s => s.NextRunUtc)
            .Take(take)
            .ToListAsync(cancellationToken);
    }

    public async Task ProcessScheduleAsync(WorkflowSchedule schedule, string triggeredBy, CancellationToken cancellationToken = default)
    {
        var utcNow = _timeProvider.GetUtcNow().UtcDateTime;
        await ProcessScheduleInternalAsync(schedule, utcNow, triggeredBy, cancellationToken);
    }

    private async Task<string?> TryAcquireScheduleLockAsync(WorkflowSchedule schedule, CancellationToken cancellationToken)
    {
        var lockToken = Guid.NewGuid().ToString("N");
        var utcNow = _timeProvider.GetUtcNow().UtcDateTime;

        var rowsAffected = await _dbContext.WorkflowSchedules
            .Where(s => s.Id == schedule.Id && s.QueueLockToken == null)
            .ExecuteUpdateAsync(
                setters => setters
                    .SetProperty(s => s.QueueLockToken, lockToken)
                    .SetProperty(s => s.UpdatedUtc, utcNow),
                cancellationToken);

        if (rowsAffected == 0)
        {
            return null;
        }

        schedule.QueueLockToken = lockToken;
        schedule.UpdatedUtc = utcNow;

        _logger.LogDebug(
            "Acquired schedule lock {LockToken} for schedule {ScheduleId} (workflow {WorkflowId})",
            lockToken,
            schedule.Id,
            schedule.WorkflowId);

        return lockToken;
    }

    private async Task ReleaseScheduleLockAsync(int scheduleId, string lockToken, CancellationToken cancellationToken)
    {
        var utcNow = _timeProvider.GetUtcNow().UtcDateTime;

        var rowsAffected = await _dbContext.WorkflowSchedules
            .Where(s => s.Id == scheduleId && s.QueueLockToken == lockToken)
            .ExecuteUpdateAsync(
                setters => setters
                    .SetProperty(s => s.QueueLockToken, (string?)null)
                    .SetProperty(s => s.UpdatedUtc, utcNow),
                cancellationToken);

        if (rowsAffected > 0)
        {
            _logger.LogWarning(
                "Released stale schedule lock for schedule {ScheduleId}",
                scheduleId);
        }
    }

    private void ApplyScheduleTiming(WorkflowSchedule schedule, WorkflowScheduleRequest request, bool isCreate)
    {
        var timezone = ResolveTimezone(request.TimezoneId);
        schedule.TimezoneId = timezone.Id;

        if (schedule.TriggerType == WorkflowTriggerType.Once)
        {
            if (request.RunAtLocalTime is null)
            {
                throw new WorkflowScheduleValidationException("RunAtLocalTime must be provided for one-time schedules.");
            }

            var local = DateTime.SpecifyKind(request.RunAtLocalTime.Value, DateTimeKind.Unspecified);
            var runUtc = TimeZoneInfo.ConvertTimeToUtc(local, timezone);
            var now = _timeProvider.GetUtcNow().UtcDateTime;
            if (runUtc <= now.AddMinutes(-1))
            {
                throw new WorkflowScheduleValidationException("Scheduled time must be in the future.");
            }

            schedule.OneTimeRunUtc = runUtc;
            schedule.CronExpression = null;
        }
        else
        {
            if (string.IsNullOrWhiteSpace(request.CronExpression))
            {
                throw new WorkflowScheduleValidationException("Cron expression is required for recurring schedules.");
            }

            ValidateCronExpression(request.CronExpression);

            schedule.CronExpression = request.CronExpression;
            if (isCreate)
            {
                schedule.OneTimeRunUtc = null;
            }
        }
    }

    private DateTime? ComputeNextRunUtc(WorkflowSchedule schedule, DateTime utcNow)
    {
        if (!schedule.IsEnabled)
        {
            return null;
        }

        var timezone = ResolveTimezone(schedule.TimezoneId);

        if (schedule.TriggerType == WorkflowTriggerType.Once)
        {
            return schedule.OneTimeRunUtc;
        }

        if (string.IsNullOrWhiteSpace(schedule.CronExpression))
        {
            return null;
        }

        if (string.Equals(schedule.CronExpression, "@hourly", StringComparison.OrdinalIgnoreCase))
        {
            var localNow = TimeZoneInfo.ConvertTimeFromUtc(utcNow, timezone);
            var baseTime = new DateTime(localNow.Year, localNow.Month, localNow.Day, localNow.Hour, 0, 0);
            var nextLocal = baseTime.AddHours(1);
            return TimeZoneInfo.ConvertTimeToUtc(nextLocal, timezone);
        }

        if (string.Equals(schedule.CronExpression, "@daily", StringComparison.OrdinalIgnoreCase))
        {
            var localNow = TimeZoneInfo.ConvertTimeFromUtc(utcNow, timezone);
            var midnight = new DateTime(localNow.Year, localNow.Month, localNow.Day, 0, 0, 0);
            var nextLocal = midnight.AddDays(1);
            return TimeZoneInfo.ConvertTimeToUtc(nextLocal, timezone);
        }

        var parts = schedule.CronExpression.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var minuteField = parts[0];
        var hourField = parts[1];

        var candidate = TimeZoneInfo.ConvertTimeFromUtc(utcNow, timezone);
        candidate = candidate.AddMinutes(1);
        candidate = new DateTime(candidate.Year, candidate.Month, candidate.Day, candidate.Hour, candidate.Minute, 0);

        for (var i = 0; i < 60 * 24 * 32; i++)
        {
            if (MatchesCronField(minuteField, candidate.Minute, 0, 59) &&
                MatchesCronField(hourField, candidate.Hour, 0, 23))
            {
                return TimeZoneInfo.ConvertTimeToUtc(candidate, timezone);
            }

            candidate = candidate.AddMinutes(1);
        }

        return null;
    }

    private async Task<WorkflowTriggerResult?> ProcessScheduleInternalAsync(
        WorkflowSchedule schedule,
        DateTime scheduledForUtc,
        string triggeredBy,
        CancellationToken cancellationToken)
    {
        var workflowId = schedule.WorkflowId;
        _logger.LogInformation(
            "Processing workflow schedule {ScheduleId} for workflow {WorkflowId} (triggered by {TriggeredBy})",
            schedule.Id,
            workflowId,
            triggeredBy);

        var lockToken = await TryAcquireScheduleLockAsync(schedule, cancellationToken);
        if (lockToken is null)
        {
            _logger.LogInformation(
                "Skipping schedule {ScheduleId} for workflow {WorkflowId} because another worker already holds the lock.",
                schedule.Id,
                workflowId);

            var lockSkipLog = new WorkflowScheduleLog
            {
                ScheduleId = schedule.Id,
                WorkflowId = schedule.WorkflowId,
                ScheduledForUtc = schedule.NextRunUtc ?? scheduledForUtc,
                CreatedUtc = _timeProvider.GetUtcNow().UtcDateTime,
                ResultStatus = "Skipped",
                Error = "Lock not acquired"
            };

            _dbContext.WorkflowScheduleLogs.Add(lockSkipLog);

            schedule.LastStatus = "Skipped (in progress)";
            schedule.LastError = null;
            schedule.UpdatedUtc = _timeProvider.GetUtcNow().UtcDateTime;

            await _dbContext.SaveChangesAsync(cancellationToken);
            return null;
        }

        var releaseLock = true;

        try
        {
            WorkflowScheduleLog logEntry = new()
            {
                ScheduleId = schedule.Id,
                WorkflowId = schedule.WorkflowId,
                ScheduledForUtc = schedule.NextRunUtc ?? scheduledForUtc,
                CreatedUtc = _timeProvider.GetUtcNow().UtcDateTime,
                ResultStatus = "Pending"
            };

            _dbContext.WorkflowScheduleLogs.Add(logEntry);

            var hasActive = await _dbContext.MissionQueues
                .AsNoTracking()
                .AnyAsync(m => m.WorkflowId == workflowId &&
                               (m.Status == QueueStatus.Queued || m.Status == QueueStatus.Processing),
                    cancellationToken);

            if (hasActive)
            {
                _logger.LogInformation(
                    "Skipping schedule {ScheduleId} for workflow {WorkflowId} because an active mission already exists in the queue.",
                    schedule.Id,
                    workflowId);

                schedule.LastRunUtc = scheduledForUtc;
                schedule.LastStatus = "Skipped (already active)";
                schedule.LastError = null;
                schedule.UpdatedUtc = _timeProvider.GetUtcNow().UtcDateTime;
                schedule.NextRunUtc = ComputeNextRunUtc(schedule, _timeProvider.GetUtcNow().UtcDateTime);
                schedule.QueueLockToken = null;

                logEntry.ResultStatus = "Skipped";
                logEntry.Error = "Workflow already active";
                logEntry.EnqueuedUtc = null;
                logEntry.QueueId = null;

                await _dbContext.SaveChangesAsync(cancellationToken);
                releaseLock = false;
                return null;
            }

            WorkflowTriggerResult? triggerResult = null;

            try
            {
                // Get workflow details
                var workflow = await _dbContext.WorkflowDiagrams
                    .AsNoTracking()
                    .FirstOrDefaultAsync(w => w.Id == workflowId, cancellationToken);

                if (workflow is null)
                {
                    throw new WorkflowNotFoundException($"Workflow with ID {workflowId} not found.");
                }

                // Create enqueue request for the workflow
                // Generate timestamp in same format as manual trigger: YYYYMMDDHHmmssSSS
                var now = _timeProvider.GetUtcNow().UtcDateTime;
                var timestamp = $"{now:yyyyMMddHHmmssfff}";

                var enqueueRequest = new EnqueueRequest
                {
                    WorkflowId = workflowId,
                    WorkflowCode = workflow.WorkflowCode,
                    WorkflowName = workflow.WorkflowName,
                    TemplateCode = workflow.WorkflowCode,
                    MissionCode = $"mission{timestamp}",
                    RequestId = $"request{timestamp}",
                    CreatedBy = triggeredBy,
                    Priority = workflow.WorkflowPriority,
                    TriggerSource = MissionTriggerSource.Scheduled
                };

                var queueResult = await _queueService.EnqueueMissionAsync(enqueueRequest, cancellationToken);

                if (!queueResult.Success)
                {
                    throw new InvalidOperationException($"Failed to enqueue workflow: {queueResult.Message}");
                }

                schedule.LastRunUtc = scheduledForUtc;
                schedule.LastStatus = queueResult.Message?.Length > 30
                    ? queueResult.Message.Substring(0, 30)
                    : queueResult.Message;
                schedule.LastError = null;
                schedule.UpdatedUtc = _timeProvider.GetUtcNow().UtcDateTime;

                if (schedule.TriggerType == WorkflowTriggerType.Once)
                {
                    schedule.IsEnabled = false;
                    schedule.NextRunUtc = null;
                }
                else
                {
                    schedule.NextRunUtc = ComputeNextRunUtc(schedule, _timeProvider.GetUtcNow().UtcDateTime);
                }

                schedule.QueueLockToken = null;

                logEntry.EnqueuedUtc = _timeProvider.GetUtcNow().UtcDateTime;
                logEntry.QueueId = queueResult.QueueId;
                logEntry.ResultStatus = "Queued";

                triggerResult = new WorkflowTriggerResult
                {
                    Success = true,
                    Message = queueResult.Message,
                    QueueId = queueResult.QueueId,
                    MissionCode = enqueueRequest.MissionCode,
                    ExecuteImmediately = queueResult.ExecuteImmediately
                };

                _logger.LogInformation(
                    "Schedule {ScheduleId} enqueued workflow {MissionCode} (QueueId {QueueId}, ExecuteImmediately={ExecuteImmediately})",
                    schedule.Id,
                    enqueueRequest.MissionCode,
                    queueResult.QueueId,
                    queueResult.ExecuteImmediately);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to enqueue workflow {WorkflowId} from schedule {ScheduleId}", workflowId, schedule.Id);
                schedule.LastRunUtc = scheduledForUtc;
                schedule.LastStatus = "Failed";
                schedule.LastError = ex.Message;
                schedule.UpdatedUtc = _timeProvider.GetUtcNow().UtcDateTime;

                if (schedule.TriggerType == WorkflowTriggerType.Once)
                {
                    schedule.IsEnabled = false;
                    schedule.NextRunUtc = null;
                }
                else
                {
                    schedule.NextRunUtc = ComputeNextRunUtc(schedule, _timeProvider.GetUtcNow().UtcDateTime);
                }

                schedule.QueueLockToken = null;

                logEntry.ResultStatus = "Failed";
                logEntry.Error = ex.Message;
            }

            await _dbContext.SaveChangesAsync(cancellationToken);
            releaseLock = false;
            return triggerResult;
        }
        finally
        {
            if (releaseLock)
            {
                await ReleaseScheduleLockAsync(schedule.Id, lockToken, cancellationToken);
            }
        }
    }

    private static TimeZoneInfo ResolveTimezone(string timezoneId)
    {
        if (string.IsNullOrWhiteSpace(timezoneId))
        {
            return TimeZoneInfo.Utc;
        }

        try
        {
            return TimeZoneInfo.FindSystemTimeZoneById(timezoneId);
        }
        catch (TimeZoneNotFoundException)
        {
            if (TryConvertIanaToWindows(timezoneId, out var windowsId))
            {
                try
                {
                    return TimeZoneInfo.FindSystemTimeZoneById(windowsId);
                }
                catch
                {
                    return TimeZoneInfo.Utc;
                }
            }

            return TimeZoneInfo.Utc;
        }
        catch (InvalidTimeZoneException)
        {
            return TimeZoneInfo.Utc;
        }
    }

    private static bool TryConvertIanaToWindows(string ianaId, out string windowsId)
    {
#if NET8_0_OR_GREATER
        if (OperatingSystem.IsWindows())
        {
            try
            {
                if (TimeZoneInfo.TryConvertIanaIdToWindowsId(ianaId, out windowsId))
                {
                    return true;
                }
            }
            catch
            {
                // ignore and fall through
            }
        }
#endif

        windowsId = string.Empty;
        return false;
    }

    private void ValidateCronExpression(string expression)
    {
        if (string.Equals(expression, "@hourly", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(expression, "@daily", StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        var parts = expression.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length != 5)
        {
            throw new WorkflowScheduleValidationException("Cron expression must have 5 components (minute hour day-of-month month day-of-week).");
        }

        if (!IsValidCronField(parts[0], 0, 59))
        {
            throw new WorkflowScheduleValidationException("Cron minute field is invalid. Use '*', '*/n', or a value between 0 and 59.");
        }

        if (!IsValidCronField(parts[1], 0, 23))
        {
            throw new WorkflowScheduleValidationException("Cron hour field is invalid. Use '*', '*/n', or a value between 0 and 23.");
        }

        if (!IsWildcard(parts[2]) || !IsWildcard(parts[3]) || !IsWildcard(parts[4]))
        {
            throw new WorkflowScheduleValidationException("Only wildcard '*' is supported for day-of-month, month, and day-of-week.");
        }
    }

    private static bool IsValidCronField(string field, int min, int max)
    {
        if (IsWildcard(field))
        {
            return true;
        }

        if (field.StartsWith("*/", StringComparison.Ordinal))
        {
            return int.TryParse(field.AsSpan(2), out var step) && step > 0 && step <= max;
        }

        return int.TryParse(field, out var value) && value >= min && value <= max;
    }

    private static bool MatchesCronField(string field, int value, int min, int max)
    {
        if (IsWildcard(field))
        {
            return true;
        }

        if (field.StartsWith("*/", StringComparison.Ordinal))
        {
            if (int.TryParse(field.AsSpan(2), out var step) && step > 0)
            {
                return (value - min) % step == 0;
            }

            return false;
        }

        return int.TryParse(field, out var literal) && literal == value;
    }

    private static bool IsWildcard(string field) => string.Equals(field, "*", StringComparison.Ordinal);
}

public class WorkflowScheduleValidationException : Exception
{
    public WorkflowScheduleValidationException(string message) : base(message)
    {
    }
}

public class WorkflowNotFoundException : Exception
{
    public WorkflowNotFoundException(string message) : base(message)
    {
    }
}
