namespace QES_KUKA_AMR_API_Simulator.Models.Login;

public class LoginResponseData
{
    /// <summary>
    /// JWT access token (short-lived, e.g., 1 hour)
    /// </summary>
    public string Token { get; set; } = string.Empty;

    /// <summary>
    /// Refresh token for obtaining new access tokens without re-login (long-lived, e.g., 7 days)
    /// </summary>
    public string RefreshToken { get; set; } = string.Empty;

    /// <summary>
    /// UTC timestamp when the access token expires
    /// </summary>
    public DateTime TokenExpiresUtc { get; set; }

    public LoginUserInfo? UserInfo { get; set; }
    public IReadOnlyList<LoginPermission> Permissions { get; set; } = Array.Empty<LoginPermission>();
}
