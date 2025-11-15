using Microsoft.Extensions.DependencyInjection;
using QES_KUKA_AMR_API.Models.Login;
using QES_KUKA_AMR_API.Services.Login;

namespace QES_KUKA_AMR_API.Services.Auth;

public class ExternalApiTokenService : IExternalApiTokenService
{
    private readonly IServiceScopeFactory _serviceScopeFactory;
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
        IServiceScopeFactory serviceScopeFactory,
        ILogger<ExternalApiTokenService> logger,
        TimeProvider timeProvider)
    {
        _serviceScopeFactory = serviceScopeFactory;
        _logger = logger;
        _timeProvider = timeProvider;
    }

    public async Task<string> GetTokenAsync(CancellationToken cancellationToken = default)
    {
        var now = _timeProvider.GetUtcNow().UtcDateTime;

        // Check if we have a valid cached token
        if (_cachedToken != null && _tokenExpiresAt != null && _tokenExpiresAt > now)
        {
            _logger.LogDebug("Using cached external API token (expires at {ExpiresAt})", _tokenExpiresAt);
            return _cachedToken;
        }

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

            _logger.LogInformation("Refreshing external API token with credentials: {Username}", ExternalApiUsername);

            var loginRequest = new LoginRequest
            {
                Username = ExternalApiUsername,
                Password = ExternalApiPassword
            };

            // Create a scope to resolve scoped ILoginServiceClient
            using var scope = _serviceScopeFactory.CreateScope();
            var loginServiceClient = scope.ServiceProvider.GetRequiredService<ILoginServiceClient>();

            var response = await loginServiceClient.LoginAsync(loginRequest, cancellationToken);

            if (response.Body?.Success == true && response.Body.Data?.Token != null)
            {
                // Strip "Bearer " prefix if present (simulator returns token with "Bearer " prefix)
                var token = response.Body.Data.Token;
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
                var errorMsg = response.Body?.Msg ?? "Unknown error";
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
}
