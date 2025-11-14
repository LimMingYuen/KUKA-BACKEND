namespace QES_KUKA_AMR_API.Models.MapZone;

public class MapZoneSummaryDto
{
    public int Id { get; set; }
    public string ZoneName { get; set; } = string.Empty;
    public string ZoneCode { get; set; } = string.Empty;
    public string Layout { get; set; } = string.Empty;
    public string AreaPurpose { get; set; } = string.Empty;
    public string StatusText { get; set; } = string.Empty;
}
