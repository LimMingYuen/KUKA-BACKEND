namespace QES_KUKA_AMR_API.Models.MobileRobot;

public class MobileRobotDto
{
    public int Id { get; set; }
    public string CreateTime { get; set; } = string.Empty;
    public string CreateBy { get; set; } = string.Empty;
    public string CreateApp { get; set; } = string.Empty;
    public string LastUpdateTime { get; set; } = string.Empty;
    public string LastUpdateBy { get; set; } = string.Empty;
    public string LastUpdateApp { get; set; } = string.Empty;
    public string RobotId { get; set; } = string.Empty;
    public string RobotTypeCode { get; set; } = string.Empty;
    public string BuildingCode { get; set; } = string.Empty;
    public string MapCode { get; set; } = string.Empty;
    public string FloorNumber { get; set; } = string.Empty;
    public int LastNodeNumber { get; set; }
    public bool LastNodeDeleteFlag { get; set; }
    public string ContainerCode { get; set; } = string.Empty;
    public int ActuatorType { get; set; }
    public string ActuatorStatusInfo { get; set; } = string.Empty;
    public string IpAddress { get; set; } = string.Empty;
    public string WarningInfo { get; set; } = string.Empty;
    public string ConfigVersion { get; set; } = string.Empty;
    public string SendConfigVersion { get; set; } = string.Empty;
    public string SendConfigTime { get; set; } = string.Empty;
    public string FirmwareVersion { get; set; } = string.Empty;
    public string SendFirmwareVersion { get; set; } = string.Empty;
    public string SendFirmwareTime { get; set; } = string.Empty;
    public int Status { get; set; }
    public int OccupyStatus { get; set; }
    public double? BatteryLevel { get; set; }  // Changed from int? to double?
    public double? Mileage { get; set; }       // Changed from int to double?
    public string MissionCode { get; set; } = string.Empty;
    public int MeetObstacleStatus { get; set; }
    public double? RobotOrientation { get; set; }  // Already double?
    public int? Reliability { get; set; }
    public int? RunTime { get; set; }
    public int RobotTypeClass { get; set; }
    public string TrailerNum { get; set; } = string.Empty;
    public string TractionStatus { get; set; } = string.Empty;
    public double? XCoordinate { get; set; }
    public double? YCoordinate { get; set; }
}
