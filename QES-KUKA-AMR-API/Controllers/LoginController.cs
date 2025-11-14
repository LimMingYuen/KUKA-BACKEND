using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using QES_KUKA_AMR_API.Models;
using QES_KUKA_AMR_API.Models.Login;
using QES_KUKA_AMR_API.Services.Login;

namespace QES_KUKA_AMR_API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class LoginController : ControllerBase
{
    private readonly ILoginServiceClient _loginServiceClient;
    private readonly ILogger<LoginController> _logger;

    public LoginController(
        ILoginServiceClient loginServiceClient,
        ILogger<LoginController> logger)
    {
        _loginServiceClient = loginServiceClient;
        _logger = logger;
    }

    [HttpPost]
    public async Task<ActionResult<ApiResponse<LoginResponseData>>> LoginAsync(
        [FromBody] LoginRequest request,
        CancellationToken cancellationToken)
    {
        var serviceResponse = await _loginServiceClient.LoginAsync(request, cancellationToken);

        if (serviceResponse.Body is null)
        {
            _logger.LogError(
                "Login service returned no content. Status Code: {StatusCode}",
                serviceResponse.StatusCode);

            return StatusCode(StatusCodes.Status502BadGateway, new ApiResponse<LoginResponseData>
            {
                Success = false,
                Code = "AUTH_EMPTY_RESPONSE",
                Msg = "Failed to retrieve a response from the login service."
            });
        }

        return StatusCode((int)serviceResponse.StatusCode, serviceResponse.Body);
    }
}
