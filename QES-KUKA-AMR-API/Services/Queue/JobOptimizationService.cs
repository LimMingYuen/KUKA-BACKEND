using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using QES_KUKA_AMR_API.Data;
using QES_KUKA_AMR_API.Data.Entities;
using QES_KUKA_AMR_API.Models.Missions;

namespace QES_KUKA_AMR_API.Services.Queue;

/// <summary>
/// Service for job optimization - reserves nearby tasks for robots in the same map.
/// Only applies to missions with SavedCustomMission.OrgId = "JOBOPTIMIZATION".
/// </summary>
public class JobOptimizationService : IJobOptimizationService
{
    private const string JOB_OPTIMIZATION_ORG_ID = "JOBOPTIMIZATION";

    private readonly ApplicationDbContext _dbContext;
    private readonly ILogger<JobOptimizationService> _logger;
    private readonly TimeProvider _timeProvider;

    public JobOptimizationService(
        ApplicationDbContext dbContext,
        ILogger<JobOptimizationService> logger,
        TimeProvider timeProvider)
    {
        _dbContext = dbContext;
        _logger = logger;
        _timeProvider = timeProvider;
    }

    /// <inheritdoc />
    public async Task<bool> IsJobOptimizationMissionAsync(int? savedMissionId, CancellationToken ct = default)
    {
        if (savedMissionId == null)
            return false;

        var savedMission = await _dbContext.SavedCustomMissions
            .AsNoTracking()
            .FirstOrDefaultAsync(m => m.Id == savedMissionId && !m.IsDeleted, ct);

        if (savedMission == null)
            return false;

        return string.Equals(savedMission.OrgId, JOB_OPTIMIZATION_ORG_ID, StringComparison.OrdinalIgnoreCase);
    }

    /// <inheritdoc />
    public async Task<string?> GetDestinationMapCodeAsync(MissionQueue mission, CancellationToken ct = default)
    {
        var location = await GetDestinationLocationAsync(mission, ct);
        return location?.MapCode;
    }

    /// <inheritdoc />
    public async Task<MissionSourceLocation?> GetDestinationLocationAsync(MissionQueue mission, CancellationToken ct = default)
    {
        if (mission.SavedMissionId == null)
            return null;

        var savedMission = await _dbContext.SavedCustomMissions
            .AsNoTracking()
            .FirstOrDefaultAsync(m => m.Id == mission.SavedMissionId && !m.IsDeleted, ct);

        if (savedMission == null)
            return null;

        // Try to get last node from workflow (synced workflows)
        if (!string.IsNullOrEmpty(savedMission.TemplateCode))
        {
            return await GetLocationFromWorkflowAsync(savedMission.TemplateCode, isFirst: false, ct);
        }

        // Try to get last step from MissionStepsJson (custom missions)
        if (!string.IsNullOrEmpty(savedMission.MissionStepsJson))
        {
            return await GetLocationFromMissionStepsAsync(savedMission.MissionStepsJson, isFirst: false, ct);
        }

        return null;
    }

    /// <inheritdoc />
    public async Task<MissionSourceLocation?> GetSourceLocationAsync(MissionQueue mission, CancellationToken ct = default)
    {
        if (mission.SavedMissionId == null)
            return null;

        var savedMission = await _dbContext.SavedCustomMissions
            .AsNoTracking()
            .FirstOrDefaultAsync(m => m.Id == mission.SavedMissionId && !m.IsDeleted, ct);

        if (savedMission == null)
            return null;

        // Try to get first node from workflow (synced workflows)
        if (!string.IsNullOrEmpty(savedMission.TemplateCode))
        {
            return await GetLocationFromWorkflowAsync(savedMission.TemplateCode, isFirst: true, ct);
        }

        // Try to get first step from MissionStepsJson (custom missions)
        if (!string.IsNullOrEmpty(savedMission.MissionStepsJson))
        {
            return await GetLocationFromMissionStepsAsync(savedMission.MissionStepsJson, isFirst: true, ct);
        }

        return null;
    }

    /// <inheritdoc />
    public async Task<MissionQueue?> ReserveNextSameMapMissionAsync(
        string robotId,
        string triggeringMissionCode,
        string destinationMapCode,
        double targetX,
        double targetY,
        CancellationToken ct = default)
    {
        if (string.IsNullOrEmpty(robotId) || string.IsNullOrEmpty(destinationMapCode) || string.IsNullOrEmpty(triggeringMissionCode))
            return null;

        // Check if robot already has a reservation
        var existingReservation = await _dbContext.MissionQueues
            .FirstOrDefaultAsync(m => m.ReservedForRobotId == robotId && m.Status == MissionQueueStatus.Queued, ct);

        if (existingReservation != null)
        {
            _logger.LogDebug("Robot {RobotId} already has reservation for mission {MissionCode}",
                robotId, existingReservation.MissionCode);
            return null;
        }

        // Get all queued missions that are not reserved
        var queuedMissions = await _dbContext.MissionQueues
            .Where(m => m.Status == MissionQueueStatus.Queued && m.ReservedForRobotId == null)
            .ToListAsync(ct);

        if (!queuedMissions.Any())
            return null;

        // Find missions with source in destination map and calculate distances
        var missionsWithDistance = new List<(MissionQueue Mission, double Distance)>();

        foreach (var mission in queuedMissions)
        {
            var sourceLocation = await GetSourceLocationAsync(mission, ct);

            if (sourceLocation == null ||
                !string.Equals(sourceLocation.MapCode, destinationMapCode, StringComparison.OrdinalIgnoreCase))
                continue;

            // Calculate distance from target to source
            double distance;
            if (sourceLocation.X.HasValue && sourceLocation.Y.HasValue)
            {
                distance = CalculateDistance(targetX, targetY, sourceLocation.X.Value, sourceLocation.Y.Value);
            }
            else
            {
                // If coordinates not available, use a large default distance
                // This will place missions without coordinates at the end
                distance = double.MaxValue;
                _logger.LogWarning("Mission {MissionCode} source node {NodeLabel} has no coordinates, using max distance",
                    mission.MissionCode, sourceLocation.NodeLabel);
            }

            missionsWithDistance.Add((mission, distance));
        }

        if (!missionsWithDistance.Any())
        {
            _logger.LogDebug("No queued missions found with source in map {MapCode}", destinationMapCode);
            return null;
        }

        // Sort by distance and pick the nearest
        var nearest = missionsWithDistance
            .OrderBy(m => m.Distance)
            .First();

        // Reserve the mission
        nearest.Mission.ReservedForRobotId = robotId;
        nearest.Mission.ReservedUtc = _timeProvider.GetUtcNow().UtcDateTime;
        nearest.Mission.ReservedByMissionCode = triggeringMissionCode;

        await _dbContext.SaveChangesAsync(ct);

        _logger.LogInformation(
            "Reserved mission {MissionCode} for robot {RobotId} (triggered by: {TriggeringMission}, distance: {Distance:F2}, map: {MapCode})",
            nearest.Mission.MissionCode, robotId, triggeringMissionCode, nearest.Distance, destinationMapCode);

        return nearest.Mission;
    }

    /// <inheritdoc />
    public async Task<MissionQueue?> GetReservedMissionAsync(string robotId, CancellationToken ct = default)
    {
        if (string.IsNullOrEmpty(robotId))
            return null;

        return await _dbContext.MissionQueues
            .FirstOrDefaultAsync(m =>
                m.ReservedForRobotId == robotId &&
                m.Status == MissionQueueStatus.Queued, ct);
    }

    /// <inheritdoc />
    public async Task ClearReservationAsync(string robotId, CancellationToken ct = default)
    {
        if (string.IsNullOrEmpty(robotId))
            return;

        var reservedMission = await _dbContext.MissionQueues
            .FirstOrDefaultAsync(m => m.ReservedForRobotId == robotId, ct);

        if (reservedMission != null)
        {
            reservedMission.ReservedForRobotId = null;
            reservedMission.ReservedUtc = null;
            reservedMission.ReservedByMissionCode = null;
            await _dbContext.SaveChangesAsync(ct);

            _logger.LogInformation("Cleared reservation for robot {RobotId} on mission {MissionCode}",
                robotId, reservedMission.MissionCode);
        }
    }

    /// <inheritdoc />
    public async Task<int> ClearCompletedReservationsAsync(
        Func<string, CancellationToken, Task<int?>> getJobStatusAsync,
        CancellationToken ct = default)
    {
        // Terminal job statuses - mission is finished (success, failed, or cancelled)
        var terminalStatuses = new HashSet<int> { 28, 30, 31, 35, 50, 60 };
        // 28 = Cancelling, 30 = Complete, 31 = Cancelled, 35 = Manual complete, 50 = Warning, 60 = Startup error

        // Get all reservations with a triggering mission code
        var reservations = await _dbContext.MissionQueues
            .Where(m => m.ReservedForRobotId != null &&
                       m.ReservedByMissionCode != null)
            .ToListAsync(ct);

        if (!reservations.Any())
            return 0;

        var clearedCount = 0;

        foreach (var mission in reservations)
        {
            try
            {
                // Query the job status of the triggering mission
                var jobStatus = await getJobStatusAsync(mission.ReservedByMissionCode!, ct);

                if (jobStatus == null)
                {
                    // Can't determine status - keep reservation for now
                    _logger.LogDebug(
                        "Could not get job status for triggering mission {TriggeringMission}, keeping reservation for {MissionCode}",
                        mission.ReservedByMissionCode, mission.MissionCode);
                    continue;
                }

                // Check if triggering mission is in a terminal state
                if (terminalStatuses.Contains(jobStatus.Value))
                {
                    _logger.LogInformation(
                        "Clearing reservation for mission {MissionCode} - triggering mission {TriggeringMission} completed with status {Status}",
                        mission.MissionCode, mission.ReservedByMissionCode, jobStatus.Value);

                    mission.ReservedForRobotId = null;
                    mission.ReservedUtc = null;
                    mission.ReservedByMissionCode = null;
                    clearedCount++;
                }
                else
                {
                    _logger.LogDebug(
                        "Keeping reservation for {MissionCode} - triggering mission {TriggeringMission} still active (status: {Status})",
                        mission.MissionCode, mission.ReservedByMissionCode, jobStatus.Value);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex,
                    "Error checking job status for triggering mission {TriggeringMission}, keeping reservation for {MissionCode}",
                    mission.ReservedByMissionCode, mission.MissionCode);
            }
        }

        if (clearedCount > 0)
        {
            await _dbContext.SaveChangesAsync(ct);
        }

        return clearedCount;
    }

    /// <inheritdoc />
    public double CalculateDistance(double x1, double y1, double x2, double y2)
    {
        return Math.Sqrt(Math.Pow(x2 - x1, 2) + Math.Pow(y2 - y1, 2));
    }

    #region Private Methods

    private async Task<MissionSourceLocation?> GetLocationFromWorkflowAsync(
        string templateCode,
        bool isFirst,
        CancellationToken ct)
    {
        // Get WorkflowDiagram by TemplateCode (which is the WorkflowCode)
        var workflow = await _dbContext.WorkflowDiagrams
            .AsNoTracking()
            .FirstOrDefaultAsync(w => w.WorkflowCode == templateCode, ct);

        if (workflow?.ExternalWorkflowId == null)
        {
            _logger.LogWarning("Workflow not found or has no ExternalWorkflowId for TemplateCode: {TemplateCode}", templateCode);
            return null;
        }

        // Get first or last node from WorkflowNodeCode
        var nodeQuery = _dbContext.WorkflowNodeCodes
            .AsNoTracking()
            .Where(wnc => wnc.ExternalWorkflowId == workflow.ExternalWorkflowId);

        var node = isFirst
            ? await nodeQuery.OrderBy(wnc => wnc.Id).FirstOrDefaultAsync(ct)
            : await nodeQuery.OrderByDescending(wnc => wnc.Id).FirstOrDefaultAsync(ct);

        if (node == null)
        {
            _logger.LogWarning("No nodes found for workflow ExternalWorkflowId: {ExternalWorkflowId}", workflow.ExternalWorkflowId);
            return null;
        }

        return await GetLocationFromNodeLabelAsync(node.NodeCode, ct);
    }

    private async Task<MissionSourceLocation?> GetLocationFromMissionStepsAsync(
        string missionStepsJson,
        bool isFirst,
        CancellationToken ct)
    {
        try
        {
            var steps = JsonSerializer.Deserialize<List<MissionDataItem>>(missionStepsJson);

            if (steps == null || !steps.Any())
                return null;

            var step = isFirst
                ? steps.OrderBy(s => s.Sequence).FirstOrDefault()
                : steps.OrderByDescending(s => s.Sequence).FirstOrDefault();

            if (step == null || string.IsNullOrEmpty(step.Position))
                return null;

            return await GetLocationFromNodeLabelAsync(step.Position, ct);
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Failed to parse MissionStepsJson");
            return null;
        }
    }

    private async Task<MissionSourceLocation?> GetLocationFromNodeLabelAsync(string nodeLabel, CancellationToken ct)
    {
        if (string.IsNullOrEmpty(nodeLabel))
            return null;

        var qrCode = await _dbContext.QrCodes
            .AsNoTracking()
            .FirstOrDefaultAsync(q => q.NodeLabel == nodeLabel, ct);

        if (qrCode == null)
        {
            _logger.LogWarning("QrCode not found for NodeLabel: {NodeLabel}", nodeLabel);
            return null;
        }

        return new MissionSourceLocation
        {
            MapCode = qrCode.MapCode,
            NodeLabel = qrCode.NodeLabel,
            X = qrCode.XCoordinate,
            Y = qrCode.YCoordinate
        };
    }

    #endregion
}
