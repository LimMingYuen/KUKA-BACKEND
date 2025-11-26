namespace QES_KUKA_AMR_API.Models.MapData;

/// <summary>
/// QR code node data for Cytoscape map visualization
/// </summary>
public class MapNodeDto
{
    /// <summary>
    /// Internal database ID
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Node label/name displayed on the map
    /// </summary>
    public string NodeLabel { get; set; } = string.Empty;

    /// <summary>
    /// X coordinate position on the map
    /// </summary>
    public double XCoordinate { get; set; }

    /// <summary>
    /// Y coordinate position on the map
    /// </summary>
    public double YCoordinate { get; set; }

    /// <summary>
    /// Node number for identification
    /// </summary>
    public int NodeNumber { get; set; }

    /// <summary>
    /// Node type (1=standard, etc.)
    /// </summary>
    public int? NodeType { get; set; }

    /// <summary>
    /// Map code this node belongs to
    /// </summary>
    public string MapCode { get; set; } = string.Empty;

    /// <summary>
    /// Floor number this node belongs to
    /// </summary>
    public string FloorNumber { get; set; } = string.Empty;

    /// <summary>
    /// Unique identifier from external system
    /// </summary>
    public string? NodeUuid { get; set; }

    /// <summary>
    /// Allowed transit orientations (e.g., "0,180")
    /// </summary>
    public string? TransitOrientations { get; set; }
}
