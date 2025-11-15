using System.ComponentModel.DataAnnotations;

namespace QES_KUKA_AMR_API.Models.Users;

public class UserDto
{
    public int Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public string? Nickname { get; set; }
    public bool IsSuperAdmin { get; set; }
    public List<string> Roles { get; set; } = new();
    public DateTime CreateTime { get; set; }
    public string? CreateBy { get; set; }
    public string? CreateApp { get; set; }
    public DateTime LastUpdateTime { get; set; }
    public string? LastUpdateBy { get; set; }
    public string? LastUpdateApp { get; set; }
}

public class UserCreateRequest
{
    [Required]
    [MaxLength(100)]
    public string Username { get; set; } = string.Empty;

    [Required]
    [MinLength(6)]
    public string Password { get; set; } = string.Empty;

    [MaxLength(100)]
    public string? Nickname { get; set; }

    public bool IsSuperAdmin { get; set; } = false;

    public List<string>? Roles { get; set; }

    [MaxLength(100)]
    public string? CreateBy { get; set; }

    [MaxLength(100)]
    public string? CreateApp { get; set; }
}

public class UserUpdateRequest
{
    [Required]
    [MaxLength(100)]
    public string Username { get; set; } = string.Empty;

    /// <summary>
    /// Optional password - only set this if you want to change the user's password
    /// </summary>
    [MinLength(6)]
    public string? Password { get; set; }

    [MaxLength(100)]
    public string? Nickname { get; set; }

    public bool IsSuperAdmin { get; set; }

    public List<string>? Roles { get; set; }

    [MaxLength(100)]
    public string? LastUpdateBy { get; set; }

    [MaxLength(100)]
    public string? LastUpdateApp { get; set; }
}
