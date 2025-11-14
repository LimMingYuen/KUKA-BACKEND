using System.ComponentModel.DataAnnotations;

namespace QES_KUKA_AMR_API.Data.Entities;

public class MissionHistory
{
    [Key]
    public int Id { get; set; }

    [Required]
    [MaxLength(100)]
    public string MissionCode { get; set; } = string.Empty;

    [Required]
    [MaxLength(100)]
    public string RequestId { get; set; } = string.Empty;

    // Nullable for custom missions (which don't have workflows)
    public int? WorkflowId { get; set; }

    [MaxLength(200)]
    public string? WorkflowName { get; set; }

    /// <summary>
    /// SavedCustomMission ID - links this mission execution to a saved mission template
    /// </summary>
    public int? SavedMissionId { get; set; }

    /// <summary>
    /// How this mission was triggered/created
    /// </summary>
    [Required]
    public MissionTriggerSource TriggerSource { get; set; } = MissionTriggerSource.Manual;

    [Required]
    [MaxLength(50)]
    public string Status { get; set; } = string.Empty;

    [MaxLength(50)]
    public string? MissionType { get; set; }

    public DateTime CreatedDate { get; set; }

    public DateTime? ProcessedDate { get; set; }

    public DateTime? SubmittedToAmrDate { get; set; }

    public DateTime? CompletedDate { get; set; }

    /// <summary>
    /// Robot ID assigned to this mission (from job query response)
    /// </summary>
    [MaxLength(50)]
    public string? AssignedRobotId { get; set; }

    [MaxLength(500)]
    public string? ErrorMessage { get; set; }

    [MaxLength(100)]
    public string? CreatedBy { get; set; }
}
