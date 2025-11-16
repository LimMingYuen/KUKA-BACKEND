using System.ComponentModel.DataAnnotations;

namespace QES_KUKA_AMR_API.Models.SavedCustomMissions;

/// <summary>
/// DTO for saved custom mission response
/// </summary>
public class SavedCustomMissionDto
{
    public int Id { get; set; }
    public string MissionName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string MissionType { get; set; } = string.Empty;
    public string RobotType { get; set; } = string.Empty;
    public int Priority { get; set; }
    public string? RobotModels { get; set; }
    public string? RobotIds { get; set; }
    public string? ContainerModelCode { get; set; }
    public string? ContainerCode { get; set; }
    public string? IdleNode { get; set; }
    public string? OrgId { get; set; }
    public string? ViewBoardType { get; set; }
    public string? TemplateCode { get; set; }
    public bool LockRobotAfterFinish { get; set; }
    public string? UnlockRobotId { get; set; }
    public string? UnlockMissionCode { get; set; }
    public string MissionStepsJson { get; set; } = string.Empty;
    public string CreatedBy { get; set; } = string.Empty;
    public DateTime CreatedUtc { get; set; }
    public DateTime UpdatedUtc { get; set; }
    public SavedMissionScheduleSummaryDto ScheduleSummary { get; set; } = new();
}

/// <summary>
/// Request model for creating a saved custom mission
/// </summary>
public class SavedCustomMissionCreateRequest
{
    [Required(ErrorMessage = "Mission name is required")]
    [MaxLength(200, ErrorMessage = "Mission name cannot exceed 200 characters")]
    public string MissionName { get; set; } = string.Empty;

    [MaxLength(500, ErrorMessage = "Description cannot exceed 500 characters")]
    public string? Description { get; set; }

    [Required(ErrorMessage = "Mission type is required")]
    [MaxLength(50)]
    public string MissionType { get; set; } = string.Empty;

    [Required(ErrorMessage = "Robot type is required")]
    [MaxLength(50)]
    public string RobotType { get; set; } = string.Empty;

    [Range(0, 10, ErrorMessage = "Priority must be between 0 and 10")]
    public int Priority { get; set; } = 1;

    [MaxLength(500)]
    public string? RobotModels { get; set; }

    [MaxLength(500)]
    public string? RobotIds { get; set; }

    [MaxLength(100)]
    public string? ContainerModelCode { get; set; }

    [MaxLength(100)]
    public string? ContainerCode { get; set; }

    [MaxLength(100)]
    public string? IdleNode { get; set; }

    [MaxLength(100)]
    public string? OrgId { get; set; }

    [MaxLength(100)]
    public string? ViewBoardType { get; set; }

    [MaxLength(100)]
    public string? TemplateCode { get; set; }

    public bool LockRobotAfterFinish { get; set; }

    [MaxLength(100)]
    public string? UnlockRobotId { get; set; }

    [MaxLength(100)]
    public string? UnlockMissionCode { get; set; }

    [Required(ErrorMessage = "At least one mission step is required")]
    public string MissionStepsJson { get; set; } = string.Empty;
}

/// <summary>
/// Request model for updating a saved custom mission
/// </summary>
public class SavedCustomMissionUpdateRequest
{
    [Required(ErrorMessage = "Mission name is required")]
    [MaxLength(200, ErrorMessage = "Mission name cannot exceed 200 characters")]
    public string MissionName { get; set; } = string.Empty;

    [MaxLength(500, ErrorMessage = "Description cannot exceed 500 characters")]
    public string? Description { get; set; }

    [Required(ErrorMessage = "Mission type is required")]
    [MaxLength(50)]
    public string MissionType { get; set; } = string.Empty;

    [Required(ErrorMessage = "Robot type is required")]
    [MaxLength(50)]
    public string RobotType { get; set; } = string.Empty;

    [Range(0, 10, ErrorMessage = "Priority must be between 0 and 10")]
    public int Priority { get; set; } = 1;

    [MaxLength(500)]
    public string? RobotModels { get; set; }

    [MaxLength(500)]
    public string? RobotIds { get; set; }

    [MaxLength(100)]
    public string? ContainerModelCode { get; set; }

    [MaxLength(100)]
    public string? ContainerCode { get; set; }

    [MaxLength(100)]
    public string? IdleNode { get; set; }

    [MaxLength(100)]
    public string? OrgId { get; set; }

    [MaxLength(100)]
    public string? ViewBoardType { get; set; }

    [MaxLength(100)]
    public string? TemplateCode { get; set; }

    public bool LockRobotAfterFinish { get; set; }

    [MaxLength(100)]
    public string? UnlockRobotId { get; set; }

    [MaxLength(100)]
    public string? UnlockMissionCode { get; set; }

    [Required(ErrorMessage = "At least one mission step is required")]
    public string MissionStepsJson { get; set; } = string.Empty;
}

/// <summary>
/// Response model for trigger operation
/// </summary>
public class TriggerMissionResponse
{
    public string MissionCode { get; set; } = string.Empty;
    public string RequestId { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
}

public class SavedMissionScheduleSummaryDto
{
    public int TotalSchedules { get; set; }
    public int ActiveSchedules { get; set; }
    public DateTime? NextRunUtc { get; set; }
    public string? LastStatus { get; set; }
    public DateTime? LastRunUtc { get; set; }
}
