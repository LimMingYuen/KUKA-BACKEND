using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using QES_KUKA_AMR_API.Models;
using QES_KUKA_AMR_API.Models.Missions;
using QES_KUKA_AMR_API.Options;

namespace QES_KUKA_AMR_API.Controllers;

[ApiController]
[Authorize]
[Route("api/robot-query")]
public class RobotQueryController : ControllerBase
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<RobotQueryController> _logger;
    private readonly AmrServiceOptions _amrOptions;

    public RobotQueryController(
        IHttpClientFactory httpClientFactory,
        ILogger<RobotQueryController> logger,
        IOptions<AmrServiceOptions> amrOptions)
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;
        _amrOptions = amrOptions.Value;
    }

    [HttpPost]
    public async Task<ActionResult<RobotQueryResponse>> QueryRobotPositionAsync(
        [FromBody] RobotQueryRequest request,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("=== RobotQueryController.QueryRobotPositionAsync DEBUG ===");
        _logger.LogInformation("Querying robot position - RobotId={RobotId}, MapCode={MapCode}, FloorNumber={FloorNumber}",
            request.RobotId, request.MapCode, request.FloorNumber);

        if (string.IsNullOrWhiteSpace(_amrOptions.RobotQueryUrl) ||
            !Uri.TryCreate(_amrOptions.RobotQueryUrl, UriKind.Absolute, out var requestUri))
        {
            _logger.LogError("Robot query URL is not configured correctly.");
            return StatusCode(StatusCodes.Status500InternalServerError, new RobotQueryResponse
            {
                Code = "AMR_SERVICE_CONFIGURATION_ERROR",
                Message = "Robot query URL is not configured.",
                Success = false
            });
        }

        var httpClient = _httpClientFactory.CreateClient();

        var apiRequest = new HttpRequestMessage(HttpMethod.Post, requestUri)
        {
            Content = JsonContent.Create(request)
        };

        // Add custom headers required by AMR backend
        apiRequest.Content.Headers.ContentType = new MediaTypeHeaderValue("application/json")
        {
            CharSet = "UTF-8"
        };
        apiRequest.Headers.Add("language", "en");
        apiRequest.Headers.Add("accept", "*/*");
        apiRequest.Headers.Add("wizards", "FRONT_END");

        try
        {
            using var response = await httpClient.SendAsync(apiRequest, cancellationToken);

            var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);

            _logger.LogInformation(
                "AMR robot query response - Status: {StatusCode}, Body: {Response}",
                response.StatusCode,
                responseContent
            );

            // Handle success responses
            if (response.IsSuccessStatusCode)
            {
                var serviceResponse = System.Text.Json.JsonSerializer.Deserialize<RobotQueryResponse>(
                    responseContent,
                    new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true }
                );

                if (serviceResponse is null)
                {
                    _logger.LogError(
                        "AMR service returned no content for robot query. Status: {StatusCode}",
                        response.StatusCode);

                    return StatusCode(StatusCodes.Status502BadGateway, new RobotQueryResponse
                    {
                        Code = "AMR_SERVICE_EMPTY_RESPONSE",
                        Message = "Failed to retrieve a response from the AMR service.",
                        Success = false
                    });
                }

                if (serviceResponse.Data != null && serviceResponse.Data.Count > 0)
                {
                    var robot = serviceResponse.Data[0];
                    _logger.LogInformation("✓ Robot {RobotId} found at node {NodeCode}", robot.RobotId, robot.NodeCode);
                }
                else
                {
                    _logger.LogWarning("✗ Robot {RobotId} not found or no data returned", request.RobotId);
                }

                _logger.LogInformation("=== END RobotQueryController.QueryRobotPositionAsync DEBUG ===");

                return Ok(serviceResponse);
            }
            else
            {
                // Handle error responses (e.g., 500 errors from external API)
                _logger.LogWarning(
                    "AMR robot query failed - Status: {StatusCode}, Response: {Response}",
                    response.StatusCode,
                    responseContent
                );

                // Try to deserialize error response to extract actual error message
                try
                {
                    // Try external API error format first (for 500 errors)
                    var errorResponse = System.Text.Json.JsonSerializer.Deserialize<ExternalApiErrorResponse>(
                        responseContent,
                        new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true }
                    );

                    if (errorResponse?.Message != null)
                    {
                        _logger.LogWarning(
                            "AMR robot query error details - Message: {Message}, Exception: {Exception}",
                            errorResponse.Message,
                            errorResponse.Exception
                        );

                        return StatusCode((int)response.StatusCode, new RobotQueryResponse
                        {
                            Code = errorResponse.Code ?? "AMR_ROBOT_QUERY_ERROR",
                            Message = errorResponse.Message,
                            Success = false
                        });
                    }

                    // Try RobotQueryResponse format (in case error is in standard format)
                    var standardErrorResponse = System.Text.Json.JsonSerializer.Deserialize<RobotQueryResponse>(
                        responseContent,
                        new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true }
                    );

                    if (standardErrorResponse != null)
                    {
                        return StatusCode((int)response.StatusCode, standardErrorResponse);
                    }
                }
                catch (System.Text.Json.JsonException ex)
                {
                    _logger.LogWarning(ex, "Failed to deserialize AMR robot query error response");
                }

                // Fallback if deserialization fails
                return StatusCode((int)response.StatusCode, new RobotQueryResponse
                {
                    Code = "AMR_ROBOT_QUERY_ERROR",
                    Message = $"Robot query failed with status {response.StatusCode}",
                    Success = false
                });
            }
        }
        catch (HttpRequestException httpRequestException)
        {
            _logger.LogError(
                httpRequestException,
                "Error while calling robot query endpoint at {Url}",
                _amrOptions.RobotQueryUrl);

            return StatusCode(StatusCodes.Status502BadGateway, new RobotQueryResponse
            {
                Code = "AMR_SERVICE_HTTP_ERROR",
                Message = $"HTTP error while contacting AMR service: {httpRequestException.Message}",
                Success = false
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error while querying robot position");

            return StatusCode(StatusCodes.Status500InternalServerError, new RobotQueryResponse
            {
                Code = "INTERNAL_SERVER_ERROR",
                Message = $"Unexpected error: {ex.Message}",
                Success = false
            });
        }
    }
}
