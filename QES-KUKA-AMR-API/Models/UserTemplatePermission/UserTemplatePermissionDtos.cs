using System.ComponentModel.DataAnnotations;

namespace QES_KUKA_AMR_API.Models.UserTemplatePermission;

public class UserTemplatePermissionDto
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public string Username { get; set; } = string.Empty;
    public int SavedCustomMissionId { get; set; }
    public string MissionName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool CanAccess { get; set; }
    public DateTime CreatedUtc { get; set; }
}

public class UserTemplatePermissionCreateRequest
{
    [Required]
    public int UserId { get; set; }

    [Required]
    public int SavedCustomMissionId { get; set; }

    [Required]
    public bool CanAccess { get; set; } = true;
}

public class UserTemplatePermissionUpdateRequest
{
    [Required]
    public bool CanAccess { get; set; }
}

public class UserTemplatePermissionBulkSetRequest
{
    [Required]
    public int UserId { get; set; }

    [Required]
    public List<UserTemplatePermissionSet> TemplatePermissions { get; set; } = new();
}

public class UserTemplatePermissionSet
{
    [Required]
    public int SavedCustomMissionId { get; set; }

    [Required]
    public bool CanAccess { get; set; }
}
