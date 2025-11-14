namespace QES_KUKA_AMR_API.Models.MobileRobot;

public class MobileRobotSummaryDto
{
    public string RobotId { get; set; } = string.Empty;
    public string RobotTypeCode { get; set; } = string.Empty;
    public string MapCode { get; set; } = string.Empty;
    public string FloorNumber { get; set; } = string.Empty;
    public int Reliability { get; set; } = 0;
    public int Status { get; set; } = 0;
    public int OccupyStatus { get; set; } = 0;
    public int LastNodeNumber { get; set; } = 0;
    public double BatteryLevel { get; set; } = 0;
    public double? XCoordinate { get; set; }
    public double? YCoordinate { get; set; }
    public double? RobotOrientation { get; set; }
}
