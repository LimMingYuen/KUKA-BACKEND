using QES_KUKA_AMR_API.Models.Auth;

namespace QES_KUKA_AMR_API.Services.Auth;

public interface IAuthService
{
    Task<InternalLoginResponse> LoginAsync(InternalLoginRequest request, CancellationToken cancellationToken = default);
    Task<InternalLoginResponse> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken = default);
    Task<UserInfo?> ValidateTokenAsync(string token, CancellationToken cancellationToken = default);
    Task<VerifyAdminResponse> VerifyAdminAsync(VerifyAdminRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Refresh the access token using a valid refresh token.
    /// Implements token rotation - old refresh token is revoked and new one is issued.
    /// </summary>
    Task<InternalLoginResponse> RefreshTokenAsync(string refreshToken, CancellationToken cancellationToken = default);

    /// <summary>
    /// Revoke a refresh token (used during logout).
    /// </summary>
    Task RevokeTokenAsync(string refreshToken, string reason = "Logout", CancellationToken cancellationToken = default);

    /// <summary>
    /// Revoke all refresh tokens for a user (used for security purposes).
    /// </summary>
    Task RevokeAllUserTokensAsync(int userId, string reason = "Security", CancellationToken cancellationToken = default);
}
