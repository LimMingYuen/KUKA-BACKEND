using System.ComponentModel.DataAnnotations;

namespace QES_KUKA_AMR_API.Models.RobotMonitoring;

/// <summary>
/// DTO for robot monitoring map configuration
/// </summary>
public class RobotMonitoringMapDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? MapCode { get; set; }
    public string? FloorNumber { get; set; }
    public string? BackgroundImagePath { get; set; }
    public string? BackgroundImageOriginalName { get; set; }
    public int? ImageWidth { get; set; }
    public int? ImageHeight { get; set; }
    public DisplaySettings? DisplaySettings { get; set; }
    public List<CustomNodeDto>? CustomNodes { get; set; }
    public List<CustomZoneDto>? CustomZones { get; set; }
    public List<CustomLineDto>? CustomLines { get; set; }
    public bool IsDefault { get; set; }
    public string CreatedBy { get; set; } = string.Empty;
    public DateTime CreatedUtc { get; set; }
    public string? LastUpdatedBy { get; set; }
    public DateTime? LastUpdatedUtc { get; set; }
}

/// <summary>
/// Summary DTO for listing robot monitoring maps
/// </summary>
public class RobotMonitoringMapSummaryDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? MapCode { get; set; }
    public string? FloorNumber { get; set; }
    public bool HasBackgroundImage { get; set; }
    public bool IsDefault { get; set; }
    public string CreatedBy { get; set; } = string.Empty;
    public DateTime CreatedUtc { get; set; }
    public DateTime? LastUpdatedUtc { get; set; }
}

/// <summary>
/// Display settings for map visualization
/// </summary>
public class DisplaySettings
{
    /// <summary>
    /// Whether to show QR code nodes on the map
    /// </summary>
    public bool ShowNodes { get; set; } = true;

    /// <summary>
    /// Whether to show map zones on the map
    /// </summary>
    public bool ShowZones { get; set; } = true;

    /// <summary>
    /// Whether to show labels on nodes
    /// </summary>
    public bool ShowLabels { get; set; } = true;

    /// <summary>
    /// Size of node markers in pixels
    /// </summary>
    public int NodeSize { get; set; } = 10;

    /// <summary>
    /// Opacity of zone polygons (0-1)
    /// </summary>
    public double ZoneOpacity { get; set; } = 0.3;
}

/// <summary>
/// Request to create a new robot monitoring map configuration
/// </summary>
public class CreateRobotMonitoringMapRequest
{
    [Required]
    [MaxLength(256)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(1000)]
    public string? Description { get; set; }

    [MaxLength(128)]
    public string? MapCode { get; set; }

    [MaxLength(16)]
    public string? FloorNumber { get; set; }

    public DisplaySettings? DisplaySettings { get; set; }

    public List<CustomNodeDto>? CustomNodes { get; set; }

    public List<CustomZoneDto>? CustomZones { get; set; }

    public List<CustomLineDto>? CustomLines { get; set; }

    public bool IsDefault { get; set; }
}

/// <summary>
/// Request to update an existing robot monitoring map configuration
/// </summary>
public class UpdateRobotMonitoringMapRequest
{
    [MaxLength(256)]
    public string? Name { get; set; }

    [MaxLength(1000)]
    public string? Description { get; set; }

    [MaxLength(128)]
    public string? MapCode { get; set; }

    [MaxLength(16)]
    public string? FloorNumber { get; set; }

    public DisplaySettings? DisplaySettings { get; set; }

    public List<CustomNodeDto>? CustomNodes { get; set; }

    public List<CustomZoneDto>? CustomZones { get; set; }

    public List<CustomLineDto>? CustomLines { get; set; }

    public bool? IsDefault { get; set; }
}

/// <summary>
/// Response after uploading a background image
/// </summary>
public class ImageUploadResponse
{
    public bool Success { get; set; }
    public string? ImagePath { get; set; }
    public string? OriginalName { get; set; }
    public int? Width { get; set; }
    public int? Height { get; set; }
    public string? Message { get; set; }
}

/// <summary>
/// Custom node placed by user on the map
/// </summary>
public class CustomNodeDto
{
    /// <summary>
    /// Unique identifier (UUID)
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Display label for the node
    /// </summary>
    public string Label { get; set; } = string.Empty;

    /// <summary>
    /// X coordinate on the map
    /// </summary>
    public double X { get; set; }

    /// <summary>
    /// Y coordinate on the map
    /// </summary>
    public double Y { get; set; }

    /// <summary>
    /// Optional color for the node marker
    /// </summary>
    public string? Color { get; set; }
}

/// <summary>
/// Custom zone drawn by user on the map
/// </summary>
public class CustomZoneDto
{
    /// <summary>
    /// Unique identifier (UUID)
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Display name for the zone
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Fill color for the zone polygon
    /// </summary>
    public string? Color { get; set; }

    /// <summary>
    /// Opacity of the zone fill (0-1)
    /// </summary>
    public double Opacity { get; set; } = 0.3;

    /// <summary>
    /// Points defining the polygon boundary
    /// </summary>
    public List<PointDto> Points { get; set; } = new();
}

/// <summary>
/// Custom line connecting two nodes on the map
/// </summary>
public class CustomLineDto
{
    /// <summary>
    /// Unique identifier (UUID)
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// ID of the start node
    /// </summary>
    public string FromNodeId { get; set; } = string.Empty;

    /// <summary>
    /// ID of the end node
    /// </summary>
    public string ToNodeId { get; set; } = string.Empty;

    /// <summary>
    /// Optional color for the line
    /// </summary>
    public string? Color { get; set; }

    /// <summary>
    /// Line thickness/weight
    /// </summary>
    public double? Weight { get; set; }
}
