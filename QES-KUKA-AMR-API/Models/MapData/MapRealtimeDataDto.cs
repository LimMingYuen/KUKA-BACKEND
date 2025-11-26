namespace QES_KUKA_AMR_API.Models.MapData;

/// <summary>
/// Cached realtime data for map visualization
/// </summary>
public class MapRealtimeDataDto
{
    /// <summary>
    /// List of robot positions
    /// </summary>
    public List<RobotPositionDto> Robots { get; set; } = new();

    /// <summary>
    /// List of container positions
    /// </summary>
    public List<ContainerPositionDto> Containers { get; set; } = new();

    /// <summary>
    /// List of robots with errors
    /// </summary>
    public List<RobotPositionDto> ErrorRobots { get; set; } = new();

    /// <summary>
    /// When this data was last updated from external API
    /// </summary>
    public DateTime LastUpdated { get; set; }
}

/// <summary>
/// Robot position and status for map visualization
/// </summary>
public class RobotPositionDto
{
    /// <summary>
    /// Robot identifier
    /// </summary>
    public string RobotId { get; set; } = string.Empty;

    /// <summary>
    /// X coordinate position on the map
    /// </summary>
    public double XCoordinate { get; set; }

    /// <summary>
    /// Y coordinate position on the map
    /// </summary>
    public double YCoordinate { get; set; }

    /// <summary>
    /// Robot orientation/heading angle in degrees
    /// </summary>
    public double Orientation { get; set; }

    /// <summary>
    /// Battery level percentage (0-100)
    /// </summary>
    public double BatteryLevel { get; set; }

    /// <summary>
    /// Robot operational status code
    /// </summary>
    public int RobotStatus { get; set; }

    /// <summary>
    /// Warning severity level (0=none)
    /// </summary>
    public int WarningLevel { get; set; }

    /// <summary>
    /// Warning code if any
    /// </summary>
    public string? WarningCode { get; set; }

    /// <summary>
    /// Warning message if any
    /// </summary>
    public string? WarningMessage { get; set; }

    /// <summary>
    /// Map code where robot is located
    /// </summary>
    public string? MapCode { get; set; }

    /// <summary>
    /// Floor number where robot is located
    /// </summary>
    public string? FloorNumber { get; set; }

    /// <summary>
    /// Current mission code if any
    /// </summary>
    public string? MissionCode { get; set; }

    /// <summary>
    /// Current job ID if any
    /// </summary>
    public string? JobId { get; set; }

    /// <summary>
    /// Robot type code
    /// </summary>
    public string? RobotTypeCode { get; set; }

    /// <summary>
    /// Connection state (1=connected)
    /// </summary>
    public int ConnectionState { get; set; }

    /// <summary>
    /// Whether robot is carrying a container
    /// </summary>
    public bool LiftState { get; set; }

    /// <summary>
    /// Container code if carrying
    /// </summary>
    public string? ContainerCode { get; set; }
}

/// <summary>
/// Container position for map visualization
/// </summary>
public class ContainerPositionDto
{
    /// <summary>
    /// Container code/identifier
    /// </summary>
    public string ContainerCode { get; set; } = string.Empty;

    /// <summary>
    /// X coordinate position on the map
    /// </summary>
    public double XCoordinate { get; set; }

    /// <summary>
    /// Y coordinate position on the map
    /// </summary>
    public double YCoordinate { get; set; }

    /// <summary>
    /// Container orientation angle
    /// </summary>
    public double Orientation { get; set; }

    /// <summary>
    /// Map code where container is located
    /// </summary>
    public string? MapCode { get; set; }

    /// <summary>
    /// Floor number where container is located
    /// </summary>
    public string? FloorNumber { get; set; }

    /// <summary>
    /// Node number where container is stationed
    /// </summary>
    public int StayNodeNumber { get; set; }

    /// <summary>
    /// Container status code
    /// </summary>
    public int Status { get; set; }

    /// <summary>
    /// Whether container is being carried
    /// </summary>
    public bool IsCarry { get; set; }

    /// <summary>
    /// Container model code
    /// </summary>
    public string? ModelCode { get; set; }
}
