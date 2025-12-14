using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Options;
using QES_KUKA_AMR_API.Models;
using QES_KUKA_AMR_API.Models.Login;
using QES_KUKA_AMR_API.Options;

namespace QES_KUKA_AMR_API.Services.Auth;

public class ExternalApiTokenService : IExternalApiTokenService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly LoginServiceOptions _loginOptions;
    private readonly ILogger<ExternalApiTokenService> _logger;
    private readonly TimeProvider _timeProvider;
    private readonly SemaphoreSlim _semaphore = new(1, 1);

    // Default credentials for external API access
    private const string ExternalApiUsername = "admin";
    private const string ExternalApiPassword = "Admin";

    // Token cache with 1 hour validity (55 minutes to be safe with buffer)
    private const int TokenValidityMinutes = 55;
    private string? _cachedToken;
    private DateTime? _tokenExpiresAt;

    public ExternalApiTokenService(
        IHttpClientFactory httpClientFactory,
        IOptions<LoginServiceOptions> loginOptions,
        ILogger<ExternalApiTokenService> logger,
        TimeProvider timeProvider)
    {
        _httpClientFactory = httpClientFactory;
        _loginOptions = loginOptions.Value;
        _logger = logger;
        _timeProvider = timeProvider;
    }

    public async Task<string> GetTokenAsync(CancellationToken cancellationToken = default)
    {
        var now = _timeProvider.GetUtcNow().UtcDateTime;

        // Check if we have a valid cached token
        if (_cachedToken != null && _tokenExpiresAt != null && _tokenExpiresAt > now)
        {
            var tokenPreview = _cachedToken.Length > 20
                ? $"{_cachedToken.Substring(0, 10)}...{_cachedToken.Substring(_cachedToken.Length - 10)}"
                : _cachedToken;
            _logger.LogInformation("Using cached external API token (expires at {ExpiresAt}, preview: {TokenPreview})",
                _tokenExpiresAt, tokenPreview);
            return _cachedToken;
        }

        _logger.LogInformation("No valid cached token. CachedToken={HasToken}, ExpiresAt={ExpiresAt}, Now={Now}",
            _cachedToken != null, _tokenExpiresAt, now);

        // Token expired or doesn't exist, refresh it
        await RefreshTokenAsync(cancellationToken);

        if (_cachedToken == null)
        {
            throw new InvalidOperationException("Failed to obtain external API token");
        }

        return _cachedToken;
    }

    public async Task RefreshTokenAsync(CancellationToken cancellationToken = default)
    {
        await _semaphore.WaitAsync(cancellationToken);

        try
        {
            // Double-check if another thread already refreshed the token
            var now = _timeProvider.GetUtcNow().UtcDateTime;
            if (_cachedToken != null && _tokenExpiresAt != null && _tokenExpiresAt > now)
            {
                return;
            }

            if (string.IsNullOrWhiteSpace(_loginOptions.LoginUrl))
            {
                throw new InvalidOperationException("Login URL is not configured. Check LoginService:LoginUrl in appsettings.json");
            }

            _logger.LogInformation("Refreshing external API token from {LoginUrl} with credentials: {Username}",
                _loginOptions.LoginUrl, ExternalApiUsername);

            var password = _loginOptions.HashPassword
                ? ComputeMd5Hash(ExternalApiPassword)
                : ExternalApiPassword;

            var loginRequest = new LoginRequest
            {
                Username = ExternalApiUsername,
                Password = password
            };

            var httpClient = _httpClientFactory.CreateClient();
            var response = await httpClient.PostAsJsonAsync(_loginOptions.LoginUrl, loginRequest, cancellationToken);

            var responseBody = await response.Content.ReadFromJsonAsync<ApiResponse<LoginResponseData>>(cancellationToken);

            if (responseBody?.Success == true && responseBody.Data?.Token != null)
            {
                // Strip "Bearer " prefix if present (simulator returns token with "Bearer " prefix)
                var token = responseBody.Data.Token;
                if (token.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
                {
                    token = token.Substring(7);
                }

                _cachedToken = token;
                _tokenExpiresAt = _timeProvider.GetUtcNow().AddMinutes(TokenValidityMinutes).UtcDateTime;

                _logger.LogInformation(
                    "External API token refreshed successfully (expires at {ExpiresAt})",
                    _tokenExpiresAt);
            }
            else
            {
                var errorMsg = responseBody?.Msg ?? "Unknown error";
                _logger.LogError(
                    "Failed to obtain external API token. Status: {StatusCode}, Message: {Message}",
                    response.StatusCode,
                    errorMsg);

                throw new InvalidOperationException($"Failed to obtain external API token: {errorMsg}");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error refreshing external API token");
            throw;
        }
        finally
        {
            _semaphore.Release();
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
