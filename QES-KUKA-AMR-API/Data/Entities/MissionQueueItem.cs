using System.ComponentModel.DataAnnotations;

namespace QES_KUKA_AMR_API.Data.Entities;

/// <summary>
/// Represents a pending mission in the MapCode-based queue system
/// </summary>
public class MissionQueueItem
{
    [Key]
    public int Id { get; set; }

    /// <summary>
    /// Unique queue item identifier (format: "queue{timestamp}{random}")
    /// </summary>
    [Required]
    [MaxLength(100)]
    public string QueueItemCode { get; set; } = string.Empty;

    /// <summary>
    /// Mission code generated when submitted to AMR
    /// </summary>
    [Required]
    [MaxLength(100)]
    public string MissionCode { get; set; } = string.Empty;

    /// <summary>
    /// Request ID for tracking
    /// </summary>
    [MaxLength(100)]
    public string? RequestId { get; set; }

    /// <summary>
    /// Mission priority (1-10, higher = more urgent)
    /// </summary>
    [Required]
    public int Priority { get; set; } = 5;

    /// <summary>
    /// Mission type (e.g., "RACK_MOVE", "DELIVERY")
    /// </summary>
    [MaxLength(50)]
    public string? MissionType { get; set; }

    /// <summary>
    /// Robot type (e.g., "LIFT", "LATENT")
    /// </summary>
    [MaxLength(50)]
    public string? RobotType { get; set; }

    /// <summary>
    /// Template/workflow code for workflow-based missions
    /// </summary>
    [MaxLength(100)]
    public string? TemplateCode { get; set; }

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
    /// Idle node position
    /// </summary>
    [MaxLength(100)]
    public string? IdleNode { get; set; }

    /// <summary>
    /// Organization ID
    /// </summary>
    [MaxLength(100)]
    public string? OrgId { get; set; }

    /// <summary>
    /// View board type
    /// </summary>
    [MaxLength(100)]
    public string? ViewBoardType { get; set; }

    /// <summary>
    /// Whether to lock robot after finish
    /// </summary>
    public bool LockRobotAfterFinish { get; set; }

    /// <summary>
    /// Robot ID to unlock
    /// </summary>
    [MaxLength(100)]
    public string? UnlockRobotId { get; set; }

    /// <summary>
    /// Mission code that unlocks robot
    /// </summary>
    [MaxLength(100)]
    public string? UnlockMissionCode { get; set; }

    /// <summary>
    /// Primary MapCode for this mission
    /// </summary>
    [Required]
    [MaxLength(128)]
    public string PrimaryMapCode { get; set; } = string.Empty;

    /// <summary>
    /// Secondary MapCode for cross-map missions
    /// </summary>
    [MaxLength(128)]
    public string? SecondaryMapCode { get; set; }

    /// <summary>
    /// Indicates if this mission spans multiple maps
    /// </summary>
    public bool IsCrossMapMission { get; set; }

    /// <summary>
    /// Foreign key to WorkflowDiagram (if triggered from workflow)
    /// </summary>
    public int? WorkflowId { get; set; }

    /// <summary>
    /// Foreign key to SavedCustomMission (if triggered from saved mission)
    /// </summary>
    public int? SavedMissionId { get; set; }

    /// <summary>
    /// How this mission was triggered
    /// </summary>
    [Required]
    public MissionTriggerSource TriggerSource { get; set; } = MissionTriggerSource.Manual;

    /// <summary>
    /// JSON array of MissionDataItem representing mission steps
    /// </summary>
    [Required]
    public string MissionStepsJson { get; set; } = "[]";

    /// <summary>
    /// Comma-separated list of compatible robot model codes
    /// </summary>
    [MaxLength(500)]
    public string? RobotModels { get; set; }

    /// <summary>
    /// Comma-separated list of specific robot IDs allowed for this mission
    /// </summary>
    [MaxLength(500)]
    public string? RobotIds { get; set; }

    /// <summary>
    /// Current queue status
    /// </summary>
    [Required]
    public MissionQueueStatus Status { get; set; } = MissionQueueStatus.Pending;

    /// <summary>
    /// When mission was added to queue
    /// </summary>
    [Required]
    public DateTime EnqueuedUtc { get; set; }

    /// <summary>
    /// When mission processing started
    /// </summary>
    public DateTime? StartedUtc { get; set; }

    /// <summary>
    /// When mission completed successfully
    /// </summary>
    public DateTime? CompletedUtc { get; set; }

    /// <summary>
    /// When mission was cancelled
    /// </summary>
    public DateTime? CancelledUtc { get; set; }

    /// <summary>
    /// Robot assigned to this mission
    /// </summary>
    [MaxLength(100)]
    public string? AssignedRobotId { get; set; }

    /// <summary>
    /// When robot was assigned
    /// </summary>
    public DateTime? RobotAssignedUtc { get; set; }

    /// <summary>
    /// Parent queue item ID for linked cross-map segments
    /// </summary>
    public int? ParentQueueItemId { get; set; }

    /// <summary>
    /// Next queue item ID for chained jobs
    /// </summary>
    public int? NextQueueItemId { get; set; }

    /// <summary>
    /// True if this job was selected during opportunistic job evaluation
    /// </summary>
    public bool IsOpportunisticJob { get; set; }

    /// <summary>
    /// First step position/node label (for distance calculations)
    /// </summary>
    [MaxLength(64)]
    public string? StartNodeLabel { get; set; }

    /// <summary>
    /// X coordinate of start node
    /// </summary>
    public double? StartXCoordinate { get; set; }

    /// <summary>
    /// Y coordinate of start node
    /// </summary>
    public double? StartYCoordinate { get; set; }

    /// <summary>
    /// Error message if mission failed
    /// </summary>
    [MaxLength(500)]
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Number of retry attempts
    /// </summary>
    public int RetryCount { get; set; }

    /// <summary>
    /// Last retry timestamp
    /// </summary>
    public DateTime? LastRetryUtc { get; set; }

    /// <summary>
    /// User who triggered this mission
    /// </summary>
    [MaxLength(100)]
    public string? CreatedBy { get; set; }

    /// <summary>
    /// When this queue item was created
    /// </summary>
    public DateTime CreatedUtc { get; set; }

    /// <summary>
    /// When this queue item was last updated
    /// </summary>
    public DateTime UpdatedUtc { get; set; }
}
