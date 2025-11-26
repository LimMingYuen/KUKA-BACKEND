using QES_KUKA_AMR_API.Data.Entities;

namespace QES_KUKA_AMR_API.Services.Queue;

/// <summary>
/// Service for job optimization - reserves nearby tasks for robots in the same map.
/// Only applies to missions with SavedCustomMission.OrgId = "JOBOPTIMIZATION".
/// </summary>
public interface IJobOptimizationService
{
    /// <summary>
    /// Check if a mission uses job optimization (OrgId = "JOBOPTIMIZATION")
    /// </summary>
    /// <param name="savedMissionId">The SavedCustomMission ID</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>True if mission uses job optimization</returns>
    Task<bool> IsJobOptimizationMissionAsync(int? savedMissionId, CancellationToken ct = default);

    /// <summary>
    /// Get the destination MapCode for a mission (from last step's position)
    /// </summary>
    /// <param name="mission">The mission queue item</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Destination map code or null</returns>
    Task<string?> GetDestinationMapCodeAsync(MissionQueue mission, CancellationToken ct = default);

    /// <summary>
    /// Get the destination location (MapCode, X, Y) for a mission (from last step's position)
    /// </summary>
    /// <param name="mission">The mission queue item</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Destination location or null</returns>
    Task<MissionSourceLocation?> GetDestinationLocationAsync(MissionQueue mission, CancellationToken ct = default);

    /// <summary>
    /// Get the source location (MapCode, X, Y) for a mission (from first step's position)
    /// </summary>
    /// <param name="mission">The mission queue item</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Source location or null</returns>
    Task<MissionSourceLocation?> GetSourceLocationAsync(MissionQueue mission, CancellationToken ct = default);

    /// <summary>
    /// Find and reserve the nearest same-map mission for a robot.
    /// Called after assigning a JOBOPTIMIZATION mission.
    /// </summary>
    /// <param name="robotId">Robot ID that will receive the reservation</param>
    /// <param name="triggeringMissionCode">Mission code of the JOBOPTIMIZATION mission that triggered this reservation</param>
    /// <param name="destinationMapCode">Map code where the robot will be after completing current mission</param>
    /// <param name="targetX">X coordinate of the robot's destination</param>
    /// <param name="targetY">Y coordinate of the robot's destination</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Reserved mission or null if none found</returns>
    Task<MissionQueue?> ReserveNextSameMapMissionAsync(
        string robotId,
        string triggeringMissionCode,
        string destinationMapCode,
        double targetX,
        double targetY,
        CancellationToken ct = default);

    /// <summary>
    /// Get reserved mission for a robot (if any)
    /// </summary>
    /// <param name="robotId">Robot ID to check</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Reserved mission or null</returns>
    Task<MissionQueue?> GetReservedMissionAsync(string robotId, CancellationToken ct = default);

    /// <summary>
    /// Clear a specific robot's reservation
    /// </summary>
    /// <param name="robotId">Robot ID whose reservation to clear</param>
    /// <param name="ct">Cancellation token</param>
    Task ClearReservationAsync(string robotId, CancellationToken ct = default);

    /// <summary>
    /// Clear reservations where the triggering JOBOPTIMIZATION mission has completed or failed.
    /// Uses job status to determine if reservation should be cleared.
    /// </summary>
    /// <param name="getJobStatusAsync">Function to query job status by mission code</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Number of reservations cleared</returns>
    Task<int> ClearCompletedReservationsAsync(
        Func<string, CancellationToken, Task<int?>> getJobStatusAsync,
        CancellationToken ct = default);

    /// <summary>
    /// Calculate Euclidean distance between two points
    /// </summary>
    double CalculateDistance(double x1, double y1, double x2, double y2);
}

/// <summary>
/// Location data for a mission node (source or destination)
/// </summary>
public class MissionSourceLocation
{
    public string? MapCode { get; set; }
    public string? NodeLabel { get; set; }
    public double? X { get; set; }
    public double? Y { get; set; }
}
