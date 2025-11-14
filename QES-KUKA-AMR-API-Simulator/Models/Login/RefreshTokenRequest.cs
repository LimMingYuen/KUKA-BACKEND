namespace QES_KUKA_AMR_API_Simulator.Models.Login;

/// <summary>
/// Request model for refreshing an access token
/// </summary>
public class RefreshTokenRequest
{
    /// <summary>
    /// The refresh token obtained from the login response
    /// </summary>
    public string RefreshToken { get; set; } = string.Empty;
}
