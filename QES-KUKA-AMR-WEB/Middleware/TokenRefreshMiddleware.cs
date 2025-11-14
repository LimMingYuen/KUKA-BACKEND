using QES_KUKA_AMR_WEB.Services;

namespace QES_KUKA_AMR_WEB.Middleware
{
    /// <summary>
    /// Middleware that automatically refreshes JWT tokens before they expire
    /// Runs on every request to check token expiration and refresh if needed
    /// </summary>
    public class TokenRefreshMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<TokenRefreshMiddleware> _logger;

        // Pages that don't require authentication (skip token refresh)
        private static readonly HashSet<string> ExcludedPaths = new(StringComparer.OrdinalIgnoreCase)
        {
            "/login",
            "/logout",
            "/error",
            "/_framework",
            "/css",
            "/js",
            "/lib",
            "/favicon.ico"
        };

        public TokenRefreshMiddleware(RequestDelegate next, ILogger<TokenRefreshMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context, ITokenRefreshService tokenRefreshService)
        {
            // Skip token refresh for excluded paths
            var path = context.Request.Path.Value ?? string.Empty;
            if (ExcludedPaths.Any(excluded => path.StartsWith(excluded, StringComparison.OrdinalIgnoreCase)))
            {
                await _next(context);
                return;
            }

            // Skip if not authenticated (no JWT token in session)
            var token = context.Session.GetString("JwtToken");
            if (string.IsNullOrEmpty(token))
            {
                await _next(context);
                return;
            }

            // Check and refresh token if needed
            try
            {
                var isValid = await tokenRefreshService.EnsureTokenIsValidAsync(context);

                if (!isValid)
                {
                    // Token refresh failed, redirect to login
                    _logger.LogWarning("Token refresh failed, redirecting to login. Path: {Path}", path);
                    context.Response.Redirect("/Login");
                    return;
                }
            }
            catch (Exception ex)
            {
                // Log error but don't block the request
                _logger.LogError(ex, "Error during token refresh check");
            }

            // Continue with the request
            await _next(context);
        }
    }

    /// <summary>
    /// Extension method to register the middleware
    /// </summary>
    public static class TokenRefreshMiddlewareExtensions
    {
        public static IApplicationBuilder UseTokenRefresh(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<TokenRefreshMiddleware>();
        }
    }
}
