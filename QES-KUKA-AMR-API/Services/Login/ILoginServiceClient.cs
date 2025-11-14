using QES_KUKA_AMR_API.Models;
using QES_KUKA_AMR_API.Models.Login;

namespace QES_KUKA_AMR_API.Services.Login;

public interface ILoginServiceClient
{
    Task<SimulatorResponse<ApiResponse<LoginResponseData>>> LoginAsync(
        LoginRequest request,
        CancellationToken cancellationToken);
}
