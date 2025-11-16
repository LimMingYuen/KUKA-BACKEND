using Microsoft.EntityFrameworkCore;
using QES_KUKA_AMR_API.Data;
using QES_KUKA_AMR_API.Data.Entities;
using QES_KUKA_AMR_API.Models.Queue;

namespace QES_KUKA_AMR_API.Services.Queue;

public class JobOpportunityEvaluator : IJobOpportunityEvaluator
{
    private readonly ApplicationDbContext _dbContext;
    private readonly IMapCodeQueueManager _queueManager;
    private readonly IRobotAssignmentService _robotAssignmentService;
    private readonly TimeProvider _timeProvider;
    private readonly ILogger<JobOpportunityEvaluator> _logger;

    public JobOpportunityEvaluator(
        ApplicationDbContext dbContext,
        IMapCodeQueueManager queueManager,
        IRobotAssignmentService robotAssignmentService,
        TimeProvider timeProvider,
        ILogger<JobOpportunityEvaluator> logger)
    {
        _dbContext = dbContext;
        _queueManager = queueManager;
        _robotAssignmentService = robotAssignmentService;
        _timeProvider = timeProvider;
        _logger = logger;
    }

    public async Task<OpportunityEvaluation> EvaluateOpportunityAsync(
        string robotId,
        int completedQueueItemId,
        CancellationToken cancellationToken = default)
    {
        // Step 1: Get completed job details
        var completedJob = await _dbContext.MissionQueueItems
            .FirstOrDefaultAsync(q => q.Id == completedQueueItemId, cancellationToken);

        if (completedJob == null)
        {
            return new OpportunityEvaluation
            {
                Decision = OpportunityDecision.NoJobsAvailable,
                Reason = "Completed job not found"
            };
        }

        // Step 2: Get robot's current position (last node of completed workflow)
        var robotPosition = await _robotAssignmentService.GetRobotLastPositionAsync(completedJob, cancellationToken);
        if (robotPosition == null)
        {
            _logger.LogWarning(
                "Unable to determine robot {RobotId} position after completing job {JobId}",
                robotId,
                completedQueueItemId
            );

            // Fallback to Robot Query API
            robotPosition = await _robotAssignmentService.GetRobotPositionAsync(
                robotId,
                PositionSource.RealTime,
                cancellationToken
            );

            if (robotPosition == null)
            {
                return new OpportunityEvaluation
                {
                    Decision = OpportunityDecision.NoJobsAvailable,
                    Reason = "Unable to determine robot position"
                };
            }
        }

        // Step 3: Get or create opportunity record
        var opportunity = await GetOrCreateOpportunityRecordAsync(
            robotId,
            completedQueueItemId,
            robotPosition,
            cancellationToken
        );

        // Step 4: Get map configuration
        var mapConfig = await GetMapCodeConfigAsync(robotPosition.MapCode, cancellationToken);

        // Step 5: Check consecutive job limit (only if on foreign map)
        if (robotPosition.MapCode != opportunity.OriginalMapCode)
        {
            if (opportunity.ConsecutiveJobsInMapCode >= mapConfig.MaxConsecutiveOpportunisticJobs)
            {
                opportunity.Decision = OpportunityDecision.LimitReached;
                opportunity.DecisionReason = $"Reached maximum consecutive jobs ({mapConfig.MaxConsecutiveOpportunisticJobs}) in foreign map";
                opportunity.OpportunityEvaluatedUtc = _timeProvider.GetUtcNow().UtcDateTime;
                await _dbContext.SaveChangesAsync(cancellationToken);

                return new OpportunityEvaluation
                {
                    Decision = OpportunityDecision.LimitReached,
                    Reason = opportunity.DecisionReason,
                    ConsecutiveJobCount = opportunity.ConsecutiveJobsInMapCode
                };
            }
        }

        // Step 6: Find all pending jobs on current map
        var pendingJobs = await _queueManager.GetPendingJobsAsync(
            robotPosition.MapCode,
            limit: 100,
            cancellationToken
        );

        if (!pendingJobs.Any())
        {
            opportunity.Decision = OpportunityDecision.NoJobsAvailable;
            opportunity.DecisionReason = "No pending jobs in current map";
            opportunity.OpportunityEvaluatedUtc = _timeProvider.GetUtcNow().UtcDateTime;
            await _dbContext.SaveChangesAsync(cancellationToken);

            return new OpportunityEvaluation
            {
                Decision = OpportunityDecision.NoJobsAvailable,
                Reason = "No pending jobs in current map",
                ConsecutiveJobCount = opportunity.ConsecutiveJobsInMapCode
            };
        }

        // Step 7: Calculate distance to first node of each pending job
        var jobDistances = new List<JobDistanceInfo>();

        foreach (var job in pendingJobs)
        {
            var firstNodePosition = await _robotAssignmentService.GetFirstNodePositionAsync(job, cancellationToken);
            if (firstNodePosition == null)
            {
                _logger.LogWarning("Skipping job {JobId}: first node has no coordinates", job.Id);
                continue;
            }

            var distance = CalculateDistance(
                robotPosition.X,
                robotPosition.Y,
                firstNodePosition.X,
                firstNodePosition.Y
            );

            jobDistances.Add(new JobDistanceInfo
            {
                Job = job,
                Distance = distance,
                FirstNodePosition = firstNodePosition
            });
        }

        if (!jobDistances.Any())
        {
            opportunity.Decision = OpportunityDecision.NoJobsAvailable;
            opportunity.DecisionReason = "No pending jobs have valid coordinates";
            opportunity.OpportunityEvaluatedUtc = _timeProvider.GetUtcNow().UtcDateTime;
            await _dbContext.SaveChangesAsync(cancellationToken);

            return new OpportunityEvaluation
            {
                Decision = OpportunityDecision.NoJobsAvailable,
                Reason = "No pending jobs have valid coordinates",
                ConsecutiveJobCount = opportunity.ConsecutiveJobsInMapCode
            };
        }

        // Step 8: Select nearest job (priority as tie-breaker)
        var selectedJobInfo = jobDistances
            .OrderBy(jd => jd.Distance)              // Nearest first
            .ThenByDescending(jd => jd.Job.Priority) // Higher priority if same distance
            .ThenBy(jd => jd.Job.EnqueuedUtc)        // Older job if still tied
            .First();

        // Step 9: Update opportunity tracking
        opportunity.ConsecutiveJobsInMapCode++;
        opportunity.SelectedOpportunisticJobId = selectedJobInfo.Job.Id;
        opportunity.OpportunityEvaluatedUtc = _timeProvider.GetUtcNow().UtcDateTime;
        opportunity.Decision = OpportunityDecision.JobChained;
        opportunity.DecisionReason =
            $"Selected nearest job: {selectedJobInfo.Job.QueueItemCode} " +
            $"(distance: {selectedJobInfo.Distance:F2}m, priority: {selectedJobInfo.Job.Priority})";

        await _dbContext.SaveChangesAsync(cancellationToken);

        // Step 10: Assign robot to selected job
        selectedJobInfo.Job.IsOpportunisticJob = true;
        selectedJobInfo.Job.AssignedRobotId = robotId;
        selectedJobInfo.Job.RobotAssignedUtc = _timeProvider.GetUtcNow().UtcDateTime;
        selectedJobInfo.Job.Status = MissionQueueStatus.Assigned;
        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Robot {RobotId} chained to nearest job {JobCode} " +
            "(distance: {Distance:F2}m, priority: {Priority}, consecutive jobs: {Count})",
            robotId,
            selectedJobInfo.Job.QueueItemCode,
            selectedJobInfo.Distance,
            selectedJobInfo.Job.Priority,
            opportunity.ConsecutiveJobsInMapCode
        );

        // Update map config statistics
        await UpdateMapConfigStatisticsAsync(
            robotPosition.MapCode,
            selectedJobInfo.Distance,
            cancellationToken
        );

        return new OpportunityEvaluation
        {
            Decision = OpportunityDecision.JobChained,
            Reason = $"Chained to nearest job (distance: {selectedJobInfo.Distance:F2}m)",
            SelectedJob = selectedJobInfo.Job,
            DistanceToJob = selectedJobInfo.Distance,
            ConsecutiveJobCount = opportunity.ConsecutiveJobsInMapCode
        };
    }

    public async Task<MissionQueueItem?> FindNearestJobAsync(
        string mapCode,
        double robotX,
        double robotY,
        CancellationToken cancellationToken = default)
    {
        var pendingJobs = await _queueManager.GetPendingJobsAsync(mapCode, 100, cancellationToken);

        MissionQueueItem? nearestJob = null;
        double minDistance = double.MaxValue;

        foreach (var job in pendingJobs)
        {
            var firstNode = await _robotAssignmentService.GetFirstNodePositionAsync(job, cancellationToken);
            if (firstNode == null)
                continue;

            var distance = CalculateDistance(robotX, robotY, firstNode.X, firstNode.Y);

            if (distance < minDistance)
            {
                minDistance = distance;
                nearestJob = job;
            }
        }

        return nearestJob;
    }

    public double CalculateDistance(double x1, double y1, double x2, double y2)
    {
        return Math.Sqrt(Math.Pow(x2 - x1, 2) + Math.Pow(y2 - y1, 2));
    }

    // Private helper methods

    private async Task<RobotJobOpportunity> GetOrCreateOpportunityRecordAsync(
        string robotId,
        int completedQueueItemId,
        RobotPosition robotPosition,
        CancellationToken cancellationToken)
    {
        var existing = await _dbContext.RobotJobOpportunities
            .Where(o => o.RobotId == robotId && o.CurrentQueueItemId == completedQueueItemId)
            .FirstOrDefaultAsync(cancellationToken);

        if (existing != null)
            return existing;

        // Determine original map code (home map)
        var originalMapCode = robotPosition.MapCode;

        // Check if robot has previous opportunity records to track original map
        var previousOpportunity = await _dbContext.RobotJobOpportunities
            .Where(o => o.RobotId == robotId)
            .OrderByDescending(o => o.MissionCompletedUtc)
            .FirstOrDefaultAsync(cancellationToken);

        if (previousOpportunity != null)
        {
            // If robot is still on same map as previous opportunity, use same original map
            if (robotPosition.MapCode == previousOpportunity.CurrentMapCode)
            {
                originalMapCode = previousOpportunity.OriginalMapCode;
            }
        }

        var newOpportunity = new RobotJobOpportunity
        {
            RobotId = robotId,
            CurrentMapCode = robotPosition.MapCode,
            CurrentXCoordinate = robotPosition.X,
            CurrentYCoordinate = robotPosition.Y,
            PositionUpdatedUtc = robotPosition.UpdatedUtc,
            CurrentQueueItemId = completedQueueItemId,
            MissionCompletedUtc = _timeProvider.GetUtcNow().UtcDateTime,
            ConsecutiveJobsInMapCode = 0,
            OriginalMapCode = originalMapCode,
            EnteredMapCodeUtc = _timeProvider.GetUtcNow().UtcDateTime,
            OpportunityCheckPending = true,
            Decision = OpportunityDecision.Pending
        };

        _dbContext.RobotJobOpportunities.Add(newOpportunity);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return newOpportunity;
    }

    private async Task<MapCodeQueueConfiguration> GetMapCodeConfigAsync(
        string mapCode,
        CancellationToken cancellationToken)
    {
        var config = await _dbContext.MapCodeQueueConfigurations
            .FirstOrDefaultAsync(c => c.MapCode == mapCode, cancellationToken);

        if (config != null)
            return config;

        // Create default configuration if not exists
        var defaultConfig = new MapCodeQueueConfiguration
        {
            MapCode = mapCode,
            EnableQueue = true,
            DefaultPriority = 5,
            MaxConsecutiveOpportunisticJobs = 1,
            EnableCrossMapOptimization = true,
            MaxConcurrentRobotsOnMap = 10,
            QueueProcessingIntervalSeconds = 5,
            CreatedUtc = _timeProvider.GetUtcNow().UtcDateTime,
            UpdatedUtc = _timeProvider.GetUtcNow().UtcDateTime
        };

        _dbContext.MapCodeQueueConfigurations.Add(defaultConfig);
        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Created default queue configuration for MapCode {MapCode}", mapCode);

        return defaultConfig;
    }

    private async Task UpdateMapConfigStatisticsAsync(
        string mapCode,
        double opportunisticJobDistance,
        CancellationToken cancellationToken)
    {
        var config = await _dbContext.MapCodeQueueConfigurations
            .FirstOrDefaultAsync(c => c.MapCode == mapCode, cancellationToken);

        if (config == null)
            return;

        config.OpportunisticJobsChained++;

        // Update rolling average
        var totalOpportunisticDistance = config.AverageOpportunisticJobDistanceMeters * (config.OpportunisticJobsChained - 1);
        config.AverageOpportunisticJobDistanceMeters = (totalOpportunisticDistance + opportunisticJobDistance) / config.OpportunisticJobsChained;

        config.UpdatedUtc = _timeProvider.GetUtcNow().UtcDateTime;

        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    private class JobDistanceInfo
    {
        public MissionQueueItem Job { get; set; } = null!;
        public double Distance { get; set; }
        public NodePosition FirstNodePosition { get; set; } = null!;
    }
}
