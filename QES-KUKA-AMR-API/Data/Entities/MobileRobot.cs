using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace QES_KUKA_AMR_API.Data.Entities
{
    [Table("MobileRobots")]
    public class MobileRobot
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        public DateTime CreateTime { get; set; }

        [MaxLength(100)]
        public string CreateBy { get; set; } = string.Empty;

        [MaxLength(100)]
        public string CreateApp { get; set; } = string.Empty;

        public DateTime LastUpdateTime { get; set; }

        [MaxLength(100)]
        public string LastUpdateBy { get; set; } = string.Empty;

        [MaxLength(100)]
        public string LastUpdateApp { get; set; } = string.Empty;

        [MaxLength(100)]
        public string RobotId { get; set; } = string.Empty;

        [MaxLength(100)]
        public string RobotTypeCode { get; set; } = string.Empty;

        [MaxLength(100)]
        public string BuildingCode { get; set; } = string.Empty;

        [MaxLength(100)]
        public string MapCode { get; set; } = string.Empty;

        [MaxLength(50)]
        public string FloorNumber { get; set; } = string.Empty;

        public int LastNodeNumber { get; set; }

        public bool LastNodeDeleteFlag { get; set; }

        [MaxLength(100)]
        public string ContainerCode { get; set; } = string.Empty;

        public int ActuatorType { get; set; }

        [MaxLength(255)]
        public string ActuatorStatusInfo { get; set; } = string.Empty;

        [MaxLength(50)]
        public string IpAddress { get; set; } = string.Empty;

        [MaxLength(255)]
        public string WarningInfo { get; set; } = string.Empty;

        [MaxLength(50)]
        public string ConfigVersion { get; set; } = string.Empty;

        [MaxLength(50)]
        public string SendConfigVersion { get; set; } = string.Empty;

        [MaxLength(100)]
        public DateTime SendConfigTime { get; set; }

        [MaxLength(100)]
        public string FirmwareVersion { get; set; } = string.Empty;

        [MaxLength(100)]
        public string SendFirmwareVersion { get; set; } = string.Empty;

        public DateTime SendFirmwareTime { get; set; }

        public int Status { get; set; }

        public int OccupyStatus { get; set; }

        public double BatteryLevel { get; set; } = 0;

        public double Mileage { get; set; }

        [MaxLength(100)]
        public string MissionCode { get; set; } = string.Empty;

        public int MeetObstacleStatus { get; set; }

        public double? RobotOrientation { get; set; }

        public int Reliability { get; set; }

        public int? RunTime { get; set; }

        public int? RobotTypeClass { get; set; }

        [MaxLength(100)]
        public string TrailerNum { get; set; } = string.Empty;

        [MaxLength(100)]
        public string TractionStatus { get; set; } = string.Empty;

        public double? XCoordinate { get; set; }

        public double? YCoordinate { get; set; }
    }
}
