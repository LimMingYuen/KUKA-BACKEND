using QES_KUKA_AMR_API.Data.Entities;
using QES_KUKA_AMR_API.Models.Queue;

namespace QES_KUKA_AMR_API.Services.Queue;

public interface IJobOpportunityEvaluator
{
    /// <summary>
    /// Evaluate whether robot should take an opportunistic job after completing current mission
    /// </summary>
    Task<OpportunityEvaluation> EvaluateOpportunityAsync(
        string robotId,
        int completedQueueItemId,
        CancellationToken cancellationToken = default
    );

    /// <summary>
    /// Find nearest job to a specific position on a MapCode
    /// </summary>
    Task<MissionQueueItem?> FindNearestJobAsync(
        string mapCode,
        double robotX,
        double robotY,
        CancellationToken cancellationToken = default
    );

    /// <summary>
    /// Calculate Euclidean distance between two points
    /// </summary>
    double CalculateDistance(double x1, double y1, double x2, double y2);
}
