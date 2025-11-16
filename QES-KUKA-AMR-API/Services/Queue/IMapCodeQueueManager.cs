using QES_KUKA_AMR_API.Data.Entities;

namespace QES_KUKA_AMR_API.Services.Queue;

public interface IMapCodeQueueManager
{
    /// <summary>
    /// Enqueue a new mission to the appropriate MapCode queue
    /// </summary>
    Task<MissionQueueItem> EnqueueAsync(MissionQueueItem queueItem, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get the next pending job from a specific MapCode queue
    /// </summary>
    Task<MissionQueueItem?> DequeueNextJobAsync(string mapCode, string? preferredRobotId = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get all pending jobs for a specific MapCode
    /// </summary>
    Task<List<MissionQueueItem>> GetPendingJobsAsync(string mapCode, int limit = 100, CancellationToken cancellationToken = default);

    /// <summary>
    /// Update job status
    /// </summary>
    Task UpdateJobStatusAsync(int queueItemId, MissionQueueStatus newStatus, CancellationToken cancellationToken = default);

    /// <summary>
    /// Cancel a queued job
    /// </summary>
    Task CancelJobAsync(int queueItemId, string reason, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get queue item by ID
    /// </summary>
    Task<MissionQueueItem?> GetQueueItemByIdAsync(int queueItemId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get queue item by mission code
    /// </summary>
    Task<MissionQueueItem?> GetQueueItemByMissionCodeAsync(string missionCode, CancellationToken cancellationToken = default);
}
