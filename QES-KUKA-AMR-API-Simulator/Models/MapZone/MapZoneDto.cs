namespace QES_KUKA_AMR_API_Simulator.Models.MapZone;

public class MapZoneDto
{
    public int Id { get; set; }
    public string CreateTime { get; set; } = string.Empty;
    public string CreateBy { get; set; } = string.Empty;
    public string CreateApp { get; set; } = string.Empty;
    public string LastUpdateTime { get; set; } = string.Empty;
    public string LastUpdateBy { get; set; } = string.Empty;
    public string LastUpdateApp { get; set; } = string.Empty;
    public string ZoneName { get; set; } = string.Empty;
    public string ZoneCode { get; set; } = string.Empty;
    public string ZoneDescription { get; set; } = string.Empty;
    public string ZoneColor { get; set; } = string.Empty;
    public string MapCode { get; set; } = string.Empty;
    public string FloorNumber { get; set; } = string.Empty;
    public string Points { get; set; } = string.Empty;
    public string Nodes { get; set; } = string.Empty;
    public string Edges { get; set; } = string.Empty;
    public string CustomerUi { get; set; } = string.Empty;
    public string ZoneType { get; set; } = string.Empty;
    public int Status { get; set; }
    public string? BeginTime { get; set; }
    public string? EndTime { get; set; }
    public MapZoneConfigsDto? Configs { get; set; }
}
