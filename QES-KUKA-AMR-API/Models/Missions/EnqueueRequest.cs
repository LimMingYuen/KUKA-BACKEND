using System.ComponentModel.DataAnnotations;

using QES_KUKA_AMR_API.Data.Entities;

namespace QES_KUKA_AMR_API.Models.Missions;

public class EnqueueRequest
{
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
    [MaxLength(100)]
    public string CreatedBy { get; set; } = string.Empty;

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
    public string? MissionData { get; set; }
}