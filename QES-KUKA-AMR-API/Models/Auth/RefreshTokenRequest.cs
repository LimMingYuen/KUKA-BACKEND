using System.ComponentModel.DataAnnotations;

namespace QES_KUKA_AMR_API.Models.Auth;

public class RefreshTokenRequest
{
    [Required]
    public string RefreshToken { get; set; } = string.Empty;
}
