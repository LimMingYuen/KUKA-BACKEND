using System.ComponentModel.DataAnnotations;

namespace QES_KUKA_AMR_API.Data.Entities;

/// <summary>
/// Queue status enumeration for mission queue items
/// </summary>
public enum MissionQueueStatus
{
    /// <summary>Mission is waiting in queue for robot assignment</summary>
    Queued = 0,

    /// <summary>Mission is being processed (checking robot availability)</summary>
    Processing = 1,

    /// <summary>Mission has been assigned to a robot and submitted to AMR</summary>
    Assigned = 2,

    /// <summary>Mission completed successfully</summary>
    Completed = 3,

    /// <summary>Mission failed during submission or execution</summary>
    Failed = 4,

    /// <summary>Mission was cancelled by user</summary>
    Cancelled = 5
}

/// <summary>
/// Represents a mission waiting in queue for robot assignment
/// </summary>
public class MissionQueue
{
    [Key]
    public int Id { get; set; }

    /// <summary>
    /// Unique mission code (generated when queued)
    /// </summary>
    [Required]
    [MaxLength(100)]
    public string MissionCode { get; set; } = string.Empty;

    /// <summary>
    /// Unique request ID (generated when queued)
    /// </summary>
    [Required]
    [MaxLength(100)]
    public string RequestId { get; set; } = string.Empty;

    /// <summary>
    /// Reference to SavedCustomMission template (if triggered from template)
    /// </summary>
    public int? SavedMissionId { get; set; }

    /// <summary>
    /// Display name for the mission
    /// </summary>
    [Required]
    [MaxLength(200)]
    public string MissionName { get; set; } = string.Empty;

    /// <summary>
    /// Full mission request payload as JSON (for resubmission)
    /// </summary>
    [Required]
    public string MissionRequestJson { get; set; } = string.Empty;

    /// <summary>
    /// Current status of the queue item
    /// </summary>
    [Required]
    public MissionQueueStatus Status { get; set; } = MissionQueueStatus.Queued;

    /// <summary>
    /// Priority for ordering in queue (lower = higher priority, 1 = highest)
    /// </summary>
    public int Priority { get; set; } = 1;

    /// <summary>
    /// Position in queue (calculated based on CreatedUtc and Priority)
    /// </summary>
    public int QueuePosition { get; set; } = 0;

    /// <summary>
    /// Robot ID assigned to this mission (when status changes to Assigned)
    /// </summary>
    [MaxLength(50)]
    public string? AssignedRobotId { get; set; }

    /// <summary>
    /// When the mission was added to queue
    /// </summary>
    public DateTime CreatedUtc { get; set; }

    /// <summary>
    /// When the mission started processing (robot selection)
    /// </summary>
    public DateTime? ProcessingStartedUtc { get; set; }

    /// <summary>
    /// When the mission was assigned to a robot
    /// </summary>
    public DateTime? AssignedUtc { get; set; }

    /// <summary>
    /// When the mission completed (success, failed, or cancelled)
    /// </summary>
    public DateTime? CompletedUtc { get; set; }

    /// <summary>
    /// User who queued this mission
    /// </summary>
    [MaxLength(100)]
    public string? CreatedBy { get; set; }

    /// <summary>
    /// Number of retry attempts for failed submissions
    /// </summary>
    public int RetryCount { get; set; } = 0;

    /// <summary>
    /// Maximum retry attempts allowed
    /// </summary>
    public int MaxRetries { get; set; } = 3;

    /// <summary>
    /// Error message if submission failed
    /// </summary>
    [MaxLength(500)]
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Robot type filter for selection (e.g., "LIFT", "FORK")
    /// </summary>
    [MaxLength(50)]
    public string? RobotTypeFilter { get; set; }

    /// <summary>
    /// Specific robot IDs to consider (comma-separated, if specified)
    /// </summary>
    [MaxLength(500)]
    public string? PreferredRobotIds { get; set; }

    /// <summary>
    /// Robot ID that has reserved this mission (for job optimization).
    /// When set, this mission will be assigned to this robot when it becomes available.
    /// </summary>
    [MaxLength(50)]
    public string? ReservedForRobotId { get; set; }

    /// <summary>
    /// When this reservation was made
    /// </summary>
    public DateTime? ReservedUtc { get; set; }

    /// <summary>
    /// Mission code of the JOBOPTIMIZATION mission that triggered this reservation.
    /// Used to check job status and determine if reservation should be cleared.
    /// </summary>
    [MaxLength(100)]
    public string? ReservedByMissionCode { get; set; }
}
