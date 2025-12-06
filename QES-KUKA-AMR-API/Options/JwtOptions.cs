namespace QES_KUKA_AMR_API.Options;

public class JwtOptions
{
    public string SecretKey { get; set; } = string.Empty;
    public string Issuer { get; set; } = string.Empty;
    public string Audience { get; set; } = string.Empty;
    public int ExpirationHours { get; set; } = 24;

    /// <summary>
    /// Access token expiration in minutes (for shorter-lived access tokens when using refresh tokens)
    /// Default: 15 minutes
    /// </summary>
    public int AccessTokenExpirationMinutes { get; set; } = 15;

    /// <summary>
    /// Refresh token expiration in days
    /// Default: 7 days
    /// </summary>
    public int RefreshTokenExpirationDays { get; set; } = 7;
}
