using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace QES_KUKA_AMR_API.Data.Entities;

/// <summary>
/// Represents a schedule for automatic triggering of a SavedCustomMission workflow template.
/// Supports one-time, interval-based, and cron expression scheduling.
/// </summary>
[Table("WorkflowSchedules")]
public class WorkflowSchedule
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    /// <summary>
    /// Reference to the SavedCustomMission template to trigger
    /// </summary>
    [Required]
    public int SavedMissionId { get; set; }

    /// <summary>
    /// Navigation property to the SavedCustomMission
    /// </summary>
    [ForeignKey(nameof(SavedMissionId))]
    public SavedCustomMission SavedMission { get; set; } = null!;

    /// <summary>
    /// User-friendly name for this schedule (e.g., "Morning Sync", "Hourly Check")
    /// </summary>
    [Required]
    [MaxLength(200)]
    public string ScheduleName { get; set; } = string.Empty;

    /// <summary>
    /// Optional description of what this schedule does
    /// </summary>
    [MaxLength(500)]
    public string? Description { get; set; }

    /// <summary>
    /// Type of schedule: "OneTime", "Interval", or "Cron"
    /// </summary>
    [Required]
    [MaxLength(20)]
    public string ScheduleType { get; set; } = "OneTime";

    /// <summary>
    /// For OneTime schedules: the specific UTC datetime to run
    /// </summary>
    public DateTime? OneTimeUtc { get; set; }

    /// <summary>
    /// For Interval schedules: repeat every X minutes (1 to 43200 = 30 days)
    /// </summary>
    public int? IntervalMinutes { get; set; }

    /// <summary>
    /// For Cron schedules: cron expression (e.g., "0 9 * * MON-FRI" for weekdays at 9 AM)
    /// </summary>
    [MaxLength(100)]
    public string? CronExpression { get; set; }

    /// <summary>
    /// Whether this schedule is currently active
    /// </summary>
    [Required]
    public bool IsEnabled { get; set; } = true;

    /// <summary>
    /// The next scheduled run time (null if schedule completed or disabled)
    /// </summary>
    public DateTime? NextRunUtc { get; set; }

    /// <summary>
    /// The last time this schedule was executed
    /// </summary>
    public DateTime? LastRunUtc { get; set; }

    /// <summary>
    /// Status of the last execution: "Success", "Failed", or null if never run
    /// </summary>
    [MaxLength(20)]
    public string? LastRunStatus { get; set; }

    /// <summary>
    /// Error message from the last failed execution
    /// </summary>
    [MaxLength(500)]
    public string? LastErrorMessage { get; set; }

    /// <summary>
    /// Total number of times this schedule has been executed
    /// </summary>
    [Required]
    public int ExecutionCount { get; set; } = 0;

    /// <summary>
    /// Maximum number of executions (null = unlimited)
    /// Schedule will be disabled after reaching this count
    /// </summary>
    public int? MaxExecutions { get; set; }

    /// <summary>
    /// When enabled, skip triggering if any instance of the same SavedMission
    /// is already running (Queued, Processing, or Assigned status).
    /// </summary>
    public bool SkipIfRunning { get; set; } = false;

    /// <summary>
    /// User who created this schedule
    /// </summary>
    [Required]
    [MaxLength(100)]
    public string CreatedBy { get; set; } = string.Empty;

    /// <summary>
    /// When this schedule was created
    /// </summary>
    [Required]
    public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// When this schedule was last updated
    /// </summary>
    public DateTime? UpdatedUtc { get; set; }
}
