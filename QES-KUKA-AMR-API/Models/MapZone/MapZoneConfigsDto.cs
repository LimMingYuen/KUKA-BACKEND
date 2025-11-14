namespace QES_KUKA_AMR_API.Models.MapZone;

public class MapZoneConfigsDto
{
    public string? SpecifiedRobotIds { get; set; }
    public string? AgvSelection { get; set; }
    public string? RobotZoneMode { get; set; }
    public string? RestSelection { get; set; }
    public string? ChargeSelection { get; set; }
    public string? SelectRobotStrategy { get; set; }
    public string? Interval { get; set; }
    public string? Radius { get; set; }
    public string? Attempts { get; set; }
    public string? CarryStrategy { get; set; }
    public string? AreaType { get; set; }
    public string? AreaDefaultContainerModelCode { get; set; }
    public string? AreaNodeList { get; set; }
}
