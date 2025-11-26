using System.ComponentModel.DataAnnotations;

namespace QES_KUKA_AMR_API.Models.RolePermission;

public class RolePermissionDto
{
    public int Id { get; set; }
    public int RoleId { get; set; }
    public string RoleName { get; set; } = string.Empty;
    public string RoleCode { get; set; } = string.Empty;
    public int PageId { get; set; }
    public string PagePath { get; set; } = string.Empty;
    public string PageName { get; set; } = string.Empty;
    public bool CanAccess { get; set; }
    public DateTime CreatedUtc { get; set; }
}

public class RolePermissionCreateRequest
{
    [Required]
    public int RoleId { get; set; }

    [Required]
    public int PageId { get; set; }

    [Required]
    public bool CanAccess { get; set; } = true;
}

public class RolePermissionUpdateRequest
{
    [Required]
    public bool CanAccess { get; set; }
}

public class RolePermissionBulkSetRequest
{
    [Required]
    public int RoleId { get; set; }

    [Required]
    public List<RolePagePermissionSet> PagePermissions { get; set; } = new();
}

public class RolePagePermissionSet
{
    [Required]
    public int PageId { get; set; }

    [Required]
    public bool CanAccess { get; set; }
}

public class RolePermissionMatrix
{
    public List<RoleInfo> Roles { get; set; } = new();
    public List<PageInfo> Pages { get; set; } = new();
    public Dictionary<string, bool> Permissions { get; set; } = new(); // Key: "roleId_pageId"
}

public class RoleInfo
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string RoleCode { get; set; } = string.Empty;
}

public class PageInfo
{
    public int Id { get; set; }
    public string PagePath { get; set; } = string.Empty;
    public string PageName { get; set; } = string.Empty;
}
