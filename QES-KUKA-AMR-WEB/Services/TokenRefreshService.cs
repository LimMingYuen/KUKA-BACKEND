using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Http;

namespace QES_KUKA_AMR_WEB.Services
{
    /// <summary>
    /// Service for automatically refreshing JWT tokens before they expire
    /// </summary>
    public interface ITokenRefreshService
    {
        /// <summary>
        /// Checks if the token needs refresh and performs the refresh if necessary
        /// </summary>
        /// <param name="httpContext">Current HTTP context containing the session</param>
        /// <returns>True if token is valid (refreshed or still valid), false if refresh failed</returns>
        Task<bool> EnsureTokenIsValidAsync(HttpContext httpContext);

        /// <summary>
        /// Checks if the token is nearing expiration (within threshold)
        /// </summary>
        bool IsTokenNearingExpiration(HttpContext httpContext);
    }

    public class TokenRefreshService : ITokenRefreshService
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfiguration _configuration;
        private readonly ILogger<TokenRefreshService> _logger;

        // How many minutes before expiration should we refresh the token
        private readonly int _refreshThresholdMinutes;

        public TokenRefreshService(
            IHttpClientFactory httpClientFactory,
            IConfiguration configuration,
            ILogger<TokenRefreshService> logger)
        {
            _httpClientFactory = httpClientFactory;
            _configuration = configuration;
            _logger = logger;
            _refreshThresholdMinutes = configuration.GetValue<int>("Authentication:TokenRefreshThresholdMinutes", 5);
        }

        public bool IsTokenNearingExpiration(HttpContext httpContext)
        {
            var expiresUtcStr = httpContext.Session.GetString("TokenExpiresUtc");
            if (string.IsNullOrEmpty(expiresUtcStr))
            {
                return false; // No expiration info, assume we can't check
            }

            if (!DateTime.TryParse(expiresUtcStr, null, System.Globalization.DateTimeStyles.RoundtripKind, out var expiresUtc))
            {
                _logger.LogWarning("Failed to parse TokenExpiresUtc: {Value}", expiresUtcStr);
                return false;
            }

            var now = DateTime.UtcNow;
            var timeUntilExpiration = expiresUtc - now;

            return timeUntilExpiration.TotalMinutes <= _refreshThresholdMinutes;
        }

        public async Task<bool> EnsureTokenIsValidAsync(HttpContext httpContext)
        {
            // Check if token exists
            var currentToken = httpContext.Session.GetString("JwtToken");
            if (string.IsNullOrEmpty(currentToken))
            {
                _logger.LogDebug("No JWT token in session");
                return false;
            }

            // Check if token is nearing expiration
            if (!IsTokenNearingExpiration(httpContext))
            {
                _logger.LogDebug("Token is still valid, no refresh needed");
                return true;
            }

            // Get refresh token
            var refreshToken = httpContext.Session.GetString("RefreshToken");
            if (string.IsNullOrEmpty(refreshToken))
            {
                _logger.LogWarning("Token is expiring but no refresh token available");
                return false;
            }

            // Perform token refresh
            _logger.LogInformation("Token is nearing expiration, attempting refresh...");
            return await RefreshTokenAsync(httpContext, refreshToken);
        }

        private async Task<bool> RefreshTokenAsync(HttpContext httpContext, string refreshToken)
        {
            try
            {
                var loginServiceUrl = _configuration["LoginService:LoginUrl"];
                if (string.IsNullOrEmpty(loginServiceUrl))
                {
                    _logger.LogError("LoginService:LoginUrl not configured");
                    return false;
                }

                // Build refresh endpoint URL (replace /login with /refresh)
                var refreshUrl = loginServiceUrl.Replace("/login", "/refresh");

                var httpClient = _httpClientFactory.CreateClient();
                var requestBody = new { refreshToken };
                var json = JsonSerializer.Serialize(requestBody);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await httpClient.PostAsync(refreshUrl, content);
                var responseContent = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning("Token refresh failed with status {StatusCode}: {Response}",
                        response.StatusCode, responseContent);
                    return false;
                }

                var refreshResponse = JsonSerializer.Deserialize<RefreshTokenApiResponse>(
                    responseContent,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                if (refreshResponse?.Success == true &&
                    !string.IsNullOrEmpty(refreshResponse.Data?.Token) &&
                    !string.IsNullOrEmpty(refreshResponse.Data?.RefreshToken))
                {
                    // Update session with new tokens
                    httpContext.Session.SetString("JwtToken", refreshResponse.Data.Token);
                    httpContext.Session.SetString("RefreshToken", refreshResponse.Data.RefreshToken);

                    if (refreshResponse.Data.TokenExpiresUtc != default)
                    {
                        httpContext.Session.SetString("TokenExpiresUtc",
                            refreshResponse.Data.TokenExpiresUtc.ToString("O"));
                    }

                    _logger.LogInformation("Token refreshed successfully. New token expires at {ExpiresUtc}",
                        refreshResponse.Data.TokenExpiresUtc);

                    return true;
                }

                _logger.LogWarning("Token refresh response missing required data");
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception occurred during token refresh");
                return false;
            }
        }

        private class RefreshTokenApiResponse
        {
            public bool Success { get; set; }
            public string? Msg { get; set; }
            public RefreshTokenResponseData? Data { get; set; }
        }

        private class RefreshTokenResponseData
        {
            public string? Token { get; set; }
            public string? RefreshToken { get; set; }
            public DateTime TokenExpiresUtc { get; set; }
        }
    }
}
