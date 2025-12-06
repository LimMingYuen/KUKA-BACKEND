using System.Text.Json;
using QES_KUKA_AMR_API.Models;
using QES_KUKA_AMR_API.Services.Licensing;

namespace QES_KUKA_AMR_API.Middleware;

public class LicenseEnforcementMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<LicenseEnforcementMiddleware> _logger;

    // Endpoints that are allowed without a valid license
    private static readonly string[] AllowedPaths = new[]
    {
        "/api/license",       // All license endpoints
        "/swagger",           // Swagger UI
        "/health",            // Health checks if any
        "/favicon.ico"        // Static files
    };

    public LicenseEnforcementMiddleware(RequestDelegate next, ILogger<LicenseEnforcementMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context, ILicenseStateService licenseState)
    {
        // Check if license is in limited mode
        if (licenseState.IsLimitedMode)
        {
            var path = context.Request.Path.Value?.ToLowerInvariant() ?? "";

            // Check if the path is allowed without a license
            var isAllowed = AllowedPaths.Any(allowed =>
                path.StartsWith(allowed, StringComparison.OrdinalIgnoreCase));

            if (!isAllowed)
            {
                _logger.LogWarning("Blocked request to {Path} - License required", path);

                context.Response.StatusCode = StatusCodes.Status403Forbidden;
                context.Response.ContentType = "application/json";

                var response = new ApiResponse<object>
                {
                    Success = false,
                    Code = "LICENSE_REQUIRED",
                    Msg = "A valid license is required to access this feature. Please activate a license."
                };

                var json = JsonSerializer.Serialize(response, new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                });

                await context.Response.WriteAsync(json);
                return;
            }
        }

        await _next(context);
    }
}

// Extension method for cleaner registration
public static class LicenseEnforcementMiddlewareExtensions
{
    public static IApplicationBuilder UseLicenseEnforcement(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<LicenseEnforcementMiddleware>();
    }
}
