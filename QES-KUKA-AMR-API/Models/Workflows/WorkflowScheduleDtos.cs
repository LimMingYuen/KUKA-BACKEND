using System.ComponentModel.DataAnnotations;
using QES_KUKA_AMR_API.Data.Entities;

namespace QES_KUKA_AMR_API.Models.Workflows;

public class WorkflowScheduleDto
{
    public int Id { get; set; }
    public int WorkflowId { get; set; }
    public WorkflowTriggerType TriggerType { get; set; }
    public string? CronExpression { get; set; }
    public DateTime? OneTimeRunUtc { get; set; }
    public string TimezoneId { get; set; } = "UTC";
    public bool IsEnabled { get; set; }
    public DateTime CreatedUtc { get; set; }
    public DateTime UpdatedUtc { get; set; }
    public DateTime? LastRunUtc { get; set; }
    public string? LastStatus { get; set; }
    public string? LastError { get; set; }
    public DateTime? NextRunUtc { get; set; }
}

public class WorkflowScheduleRequest
{
    [Required]
    public WorkflowTriggerType TriggerType { get; set; }

    [MaxLength(120)]
    public string? CronExpression { get; set; }

    public DateTime? RunAtLocalTime { get; set; }

    [Required]
    [MaxLength(100)]
    public string TimezoneId { get; set; } = "UTC";

    public bool IsEnabled { get; set; } = true;
}

public class WorkflowScheduleLogDto
{
    public int Id { get; set; }
    public int ScheduleId { get; set; }
    public DateTime ScheduledForUtc { get; set; }
    public DateTime? EnqueuedUtc { get; set; }
    public int? QueueId { get; set; }
    public string ResultStatus { get; set; } = "Pending";
    public string? Error { get; set; }
    public DateTime CreatedUtc { get; set; }
}

public class WorkflowTriggerResult
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public string MissionCode { get; set; } = string.Empty;
    public int? QueueId { get; set; }
    public bool ExecuteImmediately { get; set; }
}
