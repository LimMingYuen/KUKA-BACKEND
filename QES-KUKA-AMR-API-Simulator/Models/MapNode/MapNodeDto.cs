namespace QES_KUKA_AMR_API_Simulator.Models.MapNode;

/// <summary>
/// DTO for map node data returned by the simulator
/// </summary>
public class MapNodeDto
{
    public string? CellCode { get; set; }
    public string? BuildingCode { get; set; }
    public string? MapCode { get; set; }
    public string? FloorNumber { get; set; }
    public string? FloorName { get; set; }
    public string? FloorLevel { get; set; }
    public string? X { get; set; }
    public string? Y { get; set; }
    public string? NodeLabel { get; set; }
    public string? ForeignCode { get; set; }
    public string? NodeNumber { get; set; }
    public int? NodeId { get; set; }
    public int? NodeFunctionType { get; set; }
    public bool? LiftPointFlag { get; set; }
    public bool? NeedQueue { get; set; }
    public int? QueuePointCount { get; set; }
    public int? MapNodeNumber { get; set; }
    public string? NodeCode { get; set; }
    public int? AreaNodeType { get; set; }
    public string? RobotStopAngle { get; set; }
    public int? OpportunityCharging { get; set; }
}

/// <summary>
/// API response wrapper for map node list
/// </summary>
public class MapNodeApiResponse
{
    public int Code { get; set; }
    public string? Msg { get; set; }
    public List<MapNodeDto>? Data { get; set; }
    public bool Succ { get; set; }
}
