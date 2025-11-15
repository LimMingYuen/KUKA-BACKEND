using System.ComponentModel.DataAnnotations;

namespace QES_KUKA_AMR_API.Models.Auth;

public class InternalLoginRequest
{
    [Required]
    public string Username { get; set; } = string.Empty;

    [Required]
    public string Password { get; set; } = string.Empty;
}
