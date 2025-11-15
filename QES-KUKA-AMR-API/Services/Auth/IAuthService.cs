using QES_KUKA_AMR_API.Models.Auth;

namespace QES_KUKA_AMR_API.Services.Auth;

public interface IAuthService
{
    Task<InternalLoginResponse> LoginAsync(InternalLoginRequest request, CancellationToken cancellationToken = default);
    Task<InternalLoginResponse> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken = default);
    Task<UserInfo?> ValidateTokenAsync(string token, CancellationToken cancellationToken = default);
}
