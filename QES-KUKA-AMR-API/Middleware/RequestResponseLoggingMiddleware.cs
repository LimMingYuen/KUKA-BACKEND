using System.Diagnostics;
using System.Text;
using System.Text.Json;

namespace QES_KUKA_AMR_API.Middleware;

/// <summary>
/// Middleware that logs detailed information about every HTTP request and response.
/// Includes: method, path, query parameters, headers, request body, response status, and duration.
/// </summary>
public class RequestResponseLoggingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<RequestResponseLoggingMiddleware> _logger;

    // Paths to skip logging (reduce noise)
    private static readonly string[] SkipPaths = new[]
    {
        "/swagger",
        "/favicon.ico",
        "/hubs/queue",        // SignalR hub (too noisy)
        "/health"
    };

    // Headers that contain sensitive data (will be redacted)
    private static readonly string[] SensitiveHeaders = new[]
    {
        "Authorization",
        "Cookie",
        "X-Api-Key"
    };

    // JSON properties that contain sensitive data (will be redacted)
    private static readonly string[] SensitiveJsonProperties = new[]
    {
        "password",
        "passwordHash",
        "token",
        "accessToken",
        "refreshToken",
        "secret",
        "apiKey",
        "secretKey"
    };

    // Maximum body size to log (prevent memory issues with large uploads)
    private const int MaxBodyLogSize = 32 * 1024; // 32KB

    public RequestResponseLoggingMiddleware(RequestDelegate next, ILogger<RequestResponseLoggingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var path = context.Request.Path.Value?.ToLowerInvariant() ?? "";

        // Skip logging for excluded paths
        if (SkipPaths.Any(skip => path.StartsWith(skip, StringComparison.OrdinalIgnoreCase)))
        {
            await _next(context);
            return;
        }

        // Generate correlation ID for request tracing
        var correlationId = context.TraceIdentifier;
        if (string.IsNullOrEmpty(correlationId))
        {
            correlationId = Guid.NewGuid().ToString("N")[..8];
        }

        // Add correlation ID to response headers for debugging
        context.Response.Headers["X-Correlation-Id"] = correlationId;

        var stopwatch = Stopwatch.StartNew();

        // Log request details
        await LogRequestAsync(context, correlationId);

        // Capture response for logging
        var originalBodyStream = context.Response.Body;
        using var responseBodyStream = new MemoryStream();
        context.Response.Body = responseBodyStream;

        try
        {
            await _next(context);
        }
        finally
        {
            stopwatch.Stop();

            // Log response details
            await LogResponseAsync(context, correlationId, stopwatch.ElapsedMilliseconds, responseBodyStream);

            // Copy response body back to original stream
            responseBodyStream.Seek(0, SeekOrigin.Begin);
            await responseBodyStream.CopyToAsync(originalBodyStream);
            context.Response.Body = originalBodyStream;
        }
    }

    private async Task LogRequestAsync(HttpContext context, string correlationId)
    {
        var request = context.Request;

        // Build headers dictionary with redaction
        var headers = new Dictionary<string, string>();
        foreach (var header in request.Headers)
        {
            var headerValue = SensitiveHeaders.Contains(header.Key, StringComparer.OrdinalIgnoreCase)
                ? "[REDACTED]"
                : header.Value.ToString();
            headers[header.Key] = headerValue;
        }

        // Read and log request body
        string requestBody = "";
        if (request.ContentLength > 0 && request.ContentLength <= MaxBodyLogSize)
        {
            request.EnableBuffering();
            using var reader = new StreamReader(
                request.Body,
                Encoding.UTF8,
                detectEncodingFromByteOrderMarks: false,
                bufferSize: 1024,
                leaveOpen: true);

            requestBody = await reader.ReadToEndAsync();
            request.Body.Position = 0;

            // Redact sensitive data in JSON body
            if (!string.IsNullOrEmpty(requestBody) &&
                request.ContentType?.Contains("application/json", StringComparison.OrdinalIgnoreCase) == true)
            {
                requestBody = RedactSensitiveJsonData(requestBody);
            }
        }
        else if (request.ContentLength > MaxBodyLogSize)
        {
            requestBody = $"[BODY TOO LARGE: {request.ContentLength} bytes]";
        }

        // Get user identity if authenticated
        var userId = context.User?.Identity?.IsAuthenticated == true
            ? context.User.Identity.Name ?? "Unknown"
            : "Anonymous";

        // Build query string
        var queryString = request.QueryString.HasValue ? request.QueryString.Value : "";

        _logger.LogInformation(
            "[{CorrelationId}] REQUEST: {Method} {Path}{QueryString} | User: {UserId} | " +
            "ContentType: {ContentType} | ContentLength: {ContentLength}",
            correlationId,
            request.Method,
            request.Path,
            queryString,
            userId,
            request.ContentType ?? "N/A",
            request.ContentLength ?? 0);

        // Log headers at Debug level (more verbose)
        if (_logger.IsEnabled(LogLevel.Debug))
        {
            _logger.LogDebug(
                "[{CorrelationId}] REQUEST HEADERS: {Headers}",
                correlationId,
                JsonSerializer.Serialize(headers, new JsonSerializerOptions { WriteIndented = false }));
        }

        // Log request body if present
        if (!string.IsNullOrEmpty(requestBody))
        {
            _logger.LogInformation(
                "[{CorrelationId}] REQUEST BODY: {Body}",
                correlationId,
                requestBody);
        }
    }

    private async Task LogResponseAsync(HttpContext context, string correlationId, long elapsedMs, MemoryStream responseBodyStream)
    {
        var response = context.Response;

        // Read response body
        responseBodyStream.Seek(0, SeekOrigin.Begin);
        string responseBody = "";

        if (responseBodyStream.Length > 0 && responseBodyStream.Length <= MaxBodyLogSize)
        {
            responseBody = await new StreamReader(responseBodyStream).ReadToEndAsync();

            // Redact sensitive data in JSON response
            if (!string.IsNullOrEmpty(responseBody) &&
                response.ContentType?.Contains("application/json", StringComparison.OrdinalIgnoreCase) == true)
            {
                responseBody = RedactSensitiveJsonData(responseBody);
            }
        }
        else if (responseBodyStream.Length > MaxBodyLogSize)
        {
            responseBody = $"[BODY TOO LARGE: {responseBodyStream.Length} bytes]";
        }

        // Determine log level based on status code
        var logLevel = response.StatusCode switch
        {
            >= 500 => LogLevel.Error,
            >= 400 => LogLevel.Warning,
            _ => LogLevel.Information
        };

        _logger.Log(
            logLevel,
            "[{CorrelationId}] RESPONSE: {StatusCode} | Duration: {ElapsedMs}ms | " +
            "ContentType: {ContentType} | ContentLength: {ContentLength}",
            correlationId,
            response.StatusCode,
            elapsedMs,
            response.ContentType ?? "N/A",
            responseBodyStream.Length);

        // Log response body (only for non-success or when debugging)
        if (!string.IsNullOrEmpty(responseBody))
        {
            if (response.StatusCode >= 400 || _logger.IsEnabled(LogLevel.Debug))
            {
                _logger.Log(
                    logLevel,
                    "[{CorrelationId}] RESPONSE BODY: {Body}",
                    correlationId,
                    responseBody);
            }
        }
    }

    /// <summary>
    /// Redacts sensitive fields in JSON data to prevent logging passwords, tokens, etc.
    /// </summary>
    private string RedactSensitiveJsonData(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
            return json;

        try
        {
            using var doc = JsonDocument.Parse(json);
            using var stream = new MemoryStream();
            using var writer = new Utf8JsonWriter(stream, new JsonWriterOptions { Indented = false });

            RedactJsonElement(doc.RootElement, writer);

            writer.Flush();
            return Encoding.UTF8.GetString(stream.ToArray());
        }
        catch
        {
            // If JSON parsing fails, return original (might not be valid JSON)
            return json;
        }
    }

    private void RedactJsonElement(JsonElement element, Utf8JsonWriter writer)
    {
        switch (element.ValueKind)
        {
            case JsonValueKind.Object:
                writer.WriteStartObject();
                foreach (var property in element.EnumerateObject())
                {
                    writer.WritePropertyName(property.Name);

                    // Check if this property should be redacted
                    if (SensitiveJsonProperties.Contains(property.Name, StringComparer.OrdinalIgnoreCase))
                    {
                        writer.WriteStringValue("[REDACTED]");
                    }
                    else
                    {
                        RedactJsonElement(property.Value, writer);
                    }
                }
                writer.WriteEndObject();
                break;

            case JsonValueKind.Array:
                writer.WriteStartArray();
                foreach (var item in element.EnumerateArray())
                {
                    RedactJsonElement(item, writer);
                }
                writer.WriteEndArray();
                break;

            case JsonValueKind.String:
                writer.WriteStringValue(element.GetString());
                break;

            case JsonValueKind.Number:
                writer.WriteRawValue(element.GetRawText());
                break;

            case JsonValueKind.True:
                writer.WriteBooleanValue(true);
                break;

            case JsonValueKind.False:
                writer.WriteBooleanValue(false);
                break;

            case JsonValueKind.Null:
                writer.WriteNullValue();
                break;
        }
    }
}

/// <summary>
/// Extension method for cleaner middleware registration
/// </summary>
public static class RequestResponseLoggingMiddlewareExtensions
{
    public static IApplicationBuilder UseRequestResponseLogging(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<RequestResponseLoggingMiddleware>();
    }
}
