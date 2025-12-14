using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using QES_KUKA_AMR_API.Data;
using QES_KUKA_AMR_API.Data.Entities;
using QES_KUKA_AMR_API.Hubs;
using QES_KUKA_AMR_API.Models.Missions;
using QES_KUKA_AMR_API.Options;
using QES_KUKA_AMR_API.Services.Auth;
using System.Net.Http.Json;

namespace QES_KUKA_AMR_API.Services.Queue;

public interface IMissionQueueService
{
    Task<List<MissionQueue>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<List<MissionQueue>> GetQueuedAsync(CancellationToken cancellationToken = default);
    Task<MissionQueue?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<MissionQueue?> GetByMissionCodeAsync(string missionCode, CancellationToken cancellationToken = default);
    Task<MissionQueue> AddToQueueAsync(MissionQueue queueItem, CancellationToken cancellationToken = default);
    Task<MissionQueue?> UpdateStatusAsync(int id, MissionQueueStatus status, string? errorMessage = null, CancellationToken cancellationToken = default);
    Task<MissionQueue?> AssignRobotAsync(int id, string robotId, CancellationToken cancellationToken = default);
    Task<bool> CancelAsync(int id, string cancelMode = "FORCE", string? reason = null, CancellationToken cancellationToken = default);
    Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default);
    Task<MissionQueue?> RetryAsync(int id, CancellationToken cancellationToken = default);
    Task<bool> MoveUpAsync(int id, CancellationToken cancellationToken = default);
    Task<bool> MoveDownAsync(int id, CancellationToken cancellationToken = default);
    Task<MissionQueue?> ChangePriorityAsync(int id, int newPriority, CancellationToken cancellationToken = default);
    Task<MissionQueueStatistics> GetStatisticsAsync(CancellationToken cancellationToken = default);
    Task<MissionQueue?> GetNextQueuedItemAsync(CancellationToken cancellationToken = default);
    Task UpdateQueuePositionsAsync(CancellationToken cancellationToken = default);
    /// <summary>
    /// Get count of active mission instances for a saved template
    /// </summary>
    Task<int> GetActiveInstanceCountAsync(int savedMissionId, CancellationToken cancellationToken = default);
}

/// <summary>
/// Exception thrown when concurrency validation fails
/// </summary>
public class ConcurrencyViolationException : Exception
{
    public ConcurrencyViolationException(string message) : base(message)
    {
    }
}

public class MissionQueueStatistics
{
    public int TotalQueued { get; set; }
    public int TotalProcessing { get; set; }
    public int TotalAssigned { get; set; }
    public int TotalCompleted { get; set; }
    public int TotalFailed { get; set; }
    public int TotalCancelled { get; set; }
    public double AverageWaitTimeSeconds { get; set; }
    public double SuccessRate { get; set; }
}

public class MissionQueueService : IMissionQueueService
{
    private readonly ApplicationDbContext _dbContext;
    private readonly TimeProvider _timeProvider;
    private readonly ILogger<MissionQueueService> _logger;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IServiceProvider _serviceProvider;
    private readonly MissionServiceOptions _missionOptions;
    private readonly IQueueNotificationService _notificationService;

    public MissionQueueService(
        ApplicationDbContext dbContext,
        TimeProvider timeProvider,
        ILogger<MissionQueueService> logger,
        IHttpClientFactory httpClientFactory,
        IServiceProvider serviceProvider,
        IOptions<MissionServiceOptions> missionOptions,
        IQueueNotificationService notificationService)
    {
        _dbContext = dbContext;
        _timeProvider = timeProvider;
        _logger = logger;
        _httpClientFactory = httpClientFactory;
        _serviceProvider = serviceProvider;
        _missionOptions = missionOptions.Value;
        _notificationService = notificationService;
    }

    public async Task<List<MissionQueue>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _dbContext.MissionQueues
            .AsNoTracking()
            .OrderBy(q => q.Status)
            .ThenBy(q => q.Priority)
            .ThenBy(q => q.CreatedUtc)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<MissionQueue>> GetQueuedAsync(CancellationToken cancellationToken = default)
    {
        return await _dbContext.MissionQueues
            .AsNoTracking()
            .Where(q => q.Status == MissionQueueStatus.Queued || q.Status == MissionQueueStatus.Processing)
            .OrderBy(q => q.Priority)
            .ThenBy(q => q.CreatedUtc)
            .ToListAsync(cancellationToken);
    }

    public async Task<MissionQueue?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        return await _dbContext.MissionQueues
            .AsNoTracking()
            .FirstOrDefaultAsync(q => q.Id == id, cancellationToken);
    }

    public async Task<MissionQueue?> GetByMissionCodeAsync(string missionCode, CancellationToken cancellationToken = default)
    {
        return await _dbContext.MissionQueues
            .AsNoTracking()
            .FirstOrDefaultAsync(q => q.MissionCode == missionCode, cancellationToken);
    }

    public async Task<MissionQueue> AddToQueueAsync(MissionQueue queueItem, CancellationToken cancellationToken = default)
    {
        // Check concurrency mode if this is from a saved template
        if (queueItem.SavedMissionId.HasValue)
        {
            var savedMission = await _dbContext.SavedCustomMissions
                .AsNoTracking()
                .FirstOrDefaultAsync(m => m.Id == queueItem.SavedMissionId.Value, cancellationToken);

            if (savedMission?.ConcurrencyMode == "Wait")
            {
                var activeCount = await GetActiveInstanceCountAsync(savedMission.Id, cancellationToken);
                if (activeCount > 0)
                {
                    _logger.LogWarning("Concurrency violation: Template '{MissionName}' has {ActiveCount} active mission(s)",
                        savedMission.MissionName, activeCount);
                    throw new ConcurrencyViolationException(
                        $"Template '{savedMission.MissionName}' has {activeCount} active mission(s). " +
                        "Please wait for completion before triggering again.");
                }
            }
        }

        queueItem.CreatedUtc = _timeProvider.GetUtcNow().UtcDateTime;
        queueItem.Status = MissionQueueStatus.Queued;

        _dbContext.MissionQueues.Add(queueItem);
        await _dbContext.SaveChangesAsync(cancellationToken);

        // Update queue positions
        await UpdateQueuePositionsAsync(cancellationToken);

        _logger.LogInformation("Mission {MissionCode} added to queue with priority {Priority}",
            queueItem.MissionCode, queueItem.Priority);

        // Notify clients of queue update
        await _notificationService.NotifyQueueUpdatedAsync(cancellationToken);

        return queueItem;
    }

    public async Task<MissionQueue?> UpdateStatusAsync(
        int id,
        MissionQueueStatus status,
        string? errorMessage = null,
        CancellationToken cancellationToken = default)
    {
        var queueItem = await _dbContext.MissionQueues.FindAsync(new object[] { id }, cancellationToken);
        if (queueItem == null) return null;

        var now = _timeProvider.GetUtcNow().UtcDateTime;

        queueItem.Status = status;
        queueItem.ErrorMessage = errorMessage;

        switch (status)
        {
            case MissionQueueStatus.Processing:
                queueItem.ProcessingStartedUtc = now;
                break;
            case MissionQueueStatus.Assigned:
                queueItem.AssignedUtc = now;
                break;
            case MissionQueueStatus.Completed:
            case MissionQueueStatus.Failed:
            case MissionQueueStatus.Cancelled:
                queueItem.CompletedUtc = now;
                break;
        }

        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Mission {MissionCode} status updated to {Status}",
            queueItem.MissionCode, status);

        // Notify clients of status change
        await _notificationService.NotifyMissionStatusChangedAsync(id, status, cancellationToken);

        return queueItem;
    }

    public async Task<MissionQueue?> AssignRobotAsync(int id, string robotId, CancellationToken cancellationToken = default)
    {
        var queueItem = await _dbContext.MissionQueues.FindAsync(new object[] { id }, cancellationToken);
        if (queueItem == null) return null;

        queueItem.AssignedRobotId = robotId;
        queueItem.AssignedUtc = _timeProvider.GetUtcNow().UtcDateTime;
        queueItem.Status = MissionQueueStatus.Assigned;

        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Mission {MissionCode} assigned to robot {RobotId}",
            queueItem.MissionCode, robotId);

        // Notify clients of assignment
        await _notificationService.NotifyMissionStatusChangedAsync(id, MissionQueueStatus.Assigned, cancellationToken);

        return queueItem;
    }

    public async Task<bool> CancelAsync(int id, string cancelMode = "FORCE", string? reason = null, CancellationToken cancellationToken = default)
    {
        var queueItem = await _dbContext.MissionQueues.FindAsync(new object[] { id }, cancellationToken);
        if (queueItem == null) return false;

        // Cannot cancel if already completed, failed, or cancelled
        if (queueItem.Status == MissionQueueStatus.Completed ||
            queueItem.Status == MissionQueueStatus.Failed ||
            queueItem.Status == MissionQueueStatus.Cancelled)
        {
            _logger.LogWarning("Cannot cancel mission {MissionCode} with status {Status}",
                queueItem.MissionCode, queueItem.Status);
            return false;
        }

        // Validate cancel mode
        var validCancelModes = new[] { "FORCE", "NORMAL", "REDIRECT_START" };
        if (!validCancelModes.Contains(cancelMode, StringComparer.OrdinalIgnoreCase))
        {
            _logger.LogWarning("Invalid cancel mode {CancelMode}, defaulting to FORCE", cancelMode);
            cancelMode = "FORCE";
        }

        var cancelReason = string.IsNullOrWhiteSpace(reason) ? "Cancelled by user from queue" : reason;

        // If mission is Assigned (already submitted to external AMR), call external cancel API first
        if (queueItem.Status == MissionQueueStatus.Assigned)
        {
            _logger.LogInformation("Mission {MissionCode} is Assigned, calling external AMR cancel API with mode {CancelMode}...",
                queueItem.MissionCode, cancelMode);

            var cancelSuccess = await CancelExternalMissionAsync(queueItem.MissionCode, cancelMode, cancelReason, cancellationToken);

            if (!cancelSuccess)
            {
                _logger.LogWarning("Failed to cancel mission {MissionCode} in external AMR system", queueItem.MissionCode);
                // Continue to mark as cancelled in queue anyway
            }
        }

        queueItem.Status = MissionQueueStatus.Cancelled;
        queueItem.CompletedUtc = _timeProvider.GetUtcNow().UtcDateTime;

        await _dbContext.SaveChangesAsync(cancellationToken);
        await UpdateQueuePositionsAsync(cancellationToken);

        // Update MissionHistory to Cancelled status
        await UpdateMissionHistoryOnCancelAsync(queueItem.MissionCode, cancelReason, cancellationToken);

        _logger.LogInformation("Mission {MissionCode} cancelled with mode {CancelMode}", queueItem.MissionCode, cancelMode);

        // Notify clients of cancellation
        await _notificationService.NotifyMissionStatusChangedAsync(id, MissionQueueStatus.Cancelled, cancellationToken);

        return true;
    }

    /// <summary>
    /// Update MissionHistory record to Cancelled status after successful cancel
    /// </summary>
    private async Task UpdateMissionHistoryOnCancelAsync(
        string missionCode,
        string cancelReason,
        CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Updating MissionHistory for cancelled mission {MissionCode}", missionCode);

            // Find the MissionHistory record by missionCode
            var missionHistory = await _dbContext.MissionHistories
                .FirstOrDefaultAsync(m => m.MissionCode == missionCode, cancellationToken);

            if (missionHistory == null)
            {
                _logger.LogWarning("MissionHistory not found for cancelled mission {MissionCode}. " +
                    "This may happen if the mission was not yet submitted to AMR.", missionCode);
                return;
            }

            // Only update if not already in a terminal state
            var terminalStatuses = new[] { "Completed", "Failed", "Cancelled", "Timeout" };
            if (terminalStatuses.Contains(missionHistory.Status, StringComparer.OrdinalIgnoreCase))
            {
                _logger.LogInformation("MissionHistory {MissionCode} already in terminal state '{Status}', skipping update",
                    missionCode, missionHistory.Status);
                return;
            }

            // Update to Cancelled status
            missionHistory.Status = "Cancelled";
            missionHistory.CompletedDate = _timeProvider.GetUtcNow().UtcDateTime;
            missionHistory.ErrorMessage = cancelReason;

            await _dbContext.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("✓ MissionHistory {MissionCode} updated to Cancelled status", missionCode);
        }
        catch (Exception ex)
        {
            // Don't fail the cancel operation if history update fails
            _logger.LogError(ex, "Failed to update MissionHistory for cancelled mission {MissionCode}", missionCode);
        }
    }

    public async Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var queueItem = await _dbContext.MissionQueues.FindAsync(new object[] { id }, cancellationToken);
        if (queueItem == null) return false;

        // Can only delete items that are Completed, Failed, or Cancelled
        // Cannot delete Queued, Processing, or Assigned (must cancel first)
        if (queueItem.Status == MissionQueueStatus.Queued ||
            queueItem.Status == MissionQueueStatus.Processing ||
            queueItem.Status == MissionQueueStatus.Assigned)
        {
            _logger.LogWarning("Cannot delete mission {MissionCode} with status {Status}. Cancel it first.",
                queueItem.MissionCode, queueItem.Status);
            return false;
        }

        _dbContext.MissionQueues.Remove(queueItem);
        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Mission {MissionCode} deleted from queue", queueItem.MissionCode);

        return true;
    }

    /// <summary>
    /// Cancel mission in external AMR system
    /// </summary>
    /// <param name="missionCode">The mission code to cancel</param>
    /// <param name="cancelMode">Cancel mode: FORCE, NORMAL, or REDIRECT_START</param>
    /// <param name="reason">Reason for cancellation</param>
    /// <param name="cancellationToken">Cancellation token</param>
    private async Task<bool> CancelExternalMissionAsync(string missionCode, string cancelMode, string reason, CancellationToken cancellationToken)
    {
        _logger.LogInformation("=== QUEUE CANCEL EXTERNAL MISSION START ===");
        _logger.LogInformation("Attempting to cancel mission {MissionCode} in external AMR system with mode {CancelMode}", missionCode, cancelMode);

        if (string.IsNullOrEmpty(_missionOptions.MissionCancelUrl))
        {
            _logger.LogWarning("MissionCancelUrl is not configured. Cannot cancel in external system.");
            return false;
        }

        try
        {
            var client = _httpClientFactory.CreateClient();
            client.DefaultRequestHeaders.Add("accept", "*/*");
            client.DefaultRequestHeaders.Add("language", "en");
            client.DefaultRequestHeaders.Add("wizards", "FRONT_END");

            // AMR endpoints on port 10870 don't require authentication

            var request = new MissionCancelRequest
            {
                MissionCode = missionCode,
                Reason = reason,
                CancelMode = cancelMode,
                RequestId = $"queue_cancel_{missionCode}_{DateTime.UtcNow:yyyyMMddHHmmss}"
            };

            // Log the request being sent
            _logger.LogInformation(
                "Sending cancel request to external AMR:\n  URL: {Url}\n  MissionCode: {MissionCode}\n  CancelMode: {CancelMode}\n  Reason: {Reason}\n  RequestId: {RequestId}",
                _missionOptions.MissionCancelUrl, request.MissionCode, request.CancelMode, request.Reason, request.RequestId);

            var response = await client.PostAsJsonAsync(
                _missionOptions.MissionCancelUrl,
                request,
                cancellationToken);

            // Read and log the raw response
            var rawResponse = await response.Content.ReadAsStringAsync(cancellationToken);
            _logger.LogInformation(
                "Raw response from external AMR:\n  HTTP Status: {StatusCode}\n  Response Body: {RawResponse}",
                response.StatusCode, rawResponse);

            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation("✓ Mission {MissionCode} cancel request accepted by external AMR system", missionCode);
                _logger.LogInformation("=== QUEUE CANCEL EXTERNAL MISSION END (SUCCESS) ===");
                return true;
            }
            else
            {
                _logger.LogWarning("✗ External AMR cancel failed - HTTP {StatusCode}: {RawResponse}", response.StatusCode, rawResponse);
                _logger.LogInformation("=== QUEUE CANCEL EXTERNAL MISSION END (FAILED) ===");
                return false;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calling external AMR cancel API for {MissionCode}", missionCode);
            _logger.LogInformation("=== QUEUE CANCEL EXTERNAL MISSION END (EXCEPTION) ===");
            return false;
        }
    }

    public async Task<MissionQueue?> RetryAsync(int id, CancellationToken cancellationToken = default)
    {
        var queueItem = await _dbContext.MissionQueues.FindAsync(new object[] { id }, cancellationToken);
        if (queueItem == null) return null;

        // Can only retry failed items
        if (queueItem.Status != MissionQueueStatus.Failed)
        {
            _logger.LogWarning("Cannot retry mission {MissionCode} with status {Status}",
                queueItem.MissionCode, queueItem.Status);
            return null;
        }

        if (queueItem.RetryCount >= queueItem.MaxRetries)
        {
            _logger.LogWarning("Mission {MissionCode} has reached max retries ({MaxRetries})",
                queueItem.MissionCode, queueItem.MaxRetries);
            return null;
        }

        queueItem.Status = MissionQueueStatus.Queued;
        queueItem.RetryCount++;
        queueItem.ErrorMessage = null;
        queueItem.ProcessingStartedUtc = null;
        queueItem.AssignedUtc = null;
        queueItem.CompletedUtc = null;
        queueItem.AssignedRobotId = null;

        await _dbContext.SaveChangesAsync(cancellationToken);
        await UpdateQueuePositionsAsync(cancellationToken);

        _logger.LogInformation("Mission {MissionCode} queued for retry (attempt {RetryCount})",
            queueItem.MissionCode, queueItem.RetryCount);

        return queueItem;
    }

    public async Task<bool> MoveUpAsync(int id, CancellationToken cancellationToken = default)
    {
        var queueItem = await _dbContext.MissionQueues.FindAsync(new object[] { id }, cancellationToken);
        if (queueItem == null || queueItem.Status != MissionQueueStatus.Queued) return false;

        // Get item above (lower position number)
        var itemAbove = await _dbContext.MissionQueues
            .Where(q => q.Status == MissionQueueStatus.Queued && q.QueuePosition < queueItem.QueuePosition)
            .OrderByDescending(q => q.QueuePosition)
            .FirstOrDefaultAsync(cancellationToken);

        if (itemAbove == null) return false;

        // Swap positions
        var tempPosition = queueItem.QueuePosition;
        queueItem.QueuePosition = itemAbove.QueuePosition;
        itemAbove.QueuePosition = tempPosition;

        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Mission {MissionCode} moved up in queue", queueItem.MissionCode);

        return true;
    }

    public async Task<bool> MoveDownAsync(int id, CancellationToken cancellationToken = default)
    {
        var queueItem = await _dbContext.MissionQueues.FindAsync(new object[] { id }, cancellationToken);
        if (queueItem == null || queueItem.Status != MissionQueueStatus.Queued) return false;

        // Get item below (higher position number)
        var itemBelow = await _dbContext.MissionQueues
            .Where(q => q.Status == MissionQueueStatus.Queued && q.QueuePosition > queueItem.QueuePosition)
            .OrderBy(q => q.QueuePosition)
            .FirstOrDefaultAsync(cancellationToken);

        if (itemBelow == null) return false;

        // Swap positions
        var tempPosition = queueItem.QueuePosition;
        queueItem.QueuePosition = itemBelow.QueuePosition;
        itemBelow.QueuePosition = tempPosition;

        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Mission {MissionCode} moved down in queue", queueItem.MissionCode);

        return true;
    }

    public async Task<MissionQueue?> ChangePriorityAsync(int id, int newPriority, CancellationToken cancellationToken = default)
    {
        var queueItem = await _dbContext.MissionQueues.FindAsync(new object[] { id }, cancellationToken);
        if (queueItem == null) return null;

        queueItem.Priority = newPriority;
        await _dbContext.SaveChangesAsync(cancellationToken);
        await UpdateQueuePositionsAsync(cancellationToken);

        _logger.LogInformation("Mission {MissionCode} priority changed to {Priority}",
            queueItem.MissionCode, newPriority);

        return queueItem;
    }

    public async Task<MissionQueueStatistics> GetStatisticsAsync(CancellationToken cancellationToken = default)
    {
        var items = await _dbContext.MissionQueues
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        var completedItems = items.Where(i =>
            i.Status == MissionQueueStatus.Completed ||
            i.Status == MissionQueueStatus.Failed).ToList();

        var waitTimes = items
            .Where(i => i.AssignedUtc.HasValue)
            .Select(i => (i.AssignedUtc!.Value - i.CreatedUtc).TotalSeconds)
            .ToList();

        return new MissionQueueStatistics
        {
            TotalQueued = items.Count(i => i.Status == MissionQueueStatus.Queued),
            TotalProcessing = items.Count(i => i.Status == MissionQueueStatus.Processing),
            TotalAssigned = items.Count(i => i.Status == MissionQueueStatus.Assigned),
            TotalCompleted = items.Count(i => i.Status == MissionQueueStatus.Completed),
            TotalFailed = items.Count(i => i.Status == MissionQueueStatus.Failed),
            TotalCancelled = items.Count(i => i.Status == MissionQueueStatus.Cancelled),
            AverageWaitTimeSeconds = waitTimes.Any() ? waitTimes.Average() : 0,
            SuccessRate = completedItems.Any()
                ? (double)completedItems.Count(i => i.Status == MissionQueueStatus.Completed) / completedItems.Count * 100
                : 0
        };
    }

    public async Task<MissionQueue?> GetNextQueuedItemAsync(CancellationToken cancellationToken = default)
    {
        return await _dbContext.MissionQueues
            .Where(q => q.Status == MissionQueueStatus.Queued)
            .OrderBy(q => q.Priority)
            .ThenBy(q => q.CreatedUtc)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task UpdateQueuePositionsAsync(CancellationToken cancellationToken = default)
    {
        var queuedItems = await _dbContext.MissionQueues
            .Where(q => q.Status == MissionQueueStatus.Queued)
            .OrderBy(q => q.Priority)
            .ThenBy(q => q.CreatedUtc)
            .ToListAsync(cancellationToken);

        for (int i = 0; i < queuedItems.Count; i++)
        {
            queuedItems[i].QueuePosition = i + 1;
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    /// <summary>
    /// Get count of active mission instances for a saved template.
    /// Active means: Queued, Processing, or Assigned status.
    /// </summary>
    public async Task<int> GetActiveInstanceCountAsync(int savedMissionId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.MissionQueues
            .CountAsync(q =>
                q.SavedMissionId == savedMissionId &&
                (q.Status == MissionQueueStatus.Queued ||
                 q.Status == MissionQueueStatus.Processing ||
                 q.Status == MissionQueueStatus.Assigned),
                cancellationToken);
    }
}
