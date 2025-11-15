using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace QES_KUKA_AMR_API.Data.Entities;

[Table("WorkflowDiagrams")]
public class WorkflowDiagram
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    /// <summary>
    /// The workflow ID from the external AMR system API
    /// </summary>
    public int? ExternalWorkflowId { get; set; }

    [Required]
    [MaxLength(64)]
    public string WorkflowCode { get; set; } = string.Empty;

    [Required]
    [MaxLength(64)]
    public string WorkflowOuterCode { get; set; } = string.Empty;

    [Required]
    [MaxLength(256)]
    public string WorkflowName { get; set; } = string.Empty;

    public int WorkflowModel { get; set; }

    public int RobotTypeClass { get; set; }

    [MaxLength(128)]
    public string MapCode { get; set; } = string.Empty;

    [MaxLength(128)]
    public string? ButtonName { get; set; }

    [MaxLength(128)]
    public string CreateUsername { get; set; } = string.Empty;

    public DateTime CreateTime { get; set; }

    [MaxLength(128)]
    public string UpdateUsername { get; set; } = string.Empty;

    public DateTime UpdateTime { get; set; }

    public int Status { get; set; }

    public int NeedConfirm { get; set; }

    public int LockRobotAfterFinish { get; set; }

    public int WorkflowPriority { get; set; }

    [MaxLength(64)]
    public string? TargetAreaCode { get; set; }

    [MaxLength(64)]
    public string? PreSelectedRobotCellCode { get; set; }

    public int? PreSelectedRobotId { get; set; }
}
