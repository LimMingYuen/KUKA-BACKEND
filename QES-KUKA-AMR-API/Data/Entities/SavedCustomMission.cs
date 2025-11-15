using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace QES_KUKA_AMR_API.Data.Entities;

/// <summary>
/// Represents a saved custom mission template that can be retriggered multiple times.
/// Each trigger creates a new MissionQueue entry with fresh IDs.
/// </summary>
[Table("SavedCustomMissions")]
public class SavedCustomMission
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    /// <summary>
    /// User-friendly name for the saved mission (e.g., "Warehouse Pickup Route A")
    /// </summary>
    [Required]
    [MaxLength(200)]
    public string MissionName { get; set; } = string.Empty;

    /// <summary>
    /// Optional description of what this mission does
    /// </summary>
    [MaxLength(500)]
    public string? Description { get; set; }

    /// <summary>
    /// Mission type, e.g., "RACK_MOVE", "DELIVERY"
    /// </summary>
    [Required]
    [MaxLength(50)]
    public string MissionType { get; set; } = string.Empty;

    /// <summary>
    /// Robot type, e.g., "LIFT", "LATENT"
    /// </summary>
    [Required]
    [MaxLength(50)]
    public string RobotType { get; set; } = string.Empty;

    /// <summary>
    /// Priority level for missions (typically 1)
    /// </summary>
    [Required]
    public int Priority { get; set; } = 1;

    /// <summary>
    /// Comma-separated list of robot models, e.g., "KMP600I,KMP1500"
    /// </summary>
    [MaxLength(500)]
    public string? RobotModels { get; set; }

    /// <summary>
    /// Comma-separated list of robot IDs, e.g., "14,15"
    /// </summary>
    [MaxLength(500)]
    public string? RobotIds { get; set; }

    /// <summary>
    /// Container model code
    /// </summary>
    [MaxLength(100)]
    public string? ContainerModelCode { get; set; }

    /// <summary>
    /// Container code
    /// </summary>
    [MaxLength(100)]
    public string? ContainerCode { get; set; }

    /// <summary>
    /// Idle node/position where robot should go after completion
    /// </summary>
    [MaxLength(100)]
    public string? IdleNode { get; set; }

    /// <summary>
    /// JSON array of mission steps (sequence, position, type, putDown, passStrategy, waitingMillis)
    /// </summary>
    [Required]
    public string MissionStepsJson { get; set; } = string.Empty;

    /// <summary>
    /// User who created this saved mission
    /// </summary>
    [Required]
    [MaxLength(100)]
    public string CreatedBy { get; set; } = string.Empty;

    /// <summary>
    /// When this saved mission was created
    /// </summary>
    [Required]
    public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// When this saved mission was last updated
    /// </summary>
    [Required]
    public DateTime UpdatedUtc { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Soft delete flag
    /// </summary>
    [Required]
    public bool IsDeleted { get; set; } = false;
}
