using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using QES_KUKA_AMR_API.Data;
using QES_KUKA_AMR_API.Data.Entities;
using QES_KUKA_AMR_API.Models.Missions;
using QES_KUKA_AMR_API.Models.Queue;
using QES_KUKA_AMR_API.Options;

namespace QES_KUKA_AMR_API.Services.Queue;

public class RobotAssignmentService : IRobotAssignmentService
{
    private readonly ApplicationDbContext _dbContext;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly AmrServiceOptions _amrOptions;
    private readonly TimeProvider _timeProvider;
    private readonly ILogger<RobotAssignmentService> _logger;

    public RobotAssignmentService(
        ApplicationDbContext dbContext,
        IHttpClientFactory httpClientFactory,
        IOptions<AmrServiceOptions> amrOptions,
        TimeProvider timeProvider,
        ILogger<RobotAssignmentService> logger)
    {
        _dbContext = dbContext;
        _httpClientFactory = httpClientFactory;
        _amrOptions = amrOptions.Value;
        _timeProvider = timeProvider;
        _logger = logger;
    }

    public async Task<RobotAssignment?> AssignBestRobotAsync(
        MissionQueueItem queueItem,
        CancellationToken cancellationToken = default)
    {
        // Step 1: Get target position (first node of mission)
        var targetPosition = await GetFirstNodePositionAsync(queueItem, cancellationToken);
        if (targetPosition == null)
        {
            _logger.LogWarning(
                "Cannot assign robot to queue item {QueueItemId}: first node has no coordinates",
                queueItem.Id
            );
            return null;
        }

        // Step 2: Calculate distances for all available robots on same map
        var robotScores = await CalculateRobotDistancesAsync(
            queueItem.PrimaryMapCode,
            targetPosition.X,
            targetPosition.Y,
            queueItem.Priority,
            cancellationToken
        );

        if (!robotScores.Any())
        {
            _logger.LogInformation(
                "No available robots on map {MapCode} for queue item {QueueItemId}",
                queueItem.PrimaryMapCode,
                queueItem.Id
            );
            return null;
        }

        // Step 3: Filter by robot model compatibility (if specified)
        if (!string.IsNullOrEmpty(queueItem.RobotModelsJson))
        {
            var allowedModels = JsonSerializer.Deserialize<List<string>>(queueItem.RobotModelsJson);
            if (allowedModels != null && allowedModels.Any())
            {
                robotScores = robotScores.Where(rs =>
                {
                    var robot = _dbContext.MobileRobots.Find(rs.RobotId);
                    return robot != null && allowedModels.Contains(robot.RobotTypeCode);
                }).ToList();
            }
        }

        // Step 4: Filter by specific robot IDs (if specified)
        if (!string.IsNullOrEmpty(queueItem.RobotIdsJson))
        {
            var allowedRobotIds = JsonSerializer.Deserialize<List<string>>(queueItem.RobotIdsJson);
            if (allowedRobotIds != null && allowedRobotIds.Any())
            {
                robotScores = robotScores.Where(rs => allowedRobotIds.Contains(rs.RobotId)).ToList();
            }
        }

        // Step 5: Check if any robots available after filtering
        if (!robotScores.Any())
        {
            _logger.LogInformation(
                "No suitable robots available after filtering for queue item {QueueItemId}",
                queueItem.Id
            );
            return null;
        }

        // Step 6: Select best robot (highest score)
        var bestRobot = robotScores.First();

        _logger.LogInformation(
            "Assigned robot {RobotId} to queue item {QueueItemId} " +
            "(distance: {Distance:F2}, score: {Score:F2})",
            bestRobot.RobotId,
            queueItem.Id,
            bestRobot.Distance,
            bestRobot.Score
        );

        return new RobotAssignment
        {
            RobotId = bestRobot.RobotId,
            QueueItemId = queueItem.Id,
            Distance = bestRobot.Distance,
            Score = bestRobot.Score,
            AssignedUtc = _timeProvider.GetUtcNow().UtcDateTime,
            Position = bestRobot.Position
        };
    }

    public async Task<List<RobotDistanceScore>> CalculateRobotDistancesAsync(
        string mapCode,
        double targetX,
        double targetY,
        int jobPriority,
        CancellationToken cancellationToken = default)
    {
        // Get available robots on the same map
        var robots = await GetAvailableRobotsOnMapAsync(mapCode, PositionSource.Auto, cancellationToken);

        var scores = new List<RobotDistanceScore>();

        foreach (var robot in robots)
        {
            // Calculate Euclidean distance
            var distance = Math.Sqrt(
                Math.Pow(robot.X - targetX, 2) +
                Math.Pow(robot.Y - targetY, 2)
            );

            // Calculate composite score (based on priority and distance only)
            var score = CalculateRobotScore(distance, jobPriority);

            scores.Add(new RobotDistanceScore
            {
                RobotId = robot.RobotId,
                Distance = distance,
                BatteryLevel = robot.BatteryLevel,
                Priority = jobPriority,
                Score = score,
                Position = robot
            });
        }

        // Sort by score (highest first)
        return scores.OrderByDescending(s => s.Score).ToList();
    }

    public async Task<RobotPosition?> GetRobotPositionAsync(
        string robotId,
        PositionSource source = PositionSource.Auto,
        CancellationToken cancellationToken = default)
    {
        if (source == PositionSource.RealTime)
        {
            return await GetRobotPositionFromApiAsync(robotId, cancellationToken);
        }
        else if (source == PositionSource.Cached)
        {
            return await GetRobotPositionFromDatabaseAsync(robotId, cancellationToken);
        }
        else // Auto
        {
            // Try cache first (faster)
            var cachedPosition = await GetRobotPositionFromDatabaseAsync(robotId, cancellationToken);

            // Check if cache is fresh (updated within last 30 seconds)
            if (cachedPosition != null)
            {
                var staleThreshold = _timeProvider.GetUtcNow().UtcDateTime.AddSeconds(-30);
                if (cachedPosition.UpdatedUtc > staleThreshold)
                {
                    return cachedPosition;
                }
            }

            // Cache stale or empty, query real-time API
            _logger.LogDebug("Robot position cache stale for {RobotId}, querying real-time API", robotId);
            return await GetRobotPositionFromApiAsync(robotId, cancellationToken);
        }
    }

    public async Task<NodePosition?> GetFirstNodePositionAsync(
        MissionQueueItem queueItem,
        CancellationToken cancellationToken = default)
    {
        // Parse mission steps
        var steps = JsonSerializer.Deserialize<List<MissionDataItem>>(queueItem.MissionStepsJson);
        if (steps == null || steps.Count == 0)
            return null;

        var firstStep = steps.OrderBy(s => s.Sequence).First();

        // Look up QR code position
        var qrCode = await _dbContext.QrCodes
            .Where(q => q.NodeLabel == firstStep.Position && q.MapCode == queueItem.PrimaryMapCode)
            .FirstOrDefaultAsync(cancellationToken);

        if (qrCode?.XCoordinate == null || qrCode?.YCoordinate == null)
        {
            _logger.LogWarning(
                "QR code {NodeLabel} on map {MapCode} has no coordinates",
                firstStep.Position,
                queueItem.PrimaryMapCode
            );
            return null;
        }

        return new NodePosition
        {
            NodeLabel = qrCode.NodeLabel,
            MapCode = qrCode.MapCode,
            X = qrCode.XCoordinate.Value,
            Y = qrCode.YCoordinate.Value
        };
    }

    public async Task<RobotPosition?> GetRobotLastPositionAsync(
        MissionQueueItem completedJob,
        CancellationToken cancellationToken = default)
    {
        // Parse mission steps to get the last node
        var steps = JsonSerializer.Deserialize<List<MissionDataItem>>(completedJob.MissionStepsJson);

        if (steps == null || !steps.Any())
            return null;

        // Get last step (highest sequence number)
        var lastStep = steps.OrderByDescending(s => s.Sequence).First();

        // Look up last node's coordinates
        var lastNode = await _dbContext.QrCodes
            .Where(q => q.NodeLabel == lastStep.Position && q.MapCode == completedJob.PrimaryMapCode)
            .FirstOrDefaultAsync(cancellationToken);

        if (lastNode?.XCoordinate == null || lastNode?.YCoordinate == null)
        {
            _logger.LogWarning(
                "Last node {NodeLabel} on map {MapCode} has no coordinates",
                lastStep.Position,
                completedJob.PrimaryMapCode
            );
            return null;
        }

        return new RobotPosition
        {
            RobotId = completedJob.AssignedRobotId!,
            MapCode = completedJob.PrimaryMapCode,
            X = lastNode.XCoordinate.Value,
            Y = lastNode.YCoordinate.Value,
            UpdatedUtc = _timeProvider.GetUtcNow().UtcDateTime,
            Source = PositionSource.Cached
        };
    }

    // Private helper methods

    private async Task<List<RobotPosition>> GetAvailableRobotsOnMapAsync(
        string mapCode,
        PositionSource source,
        CancellationToken cancellationToken)
    {
        if (source == PositionSource.RealTime)
        {
            return await GetRobotPositionsFromApiAsync(mapCode, cancellationToken);
        }
        else if (source == PositionSource.Cached)
        {
            return await GetRobotPositionsFromDatabaseAsync(mapCode, cancellationToken);
        }
        else // Auto
        {
            var cachedPositions = await GetRobotPositionsFromDatabaseAsync(mapCode, cancellationToken);

            var staleThreshold = _timeProvider.GetUtcNow().UtcDateTime.AddSeconds(-30);
            var hasFreshData = cachedPositions.All(p => p.UpdatedUtc > staleThreshold);

            if (hasFreshData && cachedPositions.Any())
            {
                return cachedPositions;
            }

            return await GetRobotPositionsFromApiAsync(mapCode, cancellationToken);
        }
    }

    private async Task<List<RobotPosition>> GetRobotPositionsFromDatabaseAsync(
        string mapCode,
        CancellationToken cancellationToken)
    {
        var robots = await _dbContext.MobileRobots
            .Where(r => r.MapCode == mapCode)
            .Where(r => r.XCoordinate != null && r.YCoordinate != null)
            .Where(r => r.OccupyStatus == 0) // 0 = available
            .ToListAsync(cancellationToken);

        return robots.Select(r => new RobotPosition
        {
            RobotId = r.RobotId,
            MapCode = r.MapCode,
            X = r.XCoordinate!.Value,
            Y = r.YCoordinate!.Value,
            Orientation = r.RobotOrientation,
            BatteryLevel = (int)r.BatteryLevel,
            Status = r.Status,
            OccupyStatus = r.OccupyStatus,
            CurrentMissionCode = r.MissionCode,
            UpdatedUtc = r.LastUpdateTime,
            Source = PositionSource.Cached
        }).ToList();
    }

    private async Task<RobotPosition?> GetRobotPositionFromDatabaseAsync(
        string robotId,
        CancellationToken cancellationToken)
    {
        var robot = await _dbContext.MobileRobots
            .Where(r => r.RobotId == robotId)
            .Where(r => r.XCoordinate != null && r.YCoordinate != null)
            .FirstOrDefaultAsync(cancellationToken);

        if (robot == null)
            return null;

        return new RobotPosition
        {
            RobotId = robot.RobotId,
            MapCode = robot.MapCode,
            X = robot.XCoordinate!.Value,
            Y = robot.YCoordinate!.Value,
            Orientation = robot.RobotOrientation,
            BatteryLevel = (int)robot.BatteryLevel,
            Status = robot.Status,
            OccupyStatus = robot.OccupyStatus,
            CurrentMissionCode = robot.MissionCode,
            UpdatedUtc = robot.LastUpdateTime,
            Source = PositionSource.Cached
        };
    }

    private async Task<List<RobotPosition>> GetRobotPositionsFromApiAsync(
        string mapCode,
        CancellationToken cancellationToken)
    {
        try
        {
            var httpClient = _httpClientFactory.CreateClient();

            var request = new RobotQueryRequest
            {
                MapCode = mapCode,
                RobotId = string.Empty // Empty = query all robots on map
            };

            var requestJson = JsonSerializer.Serialize(request);
            var content = new StringContent(requestJson, Encoding.UTF8, "application/json");

            var httpRequest = new HttpRequestMessage(HttpMethod.Post, _amrOptions.RobotQueryUrl)
            {
                Content = content
            };

            httpRequest.Headers.Add("language", "en");
            httpRequest.Headers.Add("accept", "*/*");
            httpRequest.Headers.Add("wizards", "FRONT_END");

            var response = await httpClient.SendAsync(httpRequest, cancellationToken);
            var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);
            var robotResponse = JsonSerializer.Deserialize<RobotQueryResponse>(responseContent);

            if (robotResponse?.Data == null || !robotResponse.Success)
            {
                _logger.LogWarning("Robot query API returned no data or failed");
                return new List<RobotPosition>();
            }

            var positions = robotResponse.Data
                .Where(r => !string.IsNullOrEmpty(r.X) && !string.IsNullOrEmpty(r.Y))
                .Where(r => r.OccupyStatus == 0) // Available only
                .Select(r => new RobotPosition
                {
                    RobotId = r.RobotId,
                    MapCode = r.MapCode ?? string.Empty,
                    X = double.Parse(r.X),
                    Y = double.Parse(r.Y),
                    Orientation = string.IsNullOrEmpty(r.RobotOrientation)
                        ? null
                        : double.Parse(r.RobotOrientation),
                    BatteryLevel = r.BatteryLevel ?? 0,
                    Status = r.Status,
                    OccupyStatus = r.OccupyStatus ?? 0,
                    CurrentMissionCode = r.MissionCode,
                    UpdatedUtc = _timeProvider.GetUtcNow().UtcDateTime,
                    Source = PositionSource.RealTime
                })
                .ToList();

            // Update database cache
            await UpdateRobotPositionCacheAsync(positions, cancellationToken);

            return positions;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error querying robot positions from API for map {MapCode}", mapCode);
            return new List<RobotPosition>();
        }
    }

    private async Task<RobotPosition?> GetRobotPositionFromApiAsync(
        string robotId,
        CancellationToken cancellationToken)
    {
        try
        {
            var httpClient = _httpClientFactory.CreateClient();

            var request = new RobotQueryRequest { RobotId = robotId };
            var requestJson = JsonSerializer.Serialize(request);
            var content = new StringContent(requestJson, Encoding.UTF8, "application/json");

            var httpRequest = new HttpRequestMessage(HttpMethod.Post, _amrOptions.RobotQueryUrl)
            {
                Content = content
            };

            httpRequest.Headers.Add("language", "en");
            httpRequest.Headers.Add("accept", "*/*");
            httpRequest.Headers.Add("wizards", "FRONT_END");

            var response = await httpClient.SendAsync(httpRequest, cancellationToken);
            var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);
            var robotResponse = JsonSerializer.Deserialize<RobotQueryResponse>(responseContent);

            if (robotResponse?.Data == null || !robotResponse.Success || !robotResponse.Data.Any())
            {
                return null;
            }

            var robotData = robotResponse.Data.First();

            return new RobotPosition
            {
                RobotId = robotData.RobotId,
                MapCode = robotData.MapCode ?? string.Empty,
                X = double.Parse(robotData.X),
                Y = double.Parse(robotData.Y),
                Orientation = string.IsNullOrEmpty(robotData.RobotOrientation)
                    ? null
                    : double.Parse(robotData.RobotOrientation),
                BatteryLevel = robotData.BatteryLevel ?? 0,
                Status = robotData.Status,
                OccupyStatus = robotData.OccupyStatus ?? 0,
                CurrentMissionCode = robotData.MissionCode,
                UpdatedUtc = _timeProvider.GetUtcNow().UtcDateTime,
                Source = PositionSource.RealTime
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error querying position for robot {RobotId} from API", robotId);
            return null;
        }
    }

    private async Task UpdateRobotPositionCacheAsync(
        List<RobotPosition> positions,
        CancellationToken cancellationToken)
    {
        foreach (var pos in positions)
        {
            var robot = await _dbContext.MobileRobots.FindAsync(new object[] { pos.RobotId }, cancellationToken);
            if (robot != null)
            {
                robot.XCoordinate = pos.X;
                robot.YCoordinate = pos.Y;
                robot.RobotOrientation = pos.Orientation;
                robot.MapCode = pos.MapCode;
                robot.BatteryLevel = pos.BatteryLevel;
                robot.Status = pos.Status;
                robot.OccupyStatus = pos.OccupyStatus;
                robot.MissionCode = pos.CurrentMissionCode;
                robot.LastUpdateTime = pos.UpdatedUtc;
            }
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    private double CalculateRobotScore(double distance, int jobPriority)
    {
        const double PriorityWeight = 100.0;
        const double DistanceWeight = -1.0;

        var score =
            (jobPriority * PriorityWeight) +
            (distance * DistanceWeight);

        return score;
    }
}
