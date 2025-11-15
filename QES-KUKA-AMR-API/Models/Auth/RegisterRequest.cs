using System.ComponentModel.DataAnnotations;

namespace QES_KUKA_AMR_API.Models.Auth;

public class RegisterRequest
{
    [Required]
    [MaxLength(100)]
    public string Username { get; set; } = string.Empty;

    [Required]
    [MinLength(6)]
    public string Password { get; set; } = string.Empty;

    [MaxLength(100)]
    public string? Nickname { get; set; }

    public List<string>? Roles { get; set; }
}
