namespace QES_KUKA_AMR_API.Models.MapZone;

public class MapZonePage
{
    public int TotalPages { get; set; }
    public int TotalRecords { get; set; }
    public int Page { get; set; }
    public int Size { get; set; }
    public List<MapZoneDto>? Content { get; set; }
}
