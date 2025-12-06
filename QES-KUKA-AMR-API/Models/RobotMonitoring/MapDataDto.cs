namespace QES_KUKA_AMR_API.Models.RobotMonitoring;

/// <summary>
/// Aggregated map data containing nodes, zones, and robots for rendering
/// </summary>
public class MapDataDto
{
    /// <summary>
    /// QR code nodes for the map
    /// </summary>
    public List<MapNodeDto> Nodes { get; set; } = new();

    /// <summary>
    /// Map zones with boundary polygons
    /// </summary>
    public List<MapZoneDto> Zones { get; set; } = new();
}

/// <summary>
/// Simplified QR code node data for map display
/// </summary>
public class MapNodeDto
{
    public int Id { get; set; }
    public string NodeLabel { get; set; } = string.Empty;
    public string? NodeUuid { get; set; }
    public int NodeNumber { get; set; }
    public double X { get; set; }
    public double Y { get; set; }
    public int? NodeType { get; set; }
    public string MapCode { get; set; } = string.Empty;
    public string FloorNumber { get; set; } = string.Empty;
}

/// <summary>
/// Simplified zone data for map display
/// </summary>
public class MapZoneDto
{
    public int Id { get; set; }
    public string ZoneName { get; set; } = string.Empty;
    public string ZoneCode { get; set; } = string.Empty;
    public string? ZoneColor { get; set; }
    public string ZoneType { get; set; } = string.Empty;

    /// <summary>
    /// Boundary points of the zone polygon
    /// </summary>
    public List<PointDto> Points { get; set; } = new();

    /// <summary>
    /// Node labels contained in this zone
    /// </summary>
    public List<string> NodeLabels { get; set; } = new();

    public string MapCode { get; set; } = string.Empty;
    public string FloorNumber { get; set; } = string.Empty;
}

/// <summary>
/// 2D coordinate point
/// </summary>
public class PointDto
{
    public double X { get; set; }
    public double Y { get; set; }
}

/// <summary>
/// Robot position data for map display
/// </summary>
public class RobotPositionDto
{
    public string RobotId { get; set; } = string.Empty;
    public string? RobotTypeCode { get; set; }
    public double X { get; set; }
    public double Y { get; set; }
    public double Orientation { get; set; }
    public int Status { get; set; }
    public string StatusText { get; set; } = string.Empty;
    public int OccupyStatus { get; set; }
    public double BatteryLevel { get; set; }
    public string? MissionCode { get; set; }
    public int? LastNodeNumber { get; set; }
    public string? WarningInfo { get; set; }
    public string MapCode { get; set; } = string.Empty;
    public string FloorNumber { get; set; } = string.Empty;
}

/// <summary>
/// Real-time robot positions response
/// </summary>
public class RobotPositionsResponse
{
    public List<RobotPositionDto> Robots { get; set; } = new();
    public DateTime Timestamp { get; set; }
}
