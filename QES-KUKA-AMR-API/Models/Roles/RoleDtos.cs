using System.ComponentModel.DataAnnotations;

namespace QES_KUKA_AMR_API.Models.Roles;

public class RoleDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string RoleCode { get; set; } = string.Empty;
    public bool IsProtected { get; set; }
    public DateTime CreatedUtc { get; set; }
    public DateTime UpdatedUtc { get; set; }
}

public class RoleCreateRequest
{
    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    [Required]
    [MaxLength(100)]
    public string RoleCode { get; set; } = string.Empty;

    public bool IsProtected { get; set; } = false;
}

public class RoleUpdateRequest
{
    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    [Required]
    [MaxLength(100)]
    public string RoleCode { get; set; } = string.Empty;

    public bool IsProtected { get; set; }
}
