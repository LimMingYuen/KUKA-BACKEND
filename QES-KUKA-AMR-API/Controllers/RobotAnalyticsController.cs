using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using QES_KUKA_AMR_API.Models.Analytics;
using QES_KUKA_AMR_API.Services.Analytics;
using QES_KUKA_AMR_API.Services.Missions;

namespace QES_KUKA_AMR_API.Controllers;

[ApiController]
[Authorize]
[Route("api/v1/robots")]
public class RobotAnalyticsController : ControllerBase
{
    private readonly IRobotAnalyticsService _robotAnalyticsService;
    private readonly IJobStatusClient _jobStatusClient;
    private readonly ILogger<RobotAnalyticsController> _logger;

    public RobotAnalyticsController(
        IRobotAnalyticsService robotAnalyticsService,
        IJobStatusClient jobStatusClient,
        ILogger<RobotAnalyticsController> logger)
    {
        _robotAnalyticsService = robotAnalyticsService;
        _jobStatusClient = jobStatusClient;
        _logger = logger;
    }

    [HttpGet("utilization")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetUtilizationAsync(
        [FromQuery] string? robotId,
        [FromQuery] DateTimeOffset? start,
        [FromQuery] DateTimeOffset? end,
        [FromQuery] string? groupBy,
        CancellationToken cancellationToken)
    {
        try
        {
            var effectiveEnd = end?.ToUniversalTime().UtcDateTime ?? DateTime.UtcNow;
            var effectiveStart = start?.ToUniversalTime().UtcDateTime ?? effectiveEnd.AddDays(-7);

            if (effectiveEnd <= effectiveStart)
            {
                return Problem(
                    statusCode: StatusCodes.Status400BadRequest,
                    title: "Invalid time range",
                    detail: "The end timestamp must be greater than the start timestamp.");
            }

            var grouping = ParseGrouping(groupBy);
            if (grouping is null)
            {
                return Problem(
                    statusCode: StatusCodes.Status400BadRequest,
                    title: "Invalid grouping interval",
                    detail: "Supported grouping values are 'day' and 'hour'.");
            }

            var effectiveRobotId = robotId;
            if (string.IsNullOrWhiteSpace(effectiveRobotId))
            {
                var robotIds = await _jobStatusClient.GetRobotIdsAsync(cancellationToken);
                effectiveRobotId = robotIds.FirstOrDefault();

                if (string.IsNullOrWhiteSpace(effectiveRobotId))
                {
                    return Problem(
                        statusCode: StatusCodes.Status404NotFound,
                        title: "No robots available",
                        detail: "The job status service did not return any robot identifiers.");
                }
            }

            // Extract JWT token from Authorization header
            string? jwtToken = null;
            if (Request.Headers.TryGetValue("Authorization", out var authHeader))
            {
                var authHeaderValue = authHeader.ToString();
                if (authHeaderValue.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
                {
                    jwtToken = authHeaderValue.Substring("Bearer ".Length).Trim();
                    _logger.LogInformation("JWT token extracted from Authorization header. Length: {Length}", jwtToken.Length);
                }
                else
                {
                    _logger.LogWarning("Authorization header present but does not start with 'Bearer '. Value: {Value}", authHeaderValue);
                }
            }
            else
            {
                _logger.LogWarning("No Authorization header found in request to /api/v1/robots/utilization");
            }

            var metrics = await _robotAnalyticsService.GetUtilizationAsync(
                effectiveRobotId!,
                effectiveStart,
                effectiveEnd,
                grouping.Value,
                jwtToken,
                cancellationToken);

            var response = new
            {
                success = true,
                data = metrics,
                traceId = HttpContext.TraceIdentifier
            };

            return Ok(response);
        }
        catch (ArgumentException argumentException)
        {
            _logger.LogWarning(argumentException, "Validation error while computing robot utilization.");
            return Problem(
                statusCode: StatusCodes.Status400BadRequest,
                title: "Invalid request",
                detail: argumentException.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error while computing robot utilization.");
            return Problem(
                statusCode: StatusCodes.Status500InternalServerError,
                title: "Failed to compute robot utilization",
                detail: "An unexpected error occurred.");
        }
    }

    [HttpGet("utilization/debug")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetUtilizationDebugAsync(
        [FromQuery] string? robotId,
        [FromQuery] DateTimeOffset? start,
        [FromQuery] DateTimeOffset? end,
        CancellationToken cancellationToken)
    {
        try
        {
            var effectiveEnd = end?.ToUniversalTime().UtcDateTime ?? DateTime.UtcNow;
            var effectiveStart = start?.ToUniversalTime().UtcDateTime ?? effectiveEnd.AddDays(-7);

            if (effectiveEnd <= effectiveStart)
            {
                return Problem(
                    statusCode: StatusCodes.Status400BadRequest,
                    title: "Invalid time range",
                    detail: "The end timestamp must be greater than the start timestamp.");
            }

            var effectiveRobotId = robotId;
            if (string.IsNullOrWhiteSpace(effectiveRobotId))
            {
                var robotIds = await _jobStatusClient.GetRobotIdsAsync(cancellationToken);
                effectiveRobotId = robotIds.FirstOrDefault();
            }

            var diagnostics = await _robotAnalyticsService.GetUtilizationDiagnosticsAsync(
                effectiveRobotId,
                effectiveStart,
                effectiveEnd,
                cancellationToken);

            var response = new
            {
                success = true,
                data = diagnostics,
                traceId = HttpContext.TraceIdentifier
            };

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error while retrieving utilization diagnostics.");
            return Problem(
                statusCode: StatusCodes.Status500InternalServerError,
                title: "Failed to retrieve diagnostics",
                detail: "An unexpected error occurred.");
        }
    }

    private static UtilizationGroupingInterval? ParseGrouping(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return UtilizationGroupingInterval.Day;
        }

        return value.ToLowerInvariant() switch
        {
            "hour" => UtilizationGroupingInterval.Hour,
            "day" => UtilizationGroupingInterval.Day,
            _ => null
        };
    }
}
