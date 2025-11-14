namespace QES_KUKA_AMR_API.Models.MapZone;

public class MapZoneDto
{
    public int Id { get; set; }
    public string? CreateTime { get; set; }
    public string? CreateBy { get; set; }
    public string? CreateApp { get; set; }
    public string? LastUpdateTime { get; set; }
    public string? LastUpdateBy { get; set; }
    public string? LastUpdateApp { get; set; }
    public string? ZoneName { get; set; }
    public string? ZoneCode { get; set; }
    public string? ZoneDescription { get; set; }
    public string? ZoneColor { get; set; }
    public string? MapCode { get; set; }
    public string? FloorNumber { get; set; }
    public string? Points { get; set; }
    public string? Nodes { get; set; }
    public string? Edges { get; set; }
    public string? CustomerUi { get; set; }
    public string? ZoneType { get; set; }
    public int Status { get; set; }
    public string? BeginTime { get; set; }
    public string? EndTime { get; set; }
    public MapZoneConfigsDto? Configs { get; set; }
}
