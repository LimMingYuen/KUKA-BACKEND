namespace QES_KUKA_AMR_API_Simulator.Models.MapZone;

/// <summary>
/// Represents a single node/cell within an area's node list
/// Used to parse the areaNodeList JSON from MapZone.Configs
/// </summary>
public class AreaNodeDto
{
    /// <summary>
    /// Cell/node code (e.g., "Sim1-1-1")
    /// </summary>
    public string CellCode { get; set; } = string.Empty;

    /// <summary>
    /// Sort order of the node within the area
    /// </summary>
    public int Sort { get; set; }
}
