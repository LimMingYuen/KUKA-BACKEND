namespace QES_KUKA_AMR_API.Models.MapData;

/// <summary>
/// DTO for map edge data returned to frontend
/// </summary>
public class MapEdgeDto
{
    /// <summary>
    /// Edge ID
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Starting node label
    /// </summary>
    public string BeginNodeLabel { get; set; } = string.Empty;

    /// <summary>
    /// Ending node label
    /// </summary>
    public string EndNodeLabel { get; set; } = string.Empty;

    /// <summary>
    /// Length of the edge in meters
    /// </summary>
    public double EdgeLength { get; set; }

    /// <summary>
    /// Type of edge
    /// </summary>
    public int EdgeType { get; set; }

    /// <summary>
    /// Map code this edge belongs to
    /// </summary>
    public string MapCode { get; set; } = string.Empty;

    /// <summary>
    /// Floor number
    /// </summary>
    public string FloorNumber { get; set; } = string.Empty;
}
