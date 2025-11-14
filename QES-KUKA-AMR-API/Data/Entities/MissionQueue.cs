using System.ComponentModel.DataAnnotations;

namespace QES_KUKA_AMR_API.Data.Entities;

public class MissionQueue
{
    [Key]
    public int Id { get; set; }

    /// <summary>
    /// Workflow ID - required for template-based missions, null for custom missions
    /// </summary>
    public int? WorkflowId { get; set; }

    /// <summary>
    /// Workflow code - required for template-based missions, null for custom missions
    /// </summary>
    [MaxLength(50)]
    public string? WorkflowCode { get; set; }

    /// <summary>
    /// Workflow name - required for template-based missions, null for custom missions
    /// </summary>
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
    [MaxLength(100)]
    public string MissionCode { get; set; } = string.Empty;

    /// <summary>
    /// Template code - required for template-based missions, null/empty for custom missions with missionData
    /// </summary>
    [MaxLength(100)]
    public string? TemplateCode { get; set; }

    [Required]
    public int Priority { get; set; } = 5;

    [Required]
    [MaxLength(100)]
    public string RequestId { get; set; } = string.Empty;

    [Required]
    public QueueStatus Status { get; set; } = QueueStatus.Queued;

    [Required]
    public DateTime CreatedDate { get; set; }

    public DateTime? ProcessedDate { get; set; }

    public DateTime? CompletedDate { get; set; }

    [MaxLength(500)]
    public string? ErrorMessage { get; set; }

    [Required]
    [MaxLength(100)]
    public string CreatedBy { get; set; } = string.Empty;

    /// <summary>
    /// Indicates whether this mission has been submitted to the AMR system
    /// </summary>
    [Required]
    public bool SubmittedToAmr { get; set; } = false;

    /// <summary>
    /// Timestamp when mission was submitted to AMR system
    /// </summary>
    public DateTime? SubmittedToAmrDate { get; set; }

    /// <summary>
    /// Error message if AMR submission failed
    /// </summary>
    [MaxLength(500)]
    public string? AmrSubmissionError { get; set; }

    // Custom Mission Fields (for missionData approach)

    /// <summary>
    /// JSON array of robot models, e.g., ["KMP600I", "KMP1500"]
    /// </summary>
    [MaxLength(500)]
    public string? RobotModels { get; set; }

    /// <summary>
    /// JSON array of robot IDs, e.g., ["14", "15"]
    /// </summary>
    [MaxLength(500)]
    public string? RobotIds { get; set; }

    /// <summary>
    /// Robot type, e.g., "LIFT", "LATENT"
    /// </summary>
    [MaxLength(50)]
    public string? RobotType { get; set; }

    /// <summary>
    /// Mission type, e.g., "RACK_MOVE", "DELIVERY"
    /// </summary>
    [MaxLength(50)]
    public string? MissionType { get; set; }

    /// <summary>
    /// View board type for mission display
    /// </summary>
    [MaxLength(50)]
    public string? ViewBoardType { get; set; }

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
    /// Whether to lock robot after mission finishes
    /// </summary>
    public bool LockRobotAfterFinish { get; set; }

    /// <summary>
    /// Robot ID to unlock
    /// </summary>
    [MaxLength(50)]
    public string? UnlockRobotId { get; set; }

    /// <summary>
    /// Mission code to unlock
    /// </summary>
    [MaxLength(100)]
    public string? UnlockMissionCode { get; set; }

    /// <summary>
    /// Idle node/position where robot should go after completion
    /// </summary>
    [MaxLength(100)]
    public string? IdleNode { get; set; }

    /// <summary>
    /// JSON array of mission data items (sequence, position, type, putDown, passStrategy, waitingMillis)
    /// Used when mission is created with inline steps instead of templateCode
    /// </summary>
    public string? MissionDataJson { get; set; }

    // Robot Tracking Fields (populated from robot-query API)

    /// <summary>
    /// Robot ID assigned to this mission (from job query response)
    /// </summary>
    [MaxLength(50)]
    public string? AssignedRobotId { get; set; }

    /// <summary>
    /// Current node/position of the robot (from robot-query API)
    /// </summary>
    [MaxLength(100)]
    public string? RobotNodeCode { get; set; }

    /// <summary>
    /// Robot status code from AMR system
    /// </summary>
    public int? RobotStatusCode { get; set; }

    /// <summary>
    /// Robot battery level percentage
    /// </summary>
    public int? RobotBatteryLevel { get; set; }

    /// <summary>
    /// Last time robot data was queried and updated
    /// </summary>
    public DateTime? LastRobotQueryTime { get; set; }

    // Manual Waypoint Handling Fields

    /// <summary>
    /// Indicates if mission is paused waiting for user to resume from a manual waypoint
    /// </summary>
    [Required]
    public bool IsWaitingForManualResume { get; set; } = false;

    /// <summary>
    /// Current manual waypoint position where robot is waiting (e.g., "M001-A001-40")
    /// </summary>
    [MaxLength(100)]
    public string? CurrentManualWaypointPosition { get; set; }

    /// <summary>
    /// JSON array of manual waypoint positions parsed from MissionDataJson (cached for performance)
    /// e.g., ["M001-A001-40", "M001-A001-50"]
    /// </summary>
    public string? ManualWaypointsJson { get; set; }

    /// <summary>
    /// JSON array of manual waypoint positions that have been visited and resumed during this mission
    /// Used to prevent re-pausing at the same waypoint (especially for areas with multiple nodes)
    /// e.g., ["Sim1-1-1756095423769", "M001-A001-50"]
    /// </summary>
    public string? VisitedManualWaypointsJson { get; set; }
}

public enum QueueStatus
{
    Queued = 0,
    Processing = 1,
    Completed = 2,
    Failed = 3,
    Cancelled = 4
}


public enum MissionTriggerSource
{
    /// <summary>
    /// Mission manually triggered by user through UI or direct API call
    /// </summary>
    Manual = 0,

    /// <summary>
    /// Mission triggered by a saved mission schedule
    /// </summary>
    Scheduled = 1,

    /// <summary>
    /// Mission triggered as part of a workflow execution
    /// </summary>
    Workflow = 2,

    /// <summary>
    /// Mission triggered by external system via API
    /// </summary>
    API = 3,

    /// <summary>
    /// Direct mission submission (not from saved template)
    /// </summary>
    Direct = 4
}
