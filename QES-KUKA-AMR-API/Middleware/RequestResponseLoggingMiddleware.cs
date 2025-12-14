using System.Diagnostics;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace QES_KUKA_AMR_API.Middleware;

/// <summary>
/// Middleware that logs all HTTP requests and responses with full body content.
/// Automatically masks sensitive data like passwords and tokens.
/// </summary>
public class RequestResponseLoggingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<RequestResponseLoggingMiddleware> _logger;

    // Paths to exclude from logging
    private static readonly string[] ExcludedPaths = { "/swagger", "/favicon.ico", "/_framework" };

    // Maximum body size to log (10KB) - larger bodies will be truncated
    private const int MaxBodyLogSize = 10240;

    // Sensitive field patterns to mask (case-insensitive)
    private static readonly string[] SensitiveFieldPatterns = { "password", "token", "secret", "apikey", "api_key", "credential" };

    public RequestResponseLoggingMiddleware(RequestDelegate next, ILogger<RequestResponseLoggingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Skip logging for excluded paths
        var path = context.Request.Path.Value?.ToLowerInvariant() ?? "";
        if (ExcludedPaths.Any(excluded => path.StartsWith(excluded, StringComparison.OrdinalIgnoreCase)))
        {
            await _next(context);
            return;
        }

        // Generate correlation ID for tracking request/response pairs
        var correlationId = Guid.NewGuid().ToString("N")[..8].ToUpperInvariant();

        // Enable buffering to allow multiple reads of request body
        context.Request.EnableBuffering();

        // Log the request
        await LogRequestAsync(context, correlationId);

        // Capture the original response body stream
        var originalBodyStream = context.Response.Body;

        using var responseBody = new MemoryStream();
        context.Response.Body = responseBody;

        var stopwatch = Stopwatch.StartNew();

        try
        {
            // Call the next middleware in the pipeline
            await _next(context);
        }
        finally
        {
            stopwatch.Stop();

            // Log the response
            await LogResponseAsync(context, responseBody, correlationId, stopwatch.ElapsedMilliseconds);

            // Copy the response body back to the original stream
            responseBody.Seek(0, SeekOrigin.Begin);
            await responseBody.CopyToAsync(originalBodyStream);
        }
    }

    private async Task LogRequestAsync(HttpContext context, string correlationId)
    {
        try
        {
            var request = context.Request;

            // Read request body
            request.Body.Seek(0, SeekOrigin.Begin);
            using var reader = new StreamReader(request.Body, Encoding.UTF8, leaveOpen: true);
            var requestBody = await reader.ReadToEndAsync();
            request.Body.Seek(0, SeekOrigin.Begin);

            // Mask sensitive data
            var maskedBody = MaskSensitiveData(requestBody);

            // Truncate if too large
            if (maskedBody.Length > MaxBodyLogSize)
            {
                maskedBody = maskedBody[..MaxBodyLogSize] + "\n  [TRUNCATED - body exceeds 10KB]";
            }

            // Format body for logging
            var formattedBody = FormatJsonBody(maskedBody);

            // Get masked authorization header
            var authHeader = GetMaskedAuthorizationHeader(request);

            _logger.LogInformation(
                "=== API REQUEST [{CorrelationId}] ===\n" +
                "  Method: {Method}\n" +
                "  Path: {Path}\n" +
                "  Query: {Query}\n" +
                "  Authorization: {Auth}\n" +
                "  Content-Type: {ContentType}\n" +
                "  Body: {Body}",
                correlationId,
                request.Method,
                request.Path,
                request.QueryString.HasValue ? request.QueryString.Value : "(none)",
                authHeader,
                request.ContentType ?? "(none)",
                string.IsNullOrWhiteSpace(formattedBody) ? "(empty)" : formattedBody);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to log request [{CorrelationId}]", correlationId);
        }
    }

    private async Task LogResponseAsync(HttpContext context, MemoryStream responseBody, string correlationId, long durationMs)
    {
        try
        {
            // Read response body
            responseBody.Seek(0, SeekOrigin.Begin);
            using var reader = new StreamReader(responseBody, Encoding.UTF8, leaveOpen: true);
            var responseContent = await reader.ReadToEndAsync();
            responseBody.Seek(0, SeekOrigin.Begin);

            // Mask sensitive data
            var maskedBody = MaskSensitiveData(responseContent);

            // Truncate if too large
            if (maskedBody.Length > MaxBodyLogSize)
            {
                maskedBody = maskedBody[..MaxBodyLogSize] + "\n  [TRUNCATED - body exceeds 10KB]";
            }

            // Format body for logging
            var formattedBody = FormatJsonBody(maskedBody);

            // Determine log level based on status code
            var statusCode = context.Response.StatusCode;
            var logLevel = statusCode >= 500 ? LogLevel.Error
                         : statusCode >= 400 ? LogLevel.Warning
                         : LogLevel.Information;

            _logger.Log(logLevel,
                "=== API RESPONSE [{CorrelationId}] ===\n" +
                "  Status: {StatusCode}\n" +
                "  Duration: {Duration}ms\n" +
                "  Content-Type: {ContentType}\n" +
                "  Body: {Body}",
                correlationId,
                statusCode,
                durationMs,
                context.Response.ContentType ?? "(none)",
                string.IsNullOrWhiteSpace(formattedBody) ? "(empty)" : formattedBody);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to log response [{CorrelationId}]", correlationId);
        }
    }

    /// <summary>
    /// Masks sensitive data in JSON content
    /// </summary>
    private static string MaskSensitiveData(string content)
    {
        if (string.IsNullOrWhiteSpace(content))
            return content;

        try
        {
            // Try to parse and mask JSON
            using var doc = JsonDocument.Parse(content);
            var maskedJson = MaskJsonElement(doc.RootElement);
            return JsonSerializer.Serialize(maskedJson, new JsonSerializerOptions { WriteIndented = true });
        }
        catch
        {
            // If not valid JSON, use regex to mask common patterns
            return MaskSensitiveStrings(content);
        }
    }

    /// <summary>
    /// Recursively masks sensitive fields in JSON
    /// </summary>
    private static object? MaskJsonElement(JsonElement element)
    {
        switch (element.ValueKind)
        {
            case JsonValueKind.Object:
                var dict = new Dictionary<string, object?>();
                foreach (var property in element.EnumerateObject())
                {
                    var key = property.Name;
                    var isSensitive = SensitiveFieldPatterns.Any(pattern =>
                        key.Contains(pattern, StringComparison.OrdinalIgnoreCase));

                    if (isSensitive && property.Value.ValueKind == JsonValueKind.String)
                    {
                        var value = property.Value.GetString() ?? "";
                        dict[key] = MaskValue(value);
                    }
                    else
                    {
                        dict[key] = MaskJsonElement(property.Value);
                    }
                }
                return dict;

            case JsonValueKind.Array:
                var list = new List<object?>();
                foreach (var item in element.EnumerateArray())
                {
                    list.Add(MaskJsonElement(item));
                }
                return list;

            case JsonValueKind.String:
                return element.GetString();

            case JsonValueKind.Number:
                if (element.TryGetInt64(out var longValue))
                    return longValue;
                return element.GetDouble();

            case JsonValueKind.True:
                return true;

            case JsonValueKind.False:
                return false;

            case JsonValueKind.Null:
            default:
                return null;
        }
    }

    /// <summary>
    /// Masks a sensitive value, keeping first and last characters visible
    /// </summary>
    private static string MaskValue(string value)
    {
        if (string.IsNullOrEmpty(value))
            return "***MASKED***";

        if (value.Length <= 6)
            return "***MASKED***";

        // For JWT tokens, show "eyJ...last4"
        if (value.StartsWith("eyJ", StringComparison.Ordinal))
            return $"eyJ***...{value[^4..]}";

        // For other values, show first2...last2
        return $"{value[..2]}***...{value[^2..]}";
    }

    /// <summary>
    /// Fallback regex masking for non-JSON content
    /// </summary>
    private static string MaskSensitiveStrings(string content)
    {
        // Mask common patterns
        var result = content;

        // Mask password-like patterns: "password":"value" or password=value
        result = Regex.Replace(result, @"([""']?password[""']?\s*[:=]\s*[""']?)([^""',\s}]+)", "$1***MASKED***", RegexOptions.IgnoreCase);

        // Mask token-like patterns
        result = Regex.Replace(result, @"([""']?token[""']?\s*[:=]\s*[""']?)([^""',\s}]+)", "$1***MASKED***", RegexOptions.IgnoreCase);

        // Mask Bearer tokens
        result = Regex.Replace(result, @"(Bearer\s+)(\S+)", "$1***MASKED***", RegexOptions.IgnoreCase);

        return result;
    }

    /// <summary>
    /// Gets the Authorization header with token masked
    /// </summary>
    private static string GetMaskedAuthorizationHeader(HttpRequest request)
    {
        if (!request.Headers.TryGetValue("Authorization", out var authValue))
            return "(none)";

        var auth = authValue.ToString();
        if (string.IsNullOrEmpty(auth))
            return "(none)";

        // Mask Bearer token
        if (auth.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase) && auth.Length > 15)
        {
            return $"Bearer {auth[7..11]}***...{auth[^4..]}";
        }

        return "***MASKED***";
    }

    /// <summary>
    /// Formats JSON body for better readability in logs
    /// </summary>
    private static string FormatJsonBody(string body)
    {
        if (string.IsNullOrWhiteSpace(body))
            return body;

        try
        {
            // If it's already formatted JSON, return as-is
            if (body.Contains('\n'))
                return body;

            // Try to parse and format
            using var doc = JsonDocument.Parse(body);
            return JsonSerializer.Serialize(doc, new JsonSerializerOptions { WriteIndented = true });
        }
        catch
        {
            // Not JSON, return as-is
            return body;
        }
    }
}

/// <summary>
/// Extension methods for registering RequestResponseLoggingMiddleware
/// </summary>
public static class RequestResponseLoggingMiddlewareExtensions
{
    /// <summary>
    /// Adds the request/response logging middleware to the pipeline.
    /// This middleware logs all HTTP requests and responses with full body content.
    /// </summary>
    public static IApplicationBuilder UseRequestResponseLogging(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<RequestResponseLoggingMiddleware>();
    }
}
