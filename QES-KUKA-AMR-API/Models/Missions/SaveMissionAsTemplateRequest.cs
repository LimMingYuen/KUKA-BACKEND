using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace QES_KUKA_AMR_API.Models.Missions;

/// <summary>
/// Mission template data (excludes auto-generated requestId and missionCode)
/// </summary>
public class MissionTemplateData
{
    /// <summary>
    /// Organization ID
    /// </summary>
    [JsonPropertyName("orgId")]
    public string? OrgId { get; set; }

    /// <summary>
    /// Mission type (e.g., "RACK_MOVE", "DELIVERY")
    /// </summary>
    [Required(ErrorMessage = "Mission type is required")]
    [JsonPropertyName("missionType")]
    public string MissionType { get; set; } = string.Empty;

    /// <summary>
    /// View board type for mission display
    /// </summary>
    [JsonPropertyName("viewBoardType")]
    public string? ViewBoardType { get; set; }

    /// <summary>
    /// Robot models (e.g., ["KMP600I", "KMP1500"])
    /// </summary>
    [JsonPropertyName("robotModels")]
    public IReadOnlyList<string>? RobotModels { get; set; }

    /// <summary>
    /// Specific robot IDs (e.g., ["14", "15"])
    /// </summary>
    [JsonPropertyName("robotIds")]
    public IReadOnlyList<string>? RobotIds { get; set; }

    /// <summary>
    /// Robot type (e.g., "LIFT", "LATENT")
    /// </summary>
    [Required(ErrorMessage = "Robot type is required")]
    [JsonPropertyName("robotType")]
    public string RobotType { get; set; } = string.Empty;

    /// <summary>
    /// Mission priority (typically 1)
    /// </summary>
    [JsonPropertyName("priority")]
    public int Priority { get; set; } = 1;

    /// <summary>
    /// Container model code
    /// </summary>
    [JsonPropertyName("containerModelCode")]
    public string? ContainerModelCode { get; set; }

    /// <summary>
    /// Container code
    /// </summary>
    [JsonPropertyName("containerCode")]
    public string? ContainerCode { get; set; }

    /// <summary>
    /// Template code for workflow-based missions
    /// </summary>
    [JsonPropertyName("templateCode")]
    public string? TemplateCode { get; set; }

    /// <summary>
    /// Whether to lock robot after mission completion
    /// </summary>
    [JsonPropertyName("lockRobotAfterFinish")]
    public bool LockRobotAfterFinish { get; set; }

    /// <summary>
    /// Robot ID to unlock after completion
    /// </summary>
    [JsonPropertyName("unlockRobotId")]
    public string? UnlockRobotId { get; set; }

    /// <summary>
    /// Mission code that will unlock the robot
    /// </summary>
    [JsonPropertyName("unlockMissionCode")]
    public string? UnlockMissionCode { get; set; }

    /// <summary>
    /// Idle node/position where robot should go after completion
    /// </summary>
    [JsonPropertyName("idleNode")]
    public string? IdleNode { get; set; }

    /// <summary>
    /// Mission steps/waypoints
    /// </summary>
    [Required(ErrorMessage = "At least one mission step is required")]
    [JsonPropertyName("missionData")]
    public IReadOnlyList<MissionDataItem>? MissionData { get; set; }
}

/// <summary>
/// Request to save a mission as a reusable template
/// NOTE: requestId and missionCode are auto-generated when triggering, so they are not included here
/// </summary>
public class SaveMissionAsTemplateRequest
{
    /// <summary>
    /// User-friendly name for the saved mission template
    /// </summary>
    [Required(ErrorMessage = "Mission name is required")]
    [MaxLength(200, ErrorMessage = "Mission name cannot exceed 200 characters")]
    [JsonPropertyName("missionName")]
    public string MissionName { get; set; } = string.Empty;

    /// <summary>
    /// Optional description of what this mission does
    /// </summary>
    [MaxLength(500, ErrorMessage = "Description cannot exceed 500 characters")]
    [JsonPropertyName("description")]
    public string? Description { get; set; }

    /// <summary>
    /// Concurrency mode for this template:
    /// - "Unlimited" (default): Can trigger multiple times, missions queue up
    /// - "Wait": Must wait for all active instances to complete before re-triggering
    /// </summary>
    [MaxLength(20)]
    [JsonPropertyName("concurrencyMode")]
    public string ConcurrencyMode { get; set; } = "Unlimited";

    /// <summary>
    /// The mission template data (excludes requestId and missionCode which are auto-generated on trigger)
    /// </summary>
    [Required(ErrorMessage = "Mission template data is required")]
    [JsonPropertyName("missionTemplate")]
    public MissionTemplateData MissionTemplate { get; set; } = new();
}

/// <summary>
/// Response after saving a mission as a template
/// </summary>
public class SaveMissionAsTemplateResponse
{
    [JsonPropertyName("success")]
    public bool Success { get; set; }

    [JsonPropertyName("message")]
    public string Message { get; set; } = string.Empty;

    [JsonPropertyName("savedMissionId")]
    public int? SavedMissionId { get; set; }

    [JsonPropertyName("missionName")]
    public string MissionName { get; set; } = string.Empty;
}
