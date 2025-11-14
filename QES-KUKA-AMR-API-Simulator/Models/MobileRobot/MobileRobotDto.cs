namespace QES_KUKA_AMR_API_Simulator.Models.MobileRobot
{
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
        public string? SendConfigTime { get; set; } = string.Empty;
        public string FirmwareVersion { get; set; } = string.Empty;
        public string SendFirmwareVersion { get; set; } = string.Empty;
        public string? SendFirmwareTime { get; set; }
        public int Status { get; set; }
        public int OccupyStatus { get; set; }
        public int BatteryLevel { get; set; }
        public int Mileage { get; set; }
        public string MissionCode { get; set; } = string.Empty;
        public int MeetObstacleStatus { get; set; }
        public int? RobotOrientation { get; set; }
        public int? Reliability { get; set; }
        public int? RunTime { get; set; }
        public int RobotTypeClass { get; set; }
        public string? TrailerNum { get; set; }
        public string? TractionStatus { get; set; }
        public double? XCoordinate { get; set; }
        public double? YCoordinate { get; set; }

    }
}
