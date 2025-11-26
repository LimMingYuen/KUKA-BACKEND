using System.ComponentModel.DataAnnotations;

namespace QES_KUKA_AMR_API.Models.Schedule;

/// <summary>
/// Response DTO for workflow schedule
/// </summary>
public class WorkflowScheduleDto
{
    public int Id { get; set; }
    public int SavedMissionId { get; set; }
    public string SavedMissionName { get; set; } = string.Empty;
    public string ScheduleName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string ScheduleType { get; set; } = string.Empty;
    public DateTime? OneTimeUtc { get; set; }
    public int? IntervalMinutes { get; set; }
    public string? CronExpression { get; set; }
    public bool IsEnabled { get; set; }
    public DateTime? NextRunUtc { get; set; }
    public DateTime? LastRunUtc { get; set; }
    public string? LastRunStatus { get; set; }
    public string? LastErrorMessage { get; set; }
    public int ExecutionCount { get; set; }
    public int? MaxExecutions { get; set; }
    public string CreatedBy { get; set; } = string.Empty;
    public DateTime CreatedUtc { get; set; }
    public DateTime? UpdatedUtc { get; set; }
}

/// <summary>
/// Request DTO for creating a new workflow schedule
/// </summary>
public class CreateWorkflowScheduleRequest
{
    [Required]
    [MaxLength(200)]
    public string ScheduleName { get; set; } = string.Empty;

    [MaxLength(500)]
    public string? Description { get; set; }

    [Required]
    public int SavedMissionId { get; set; }

    /// <summary>
    /// Schedule type: "OneTime", "Interval", or "Cron"
    /// </summary>
    [Required]
    [RegularExpression("^(OneTime|Interval|Cron)$", ErrorMessage = "ScheduleType must be OneTime, Interval, or Cron")]
    public string ScheduleType { get; set; } = "OneTime";

    /// <summary>
    /// For OneTime schedules: the specific UTC datetime to run
    /// </summary>
    public DateTime? OneTimeUtc { get; set; }

    /// <summary>
    /// For Interval schedules: repeat every X minutes (1 to 43200 = 30 days)
    /// </summary>
    [Range(1, 43200, ErrorMessage = "IntervalMinutes must be between 1 and 43200 (30 days)")]
    public int? IntervalMinutes { get; set; }

    /// <summary>
    /// For Cron schedules: cron expression (e.g., "0 9 * * MON-FRI")
    /// </summary>
    [MaxLength(100)]
    public string? CronExpression { get; set; }

    /// <summary>
    /// Whether to enable this schedule immediately
    /// </summary>
    public bool IsEnabled { get; set; } = true;

    /// <summary>
    /// Maximum number of executions (null = unlimited)
    /// </summary>
    [Range(1, int.MaxValue, ErrorMessage = "MaxExecutions must be at least 1")]
    public int? MaxExecutions { get; set; }
}

/// <summary>
/// Request DTO for updating an existing workflow schedule
/// </summary>
public class UpdateWorkflowScheduleRequest
{
    [MaxLength(200)]
    public string? ScheduleName { get; set; }

    [MaxLength(500)]
    public string? Description { get; set; }

    [RegularExpression("^(OneTime|Interval|Cron)$", ErrorMessage = "ScheduleType must be OneTime, Interval, or Cron")]
    public string? ScheduleType { get; set; }

    public DateTime? OneTimeUtc { get; set; }

    [Range(1, 43200, ErrorMessage = "IntervalMinutes must be between 1 and 43200 (30 days)")]
    public int? IntervalMinutes { get; set; }

    [MaxLength(100)]
    public string? CronExpression { get; set; }

    public bool? IsEnabled { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "MaxExecutions must be at least 1")]
    public int? MaxExecutions { get; set; }
}

/// <summary>
/// Request DTO for toggling schedule enabled state
/// </summary>
public class ToggleScheduleRequest
{
    [Required]
    public bool IsEnabled { get; set; }
}

/// <summary>
/// Result of triggering a schedule manually
/// </summary>
public class ScheduleTriggerResult
{
    public bool Success { get; set; }
    public string? MissionCode { get; set; }
    public string? RequestId { get; set; }
    public string? ErrorMessage { get; set; }
}
