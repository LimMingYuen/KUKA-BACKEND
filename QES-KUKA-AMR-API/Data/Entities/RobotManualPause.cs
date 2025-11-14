using System.ComponentModel.DataAnnotations;

namespace QES_KUKA_AMR_API.Data.Entities;

public class RobotManualPause
{
    [Key]
    public int Id { get; set; }

    [Required]
    [MaxLength(50)]
    public string RobotId { get; set; } = string.Empty;

    [Required]
    [MaxLength(100)]
    public string MissionCode { get; set; } = string.Empty;

    [MaxLength(100)]
    public string? WaypointCode { get; set; }

    public DateTime PauseStartUtc { get; set; }

    public DateTime? PauseEndUtc { get; set; }

    [MaxLength(200)]
    public string? Reason { get; set; }

    public DateTime CreatedUtc { get; set; }

    public DateTime UpdatedUtc { get; set; }
}
