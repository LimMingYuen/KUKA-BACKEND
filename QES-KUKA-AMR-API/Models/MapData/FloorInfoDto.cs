namespace QES_KUKA_AMR_API.Models.MapData;

/// <summary>
/// Represents a floor/map tab for the warehouse map visualization
/// </summary>
public class FloorInfoDto
{
    /// <summary>
    /// Floor number identifier
    /// </summary>
    public string FloorNumber { get; set; } = string.Empty;

    /// <summary>
    /// Map code identifier
    /// </summary>
    public string MapCode { get; set; } = string.Empty;

    /// <summary>
    /// Display name for the tab (e.g., "Floor 1 - MAP1")
    /// </summary>
    public string DisplayName { get; set; } = string.Empty;

    /// <summary>
    /// Number of QR code nodes on this floor/map
    /// </summary>
    public int NodeCount { get; set; }

    /// <summary>
    /// Number of zones on this floor/map
    /// </summary>
    public int ZoneCount { get; set; }
}
