using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using QES_KUKA_AMR_API.Data;
using QES_KUKA_AMR_API.Data.Entities;
using QES_KUKA_AMR_API.Models.Missions;
using QES_KUKA_AMR_API.Models.Jobs;
using QES_KUKA_AMR_API.Options;
using QES_KUKA_AMR_API.Services;

namespace QES_KUKA_AMR_API.Controllers;

[ApiController]
[Route("api/missions")]
public class MissionsController : ControllerBase
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<MissionsController> _logger;
    private readonly MissionServiceOptions _missionOptions;

    public MissionsController(
        IHttpClientFactory httpClientFactory,
        ILogger<MissionsController> logger,
        IOptions<MissionServiceOptions> missionOptions)
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;
        _missionOptions = missionOptions.Value;
    }

    [HttpPost("submit")]
    public async Task<ActionResult<SubmitMissionResponse>> SubmitMissionAsync(
        [FromBody] SubmitMissionRequest request,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("=== MissionsController.SubmitMissionAsync DEBUG ===");
        _logger.LogInformation("Received mission submission - MissionCode={MissionCode}, TemplateCode={TemplateCode}, Priority={Priority}, RequestId={RequestId}",
            request.MissionCode, request.TemplateCode, request.Priority, request.RequestId);
        _logger.LogInformation("=== END MissionsController.SubmitMissionAsync DEBUG ===");

        if (string.IsNullOrWhiteSpace(_missionOptions.SubmitMissionUrl) ||
            !Uri.TryCreate(_missionOptions.SubmitMissionUrl, UriKind.Absolute, out var requestUri))
        {
            _logger.LogError("Submit mission URL is not configured correctly.");
            return StatusCode(StatusCodes.Status500InternalServerError, new SubmitMissionResponse
            {
                Code = "MISSION_SERVICE_CONFIGURATION_ERROR",
                Message = "Submit mission URL is not configured.",
                Success = false
            });
        }

        var httpClient = _httpClientFactory.CreateClient();

        var apiRequest = new HttpRequestMessage(
            HttpMethod.Post,
            requestUri)
        {
            Content = JsonContent.Create(request)
        };

        // Add custom headers required by real backend (no auth required)
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

            var serviceResponse =
                await response.Content.ReadFromJsonAsync<SubmitMissionResponse>(cancellationToken: cancellationToken);

            if (serviceResponse is null)
            {
                _logger.LogError(
                    "Mission service returned no content for submit mission. Status: {StatusCode}",
                    response.StatusCode);

                return StatusCode(StatusCodes.Status502BadGateway, new SubmitMissionResponse
                {
                    Code = "MISSION_SERVICE_EMPTY_RESPONSE",
                    Message = "Failed to retrieve a response from the mission service.",
                    Success = false
                });
            }

            return StatusCode((int)response.StatusCode, serviceResponse);
        }
        catch (HttpRequestException httpRequestException)
        {
            _logger.LogError(
                httpRequestException,
                "Error while calling mission submit endpoint at {BaseAddress}",
                httpClient.BaseAddress);

            return StatusCode(StatusCodes.Status502BadGateway, new SubmitMissionResponse
            {
                Code = "MISSION_SERVICE_UNREACHABLE",
                Message = "Unable to reach the mission submit endpoint.",
                Success = false
            });
        }
    }

    [HttpPost("cancel")]
    public async Task<ActionResult<MissionCancelResponse>> CancelMissionAsync(
        [FromBody] MissionCancelRequest request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(_missionOptions.MissionCancelUrl) ||
            !Uri.TryCreate(_missionOptions.MissionCancelUrl, UriKind.Absolute, out var requestUri))
        {
            _logger.LogError("Mission cancel URL is not configured correctly.");
            return StatusCode(StatusCodes.Status500InternalServerError, new MissionCancelResponse
            {
                Code = "MISSION_SERVICE_CONFIGURATION_ERROR",
                Message = "Mission cancel URL is not configured.",
                Success = false
            });
        }

        var httpClient = _httpClientFactory.CreateClient();

        var apiRequest = new HttpRequestMessage(
            HttpMethod.Post,
            requestUri)
        {
            Content = JsonContent.Create(request)
        };

        // Add custom headers required by real backend (no auth required)
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

            var serviceResponse =
                await response.Content.ReadFromJsonAsync<MissionCancelResponse>(cancellationToken: cancellationToken);

            if (serviceResponse is null)
            {
                _logger.LogError(
                    "Mission service returned no content for mission cancel. Status: {StatusCode}",
                    response.StatusCode);

                return StatusCode(StatusCodes.Status502BadGateway, new MissionCancelResponse
                {
                    Code = "MISSION_SERVICE_EMPTY_RESPONSE",
                    Message = "Failed to retrieve a response from the mission service.",
                    Success = false
                });
            }

            return StatusCode((int)response.StatusCode, serviceResponse);
        }
        catch (HttpRequestException httpRequestException)
        {
            _logger.LogError(
                httpRequestException,
                "Error while calling mission cancel endpoint at {BaseAddress}",
                httpClient.BaseAddress);

            return StatusCode(StatusCodes.Status502BadGateway, new MissionCancelResponse
            {
                Code = "MISSION_SERVICE_UNREACHABLE",
                Message = "Unable to reach the mission cancel endpoint.",
                Success = false
            });
        }
    }

    [HttpPost("jobs/query")]
    public async Task<ActionResult<JobQueryResponse>> QueryJobsAsync(
        [FromBody] JobQueryRequest request,
        CancellationToken cancellationToken)
    {
        request.Limit ??= 10;

        if (string.IsNullOrWhiteSpace(_missionOptions.JobQueryUrl) ||
            !Uri.TryCreate(_missionOptions.JobQueryUrl, UriKind.Absolute, out var requestUri))
        {
            _logger.LogError("Job query URL is not configured correctly.");
            return StatusCode(StatusCodes.Status500InternalServerError, new JobQueryResponse
            {
                Code = "MISSION_SERVICE_CONFIGURATION_ERROR",
                Message = "Job query URL is not configured.",
                Success = false
            });
        }

        var httpClient = _httpClientFactory.CreateClient();

        var apiRequest = new HttpRequestMessage(
            HttpMethod.Post,
            requestUri)
        {
            Content = JsonContent.Create(request)
        };

        // Add custom headers required by real backend (no auth required)
        apiRequest.Content.Headers.ContentType = new MediaTypeHeaderValue("application/json")
        {
            CharSet = "UTF-8"
        };
        apiRequest.Headers.Add("language", "en");
        apiRequest.Headers.Add("accept", "*/*");
        apiRequest.Headers.Add("wizards", "FRONT_END");

        // Log request details
        var requestBody = JsonSerializer.Serialize(request);
        _logger.LogInformation("QueryJobsAsync Request - URL: {Url}, Body: {Body}",
            requestUri, requestBody);

        try
        {
            using var response = await httpClient.SendAsync(apiRequest, cancellationToken);

            var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);
            _logger.LogInformation("QueryJobsAsync Response - Status: {StatusCode}, Body: {Body}",
                response.StatusCode, responseBody);

            var serviceResponse = JsonSerializer.Deserialize<JobQueryResponse>(responseBody,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            if (serviceResponse is null)
            {
                _logger.LogError(
                    "Mission service returned no content for job query. Status: {StatusCode}",
                    response.StatusCode);

                return StatusCode(StatusCodes.Status502BadGateway, new JobQueryResponse
                {
                    Code = "MISSION_SERVICE_EMPTY_RESPONSE",
                    Message = "Failed to retrieve a response from the mission service.",
                    Success = false
                });
            }

            return StatusCode((int)response.StatusCode, serviceResponse);
        }
        catch (HttpRequestException httpRequestException)
        {
            _logger.LogError(
                httpRequestException,
                "Error while calling mission job query endpoint at {BaseAddress}",
                httpClient.BaseAddress);

            return StatusCode(StatusCodes.Status502BadGateway, new JobQueryResponse
            {
                Code = "MISSION_SERVICE_UNREACHABLE",
                Message = "Unable to reach the mission job query endpoint.",
                Success = false
            });
        }
    }

    [HttpPost("operation-feedback")]
    public async Task<ActionResult<OperationFeedbackResponse>> OperationFeedbackAsync(
        [FromBody] OperationFeedbackRequest request,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Operation feedback received - RequestId={RequestId}, MissionCode={MissionCode}, Position={Position}",
            request.RequestId, request.MissionCode, request.Position);

        if (string.IsNullOrWhiteSpace(_missionOptions.OperationFeedbackUrl) ||
            !Uri.TryCreate(_missionOptions.OperationFeedbackUrl, UriKind.Absolute, out var requestUri))
        {
            _logger.LogError("Operation feedback URL is not configured correctly.");
            return StatusCode(StatusCodes.Status500InternalServerError, new OperationFeedbackResponse
            {
                Code = "MISSION_SERVICE_CONFIGURATION_ERROR",
                Message = "Operation feedback URL is not configured.",
                Success = false
            });
        }

        var httpClient = _httpClientFactory.CreateClient();

        var apiRequest = new HttpRequestMessage(
            HttpMethod.Post,
            requestUri)
        {
            Content = JsonContent.Create(request)
        };

        // Add custom headers required by real backend (no auth required)
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

            var serviceResponse =
                await response.Content.ReadFromJsonAsync<OperationFeedbackResponse>(cancellationToken: cancellationToken);

            if (serviceResponse is null)
            {
                _logger.LogError(
                    "Mission service returned no content for operation feedback. Status: {StatusCode}",
                    response.StatusCode);

                return StatusCode(StatusCodes.Status502BadGateway, new OperationFeedbackResponse
                {
                    Code = "MISSION_SERVICE_EMPTY_RESPONSE",
                    Message = "Failed to retrieve a response from the mission service.",
                    Success = false
                });
            }

            return StatusCode((int)response.StatusCode, serviceResponse);
        }
        catch (HttpRequestException httpRequestException)
        {
            _logger.LogError(
                httpRequestException,
                "Error while calling operation feedback endpoint at {BaseAddress}",
                httpClient.BaseAddress);

            return StatusCode(StatusCodes.Status502BadGateway, new OperationFeedbackResponse
            {
                Code = "MISSION_SERVICE_UNREACHABLE",
                Message = "Unable to reach the operation feedback endpoint.",
                Success = false
            });
        }
    }

    [HttpPost("robot-query")]
    public async Task<ActionResult<RobotQueryResponse>> RobotQueryAsync(
        [FromBody] RobotQueryRequest request,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Robot query received - RobotId={RobotId}, RobotType={RobotType}, MapCode={MapCode}, FloorNumber={FloorNumber}",
            request.RobotId, request.RobotType, request.MapCode, request.FloorNumber);

        if (string.IsNullOrWhiteSpace(_missionOptions.RobotQueryUrl) ||
            !Uri.TryCreate(_missionOptions.RobotQueryUrl, UriKind.Absolute, out var requestUri))
        {
            _logger.LogError("Robot query URL is not configured correctly.");
            return StatusCode(StatusCodes.Status500InternalServerError, new RobotQueryResponse
            {
                Code = "MISSION_SERVICE_CONFIGURATION_ERROR",
                Message = "Robot query URL is not configured.",
                Success = false
            });
        }

        var httpClient = _httpClientFactory.CreateClient();

        var apiRequest = new HttpRequestMessage(
            HttpMethod.Post,
            requestUri)
        {
            Content = JsonContent.Create(request)
        };

        // Add custom headers required by real backend (no auth required)
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

            var serviceResponse =
                await response.Content.ReadFromJsonAsync<RobotQueryResponse>(cancellationToken: cancellationToken);

            if (serviceResponse is null)
            {
                _logger.LogError(
                    "Mission service returned no content for robot query. Status: {StatusCode}",
                    response.StatusCode);

                return StatusCode(StatusCodes.Status502BadGateway, new RobotQueryResponse
                {
                    Code = "MISSION_SERVICE_EMPTY_RESPONSE",
                    Message = "Failed to retrieve a response from the mission service.",
                    Success = false
                });
            }

            return StatusCode((int)response.StatusCode, serviceResponse);
        }
        catch (HttpRequestException httpRequestException)
        {
            _logger.LogError(
                httpRequestException,
                "Error while calling robot query endpoint at {BaseAddress}",
                httpClient.BaseAddress);

            return StatusCode(StatusCodes.Status502BadGateway, new RobotQueryResponse
            {
                Code = "MISSION_SERVICE_UNREACHABLE",
                Message = "Unable to reach the robot query endpoint.",
                Success = false
            });
        }
    }

    [HttpPost("resume-manual-waypoint")]
    public async Task<ActionResult<ResumeManualWaypointResponse>> ResumeManualWaypointAsync(
        [FromBody] ResumeManualWaypointRequest request,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Resume manual waypoint request received for mission {MissionCode}", request.MissionCode);

        if (string.IsNullOrWhiteSpace(request.MissionCode))
        {
            return BadRequest(new ResumeManualWaypointResponse
            {
                Success = false,
                Message = "MissionCode is required"
            });
        }

        // Queue functionality removed
        return Ok(new ResumeManualWaypointResponse
        {
            Success = false,
            Message = "Queue functionality has been removed from the system",
            RequestId = ""
        });
    }

    [HttpGet("waiting-for-resume")]
    public async Task<ActionResult<List<WaitingMissionDto>>> GetWaitingMissionsAsync(
        CancellationToken cancellationToken)
    {
        // Queue functionality removed - return empty list
        return Ok(new List<WaitingMissionDto>());
    }
}

public class ResumeManualWaypointRequest
{
    public string MissionCode { get; set; } = string.Empty;
}

public class ResumeManualWaypointResponse
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public string? RequestId { get; set; }
}

public class WaitingMissionDto
{
    public string MissionCode { get; set; } = string.Empty;
    public string? CurrentPosition { get; set; }
    public string? RobotId { get; set; }
    public int? BatteryLevel { get; set; }
    public DateTime? WaitingSince { get; set; }
}
