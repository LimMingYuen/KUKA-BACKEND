namespace QES_KUKA_AMR_API.Models.RobotRealtime;

/// <summary>
/// Lightweight DTO for real-time robot position updates via SignalR.
/// Contains only essential fields for map display (not full 250+ field RobotRealtimeDto).
/// </summary>
public class RobotPositionDto
{
    public string RobotId { get; set; } = string.Empty;
    public double XCoordinate { get; set; }
    public double YCoordinate { get; set; }
    public double RobotOrientation { get; set; }
    public int RobotStatus { get; set; }
    public string RobotStatusText { get; set; } = string.Empty;
    public double BatteryLevel { get; set; }
    public bool BatteryIsCharging { get; set; }
    public string? MapCode { get; set; }
    public string? FloorNumber { get; set; }
    public string? CurrentJobId { get; set; }
    public string? RobotTypeCode { get; set; }
    public int ConnectionState { get; set; }
    public int WarningLevel { get; set; }
    public double Velocity { get; set; }
    public DateTime Timestamp { get; set; }
}

/// <summary>
/// Batch update payload sent via SignalR
/// </summary>
public class RobotPositionUpdateDto
{
    public List<RobotPositionDto> Robots { get; set; } = new();
    public DateTime ServerTimestamp { get; set; }
    public string? MapCode { get; set; }
    public string? FloorNumber { get; set; }
}
