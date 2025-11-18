namespace QES_KUKA_AMR_API.Models.MapZone;

public class MapZoneWithNodesDto
{
    public int Id { get; set; }
    public string ZoneName { get; set; } = string.Empty;
    public string ZoneCode { get; set; } = string.Empty;
    public string Nodes { get; set; } = string.Empty;
    public string MapCode { get; set; } = string.Empty;
}
