namespace QES_KUKA_AMR_API.Models.Queue;

public class RobotPosition
{
    public string RobotId { get; set; } = string.Empty;
    public string MapCode { get; set; } = string.Empty;
    public double X { get; set; }
    public double Y { get; set; }
    public double? Orientation { get; set; }
    public int BatteryLevel { get; set; }
    public int Status { get; set; }
    public int OccupyStatus { get; set; }
    public string? CurrentMissionCode { get; set; }
    public DateTime UpdatedUtc { get; set; }
    public PositionSource Source { get; set; }
}

public enum PositionSource
{
    Cached = 0,
    RealTime = 1,
    Auto = 2
}
