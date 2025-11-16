using Microsoft.EntityFrameworkCore;
using QES_KUKA_AMR_API.Data;
using QES_KUKA_AMR_API.Data.Entities;

namespace QES_KUKA_AMR_API.Services.Queue;

public class MapCodeQueueManager : IMapCodeQueueManager
{
    private readonly ApplicationDbContext _dbContext;
    private readonly TimeProvider _timeProvider;
    private readonly ILogger<MapCodeQueueManager> _logger;

    public MapCodeQueueManager(
        ApplicationDbContext dbContext,
        TimeProvider timeProvider,
        ILogger<MapCodeQueueManager> logger)
    {
        _dbContext = dbContext;
        _timeProvider = timeProvider;
        _logger = logger;
    }

    public async Task<MissionQueueItem> EnqueueAsync(
        MissionQueueItem queueItem,
        CancellationToken cancellationToken = default)
    {
        queueItem.EnqueuedUtc = _timeProvider.GetUtcNow().UtcDateTime;
        queueItem.Status = MissionQueueStatus.Pending;

        _dbContext.MissionQueueItems.Add(queueItem);
        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Enqueued mission {QueueItemCode} to MapCode {MapCode} with priority {Priority}",
            queueItem.QueueItemCode,
            queueItem.PrimaryMapCode,
            queueItem.Priority
        );

        return queueItem;
    }

    public async Task<MissionQueueItem?> DequeueNextJobAsync(
        string mapCode,
        string? preferredRobotId = null,
        CancellationToken cancellationToken = default)
    {
        var query = _dbContext.MissionQueueItems
            .Where(q => q.PrimaryMapCode == mapCode)
            .Where(q => q.Status == MissionQueueStatus.Pending || q.Status == MissionQueueStatus.ReadyToAssign)
            .OrderByDescending(q => q.Priority)
            .ThenBy(q => q.EnqueuedUtc);

        // If preferred robot specified, try to match robot constraints
        if (!string.IsNullOrEmpty(preferredRobotId))
        {
            var preferredJob = await query
                .Where(q => q.RobotIdsJson == null || q.RobotIdsJson.Contains(preferredRobotId))
                .FirstOrDefaultAsync(cancellationToken);

            if (preferredJob != null)
                return preferredJob;
        }

        return await query.FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<List<MissionQueueItem>> GetPendingJobsAsync(
        string mapCode,
        int limit = 100,
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.MissionQueueItems
            .Where(q => q.PrimaryMapCode == mapCode)
            .Where(q => q.Status == MissionQueueStatus.Pending || q.Status == MissionQueueStatus.ReadyToAssign)
            .OrderByDescending(q => q.Priority)
            .ThenBy(q => q.EnqueuedUtc)
            .Take(limit)
            .ToListAsync(cancellationToken);
    }

    public async Task UpdateJobStatusAsync(
        int queueItemId,
        MissionQueueStatus newStatus,
        CancellationToken cancellationToken = default)
    {
        var queueItem = await _dbContext.MissionQueueItems.FindAsync(new object[] { queueItemId }, cancellationToken);
        if (queueItem == null)
        {
            _logger.LogWarning("Queue item {QueueItemId} not found for status update", queueItemId);
            return;
        }

        var oldStatus = queueItem.Status;
        queueItem.Status = newStatus;

        // Update timestamps based on status
        switch (newStatus)
        {
            case MissionQueueStatus.Assigned:
            case MissionQueueStatus.SubmittedToAmr:
                if (!queueItem.StartedUtc.HasValue)
                    queueItem.StartedUtc = _timeProvider.GetUtcNow().UtcDateTime;
                break;

            case MissionQueueStatus.Completed:
                queueItem.CompletedUtc = _timeProvider.GetUtcNow().UtcDateTime;
                break;

            case MissionQueueStatus.Cancelled:
                queueItem.CancelledUtc = _timeProvider.GetUtcNow().UtcDateTime;
                break;
        }

        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Updated queue item {QueueItemCode} status from {OldStatus} to {NewStatus}",
            queueItem.QueueItemCode,
            oldStatus,
            newStatus
        );
    }

    public async Task CancelJobAsync(
        int queueItemId,
        string reason,
        CancellationToken cancellationToken = default)
    {
        var queueItem = await _dbContext.MissionQueueItems.FindAsync(new object[] { queueItemId }, cancellationToken);
        if (queueItem == null)
        {
            _logger.LogWarning("Queue item {QueueItemId} not found for cancellation", queueItemId);
            return;
        }

        queueItem.Status = MissionQueueStatus.Cancelled;
        queueItem.CancelledUtc = _timeProvider.GetUtcNow().UtcDateTime;
        queueItem.ErrorMessage = reason;

        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Cancelled queue item {QueueItemCode}: {Reason}",
            queueItem.QueueItemCode,
            reason
        );
    }

    public async Task<MissionQueueItem?> GetQueueItemByIdAsync(
        int queueItemId,
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.MissionQueueItems
            .AsNoTracking()
            .FirstOrDefaultAsync(q => q.Id == queueItemId, cancellationToken);
    }

    public async Task<MissionQueueItem?> GetQueueItemByMissionCodeAsync(
        string missionCode,
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.MissionQueueItems
            .AsNoTracking()
            .FirstOrDefaultAsync(q => q.MissionCode == missionCode, cancellationToken);
    }
}
