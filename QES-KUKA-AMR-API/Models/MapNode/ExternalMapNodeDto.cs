namespace QES_KUKA_AMR_API.Models.MapNode;

/// <summary>
/// DTO representing a map node from the external KUKA map API
/// </summary>
public class ExternalMapNodeDto
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
/// Response wrapper for the map node API
/// </summary>
public class MapNodeApiResponse
{
    public int Code { get; set; }
    public string? Msg { get; set; }
    public List<ExternalMapNodeDto>? Data { get; set; }
    public bool Succ { get; set; }
}

/// <summary>
/// Result DTO for coordinate sync operation
/// </summary>
public class CoordinateSyncResultDto
{
    public int Total { get; set; }
    public int Updated { get; set; }
    public int Skipped { get; set; }
    public string? ErrorMessage { get; set; }
}

/// <summary>
/// Combined result DTO for QR code sync with coordinates
/// </summary>
public class QrCodeWithCoordinateSyncResultDto
{
    public int QrCodeTotal { get; set; }
    public int QrCodeInserted { get; set; }
    public int QrCodeUpdated { get; set; }
    public int CoordinateTotal { get; set; }
    public int CoordinateUpdated { get; set; }
    public int CoordinateSkipped { get; set; }
    public string? CoordinateSyncError { get; set; }
}
