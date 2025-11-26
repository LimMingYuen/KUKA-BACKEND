using System.Text.Json.Serialization;

namespace QES_KUKA_AMR_API.Models.MapData;

/// <summary>
/// Zone polygon data for Cytoscape map visualization
/// </summary>
public class MapZoneDisplayDto
{
    /// <summary>
    /// Internal database ID
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Zone display name
    /// </summary>
    public string ZoneName { get; set; } = string.Empty;

    /// <summary>
    /// Unique zone code
    /// </summary>
    public string ZoneCode { get; set; } = string.Empty;

    /// <summary>
    /// Hex color for zone rendering (e.g., "#FF5733")
    /// </summary>
    public string ZoneColor { get; set; } = string.Empty;

    /// <summary>
    /// Zone type identifier
    /// </summary>
    public string ZoneType { get; set; } = string.Empty;

    /// <summary>
    /// Map code this zone belongs to
    /// </summary>
    public string MapCode { get; set; } = string.Empty;

    /// <summary>
    /// Floor number this zone belongs to
    /// </summary>
    public string FloorNumber { get; set; } = string.Empty;

    /// <summary>
    /// Polygon points defining zone boundary
    /// </summary>
    public List<PolygonPoint> PolygonPoints { get; set; } = new();

    /// <summary>
    /// Node IDs/UUIDs contained in this zone
    /// </summary>
    public List<string> NodeIds { get; set; } = new();

    /// <summary>
    /// Zone status (1=Enabled, 0=Disabled)
    /// </summary>
    public int Status { get; set; }
}

/// <summary>
/// A point in a polygon boundary
/// </summary>
public class PolygonPoint
{
    [JsonPropertyName("x")]
    public double X { get; set; }

    [JsonPropertyName("y")]
    public double Y { get; set; }
}
