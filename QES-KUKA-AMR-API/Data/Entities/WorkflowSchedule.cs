using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace QES_KUKA_AMR_API.Data.Entities;

[Table("WorkflowSchedules")]
public class WorkflowSchedule
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    [Required]
    public int WorkflowId { get; set; }

    [ForeignKey(nameof(WorkflowId))]
    public WorkflowDiagram? Workflow { get; set; }

    [Required]
    public WorkflowTriggerType TriggerType { get; set; }

    /// <summary>
    /// Cron expression for recurring schedules. Null when TriggerType = Once.
    /// </summary>
    [MaxLength(120)]
    public string? CronExpression { get; set; }

    /// <summary>
    /// UTC timestamp for the one-time run when TriggerType = Once.
    /// </summary>
    public DateTime? OneTimeRunUtc { get; set; }

    /// <summary>
    /// IANA or Windows timezone identifier.
    /// </summary>
    [MaxLength(100)]
    public string TimezoneId { get; set; } = "UTC";

    [Required]
    public bool IsEnabled { get; set; } = true;

    public DateTime CreatedUtc { get; set; }

    public DateTime UpdatedUtc { get; set; }

    public DateTime? LastRunUtc { get; set; }

    [MaxLength(30)]
    public string? LastStatus { get; set; }

    [MaxLength(500)]
    public string? LastError { get; set; }

    public DateTime? NextRunUtc { get; set; }

    [MaxLength(80)]
    public string? QueueLockToken { get; set; }

    public ICollection<WorkflowScheduleLog> Logs { get; set; } = new List<WorkflowScheduleLog>();
}

public enum WorkflowTriggerType
{
    Once = 0,
    Recurring = 1
}
