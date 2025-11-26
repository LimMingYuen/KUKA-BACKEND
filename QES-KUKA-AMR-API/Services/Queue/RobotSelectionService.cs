using QES_KUKA_AMR_API.Models.Missions;

namespace QES_KUKA_AMR_API.Services.Queue;

public interface IRobotSelectionService
{
    /// <summary>
    /// Select an available robot from the list using sequential selection logic.
    /// First robot matching all conditions wins. If none match, returns the first robot as fallback.
    /// </summary>
    /// <param name="robots">List of robots to select from</param>
    /// <param name="robotTypeFilter">Optional robot type filter</param>
    /// <param name="preferredRobotIds">Optional comma-separated list of preferred robot IDs</param>
    /// <returns>Selected robot ID, or null if no robots available</returns>
    string? SelectAvailableRobot(
        List<RobotDataDto> robots,
        string? robotTypeFilter = null,
        string? preferredRobotIds = null);

    /// <summary>
    /// Check if a specific robot is available for mission assignment
    /// </summary>
    bool IsRobotAvailable(RobotDataDto robot);
}

/// <summary>
/// Robot status values from external AMR system
/// </summary>
public static class RobotStatusCodes
{
    public const int Unknown = 0;
    public const int Available = 1;
    public const int Busy = 2;
    public const int Error = 3;
    public const int Maintenance = 4;
    public const int Charging = 5;
    public const int Offline = 6;
}

/// <summary>
/// Robot occupy status values from external AMR system
/// </summary>
public static class RobotOccupyStatusCodes
{
    public const int Unknown = 0;
    public const int Free = 1;
    public const int Occupied = 2;
    public const int Reserved = 3;
}

public class RobotSelectionService : IRobotSelectionService
{
    private readonly ILogger<RobotSelectionService> _logger;

    // Minimum battery level required for mission assignment
    private const int MinimumBatteryLevel = 20;

    public RobotSelectionService(ILogger<RobotSelectionService> logger)
    {
        _logger = logger;
    }

    public string? SelectAvailableRobot(
        List<RobotDataDto> robots,
        string? robotTypeFilter = null,
        string? preferredRobotIds = null)
    {
        if (robots == null || robots.Count == 0)
        {
            _logger.LogWarning("No robots provided for selection");
            return null;
        }

        // Parse preferred robot IDs if provided
        var preferredIds = string.IsNullOrEmpty(preferredRobotIds)
            ? new HashSet<string>()
            : new HashSet<string>(preferredRobotIds.Split(',').Select(id => id.Trim()));

        // Filter robots by type if specified
        var candidateRobots = robots;
        if (!string.IsNullOrEmpty(robotTypeFilter))
        {
            candidateRobots = robots.Where(r =>
                string.Equals(r.RobotType, robotTypeFilter, StringComparison.OrdinalIgnoreCase))
                .ToList();

            _logger.LogDebug("Filtered to {Count} robots of type {RobotType}",
                candidateRobots.Count, robotTypeFilter);
        }

        // If preferred IDs specified, prioritize those robots
        if (preferredIds.Count > 0)
        {
            var preferredRobots = candidateRobots
                .Where(r => preferredIds.Contains(r.RobotId))
                .ToList();

            if (preferredRobots.Count > 0)
            {
                candidateRobots = preferredRobots;
                _logger.LogDebug("Using {Count} preferred robots", candidateRobots.Count);
            }
        }

        // Sort robots by ID for consistent sequential selection
        var sortedRobots = candidateRobots.OrderBy(r => r.RobotId).ToList();

        _logger.LogDebug("Selecting from {Count} candidate robots", sortedRobots.Count);

        // Sequential selection: find first robot matching all conditions
        foreach (var robot in sortedRobots)
        {
            if (IsRobotAvailable(robot))
            {
                _logger.LogInformation("Selected available robot {RobotId} (Status: {Status}, Battery: {Battery}%)",
                    robot.RobotId, robot.Status, robot.BatteryLevel);
                return robot.RobotId;
            }

            _logger.LogDebug("Robot {RobotId} not available (Status: {Status}, OccupyStatus: {OccupyStatus}, Battery: {Battery}%)",
                robot.RobotId, robot.Status, robot.OccupyStatus, robot.BatteryLevel);
        }

        // Fallback: return first robot if none match conditions
        if (sortedRobots.Count > 0)
        {
            var fallbackRobot = sortedRobots[0];
            _logger.LogWarning("No available robot found. Using fallback robot {RobotId} (Status: {Status}, Battery: {Battery}%)",
                fallbackRobot.RobotId, fallbackRobot.Status, fallbackRobot.BatteryLevel);
            return fallbackRobot.RobotId;
        }

        _logger.LogWarning("No robots available for selection");
        return null;
    }

    public bool IsRobotAvailable(RobotDataDto robot)
    {
        // Check status is Available (1)
        if (robot.Status != RobotStatusCodes.Available)
        {
            return false;
        }

        // Check occupy status is Free (1) - allow Reserved (3) as well
        if (robot.OccupyStatus.HasValue &&
            robot.OccupyStatus != RobotOccupyStatusCodes.Free &&
            robot.OccupyStatus != RobotOccupyStatusCodes.Reserved)
        {
            return false;
        }

        // Check battery level is above minimum
        if (robot.BatteryLevel.HasValue && robot.BatteryLevel < MinimumBatteryLevel)
        {
            return false;
        }

        return true;
    }
}
