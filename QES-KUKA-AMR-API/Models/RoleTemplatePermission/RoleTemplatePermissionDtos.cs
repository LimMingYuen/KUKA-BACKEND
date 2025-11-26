using System.ComponentModel.DataAnnotations;

namespace QES_KUKA_AMR_API.Models.RoleTemplatePermission;

public class RoleTemplatePermissionDto
{
    public int Id { get; set; }
    public int RoleId { get; set; }
    public string RoleName { get; set; } = string.Empty;
    public string RoleCode { get; set; } = string.Empty;
    public int SavedCustomMissionId { get; set; }
    public string MissionName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool CanAccess { get; set; }
    public DateTime CreatedUtc { get; set; }
}

public class RoleTemplatePermissionCreateRequest
{
    [Required]
    public int RoleId { get; set; }

    [Required]
    public int SavedCustomMissionId { get; set; }

    [Required]
    public bool CanAccess { get; set; } = true;
}

public class RoleTemplatePermissionUpdateRequest
{
    [Required]
    public bool CanAccess { get; set; }
}

public class RoleTemplatePermissionBulkSetRequest
{
    [Required]
    public int RoleId { get; set; }

    [Required]
    public List<RoleTemplatePermissionSet> TemplatePermissions { get; set; } = new();
}

public class RoleTemplatePermissionSet
{
    [Required]
    public int SavedCustomMissionId { get; set; }

    [Required]
    public bool CanAccess { get; set; }
}

public class RoleTemplatePermissionMatrix
{
    public List<TemplateRoleInfo> Roles { get; set; } = new();
    public List<TemplateInfo> Templates { get; set; } = new();
    public Dictionary<string, bool> Permissions { get; set; } = new(); // Key: "roleId_templateId"
}

public class TemplateRoleInfo
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string RoleCode { get; set; } = string.Empty;
}

public class TemplateInfo
{
    public int Id { get; set; }
    public string MissionName { get; set; } = string.Empty;
    public string? Description { get; set; }
}
