using QES_KUKA_AMR_API.Models.MapData;
using QES_KUKA_AMR_API.Models.MobileRobot;

namespace QES_KUKA_AMR_API.Services.MapData;

/// <summary>
/// In-memory cache service for realtime map data
/// Thread-safe using lock for concurrent access
/// </summary>
public class MapDataCacheService : IMapDataCacheService
{
    private readonly object _lock = new();
    private MapRealtimeDataDto? _cachedData;
    private readonly ILogger<MapDataCacheService> _logger;

    public MapDataCacheService(ILogger<MapDataCacheService> logger)
    {
        _logger = logger;
    }

    /// <inheritdoc />
    public void UpdateRealtimeData(RealtimeInfoData data)
    {
        lock (_lock)
        {
            var robots = data.RobotRealtimeList
                .Select(MapToRobotPosition)
                .ToList();

            var containers = data.ContainerRealtimeList
                .Select(MapToContainerPosition)
                .ToList();

            var errorRobots = data.ErrorRobotList
                .Select(MapToRobotPosition)
                .ToList();

            _cachedData = new MapRealtimeDataDto
            {
                Robots = robots,
                Containers = containers,
                ErrorRobots = errorRobots,
                LastUpdated = DateTime.UtcNow
            };

            _logger.LogDebug(
                "Cache updated: {RobotCount} robots, {ContainerCount} containers, {ErrorCount} error robots",
                robots.Count, containers.Count, errorRobots.Count);
        }
    }

    /// <inheritdoc />
    public MapRealtimeDataDto? GetCachedData(string? floorNumber = null, string? mapCode = null)
    {
        lock (_lock)
        {
            if (_cachedData == null)
            {
                return null;
            }

            // If no filters, return all data
            if (string.IsNullOrEmpty(floorNumber) && string.IsNullOrEmpty(mapCode))
            {
                return _cachedData;
            }

            // Apply filters
            var filteredRobots = _cachedData.Robots.AsEnumerable();
            var filteredContainers = _cachedData.Containers.AsEnumerable();
            var filteredErrorRobots = _cachedData.ErrorRobots.AsEnumerable();

            if (!string.IsNullOrEmpty(floorNumber))
            {
                filteredRobots = filteredRobots.Where(r =>
                    string.Equals(r.FloorNumber, floorNumber, StringComparison.OrdinalIgnoreCase));
                filteredContainers = filteredContainers.Where(c =>
                    string.Equals(c.FloorNumber, floorNumber, StringComparison.OrdinalIgnoreCase));
                filteredErrorRobots = filteredErrorRobots.Where(r =>
                    string.Equals(r.FloorNumber, floorNumber, StringComparison.OrdinalIgnoreCase));
            }

            if (!string.IsNullOrEmpty(mapCode))
            {
                filteredRobots = filteredRobots.Where(r =>
                    string.Equals(r.MapCode, mapCode, StringComparison.OrdinalIgnoreCase));
                filteredContainers = filteredContainers.Where(c =>
                    string.Equals(c.MapCode, mapCode, StringComparison.OrdinalIgnoreCase));
                filteredErrorRobots = filteredErrorRobots.Where(r =>
                    string.Equals(r.MapCode, mapCode, StringComparison.OrdinalIgnoreCase));
            }

            return new MapRealtimeDataDto
            {
                Robots = filteredRobots.ToList(),
                Containers = filteredContainers.ToList(),
                ErrorRobots = filteredErrorRobots.ToList(),
                LastUpdated = _cachedData.LastUpdated
            };
        }
    }

    /// <inheritdoc />
    public DateTime? GetLastUpdateTime()
    {
        lock (_lock)
        {
            return _cachedData?.LastUpdated;
        }
    }

    private static RobotPositionDto MapToRobotPosition(RobotRealtimeDto robot)
    {
        return new RobotPositionDto
        {
            RobotId = robot.RobotId,
            XCoordinate = robot.XCoordinate,
            YCoordinate = robot.YCoordinate,
            Orientation = robot.RobotOrientation,
            BatteryLevel = robot.BatteryLevel,
            RobotStatus = robot.RobotStatus,
            WarningLevel = robot.WarningLevel,
            WarningCode = robot.WarningCode,
            WarningMessage = robot.WarningMessage,
            MapCode = robot.MapCode,
            FloorNumber = robot.FloorNumber,
            MissionCode = robot.MissionCode,
            JobId = robot.JobId,
            RobotTypeCode = robot.RobotTypeCode,
            ConnectionState = robot.ConnectionState,
            LiftState = robot.LiftState,
            ContainerCode = robot.ContainerCode
        };
    }

    private static ContainerPositionDto MapToContainerPosition(ContainerRealtimeDto container)
    {
        return new ContainerPositionDto
        {
            ContainerCode = container.Code,
            XCoordinate = container.StayNodeX,
            YCoordinate = container.StayNodeY,
            Orientation = container.Orientation,
            MapCode = container.MapCode,
            FloorNumber = container.FloorNumber,
            StayNodeNumber = container.StayNodeNumber,
            Status = container.Status,
            IsCarry = container.IsCarry == 1,
            ModelCode = container.ModelCode
        };
    }
}
