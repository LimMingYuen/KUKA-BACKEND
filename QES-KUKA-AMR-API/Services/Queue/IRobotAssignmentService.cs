using QES_KUKA_AMR_API.Data.Entities;
using QES_KUKA_AMR_API.Models.Queue;

namespace QES_KUKA_AMR_API.Services.Queue;

public interface IRobotAssignmentService
{
    /// <summary>
    /// Assign best available robot to a queue item
    /// </summary>
    Task<RobotAssignment?> AssignBestRobotAsync(
        MissionQueueItem queueItem,
        CancellationToken cancellationToken = default
    );

    /// <summary>
    /// Calculate robot distances and scores for a target position
    /// </summary>
    Task<List<RobotDistanceScore>> CalculateRobotDistancesAsync(
        string mapCode,
        double targetX,
        double targetY,
        int jobPriority,
        CancellationToken cancellationToken = default
    );

    /// <summary>
    /// Get robot position from database or API
    /// </summary>
    Task<RobotPosition?> GetRobotPositionAsync(
        string robotId,
        PositionSource source = PositionSource.Auto,
        CancellationToken cancellationToken = default
    );

    /// <summary>
    /// Get first node position from queue item
    /// </summary>
    Task<NodePosition?> GetFirstNodePositionAsync(
        MissionQueueItem queueItem,
        CancellationToken cancellationToken = default
    );

    /// <summary>
    /// Get last node position from completed queue item
    /// </summary>
    Task<RobotPosition?> GetRobotLastPositionAsync(
        MissionQueueItem completedJob,
        CancellationToken cancellationToken = default
    );
}
