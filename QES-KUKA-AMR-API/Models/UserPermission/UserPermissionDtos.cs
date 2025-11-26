using System.ComponentModel.DataAnnotations;

namespace QES_KUKA_AMR_API.Models.UserPermission;

public class UserPermissionDto
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public string Username { get; set; } = string.Empty;
    public int PageId { get; set; }
    public string PagePath { get; set; } = string.Empty;
    public string PageName { get; set; } = string.Empty;
    public bool CanAccess { get; set; }
    public DateTime CreatedUtc { get; set; }
}

public class UserPermissionCreateRequest
{
    [Required]
    public int UserId { get; set; }

    [Required]
    public int PageId { get; set; }

    [Required]
    public bool CanAccess { get; set; } = true;
}

public class UserPermissionUpdateRequest
{
    [Required]
    public bool CanAccess { get; set; }
}

public class UserPermissionBulkSetRequest
{
    [Required]
    public int UserId { get; set; }

    [Required]
    public List<UserPagePermissionSet> PagePermissions { get; set; } = new();
}

public class UserPagePermissionSet
{
    [Required]
    public int PageId { get; set; }

    [Required]
    public bool CanAccess { get; set; }
}
