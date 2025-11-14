using System.Net;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Options;
using QES_KUKA_AMR_API.Models;
using QES_KUKA_AMR_API.Models.Login;
using QES_KUKA_AMR_API.Options;

namespace QES_KUKA_AMR_API.Services.Login;

public class LoginServiceClient : ILoginServiceClient
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IOptions<LoginServiceOptions> _options;

    public LoginServiceClient(
        IHttpClientFactory httpClientFactory,
        IOptions<LoginServiceOptions> options)
    {
        _httpClientFactory = httpClientFactory;
        _options = options;
    }

    public async Task<SimulatorResponse<ApiResponse<LoginResponseData>>> LoginAsync(
        LoginRequest request,
        CancellationToken cancellationToken)
    {
        var options = _options.Value;
        if (string.IsNullOrWhiteSpace(options.LoginUrl) ||
            !Uri.TryCreate(options.LoginUrl, UriKind.Absolute, out var requestUri))
        {
            return new SimulatorResponse<ApiResponse<LoginResponseData>>(
                HttpStatusCode.InternalServerError,
                new ApiResponse<LoginResponseData>
                {
                    Success = false,
                    Code = "AUTH_CONFIGURATION_ERROR",
                    Msg = "Login service URL is not configured correctly."
                });
        }

        var httpClient = _httpClientFactory.CreateClient();

        var loginRequest = options.HashPassword
            ? new LoginRequest
            {
                Username = request.Username,
                Password = ComputeMd5Hash(request.Password)
            }
            : request;

        try
        {
            using var response = await httpClient.PostAsJsonAsync(
                requestUri,
                loginRequest,
                cancellationToken);

            var body = await response.Content.ReadFromJsonAsync<ApiResponse<LoginResponseData>>(
                cancellationToken: cancellationToken);

            return new SimulatorResponse<ApiResponse<LoginResponseData>>(
                response.StatusCode,
                body);
        }
        catch (HttpRequestException)
        {
            return new SimulatorResponse<ApiResponse<LoginResponseData>>(
                HttpStatusCode.BadGateway,
                new ApiResponse<LoginResponseData>
                {
                    Success = false,
                    Code = "AUTH_UPSTREAM_ERROR",
                    Msg = "Unable to reach the login service."
                });
        }
    }

    private static string ComputeMd5Hash(string input)
    {
        using var md5 = MD5.Create();
        var inputBytes = Encoding.UTF8.GetBytes(input);
        var hashBytes = md5.ComputeHash(inputBytes);

        return Convert.ToHexString(hashBytes).ToLowerInvariant();
    }
}
