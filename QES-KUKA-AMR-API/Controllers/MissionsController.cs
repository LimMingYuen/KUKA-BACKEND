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
using QES_KUKA_AMR_API.Services.SavedCustomMissions;
using QES_KUKA_AMR_API.Services.Queue;

namespace QES_KUKA_AMR_API.Controllers;

[ApiController]
[Route("api/missions")]
public class MissionsController : ControllerBase
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<MissionsController> _logger;
    private readonly MissionServiceOptions _missionOptions;
    private readonly ISavedCustomMissionService _savedCustomMissionService;
    private readonly IMissionEnqueueService _missionEnqueueService;

    public MissionsController(
        IHttpClientFactory httpClientFactory,
        ILogger<MissionsController> logger,
        IOptions<MissionServiceOptions> missionOptions,
        ISavedCustomMissionService savedCustomMissionService,
        IMissionEnqueueService missionEnqueueService)
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;
        _missionOptions = missionOptions.Value;
        _savedCustomMissionService = savedCustomMissionService;
        _missionEnqueueService = missionEnqueueService;
    }

    [HttpPost("save-as-template")]
    public async Task<ActionResult<SaveMissionAsTemplateResponse>> SaveMissionAsTemplateAsync(
        [FromBody] SaveMissionAsTemplateRequest request,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        // Get username from JWT token claims or default to System
        var createdBy = User.Identity?.Name
            ?? User.FindFirst("username")?.Value
            ?? User.FindFirst("sub")?.Value
            ?? "System";

        try
        {
            var template = request.MissionTemplate;

            // Serialize mission data to JSON
            var missionStepsJson = template.MissionData != null
                ? JsonSerializer.Serialize(template.MissionData)
                : "[]";

            // Convert robot models and IDs to comma-separated strings
            var robotModels = template.RobotModels?.Any() == true
                ? string.Join(",", template.RobotModels)
                : null;

            var robotIds = template.RobotIds?.Any() == true
                ? string.Join(",", template.RobotIds)
                : null;

            var savedMission = await _savedCustomMissionService.CreateAsync(new SavedCustomMission
            {
                MissionName = request.MissionName,
                Description = request.Description,
                MissionType = template.MissionType,
                RobotType = template.RobotType,
                Priority = template.Priority,
                RobotModels = robotModels,
                RobotIds = robotIds,
                ContainerModelCode = template.ContainerModelCode,
                ContainerCode = template.ContainerCode,
                IdleNode = template.IdleNode,
                OrgId = template.OrgId,
                ViewBoardType = template.ViewBoardType,
                TemplateCode = template.TemplateCode,
                LockRobotAfterFinish = template.LockRobotAfterFinish,
                UnlockRobotId = template.UnlockRobotId,
                UnlockMissionCode = template.UnlockMissionCode,
                MissionStepsJson = missionStepsJson
            }, createdBy, cancellationToken);

            _logger.LogInformation("Mission saved as template '{MissionName}' with ID {Id} by {CreatedBy}",
                request.MissionName, savedMission.Id, createdBy);

            return Ok(new SaveMissionAsTemplateResponse
            {
                Success = true,
                Message = "Mission saved as template successfully",
                SavedMissionId = savedMission.Id,
                MissionName = savedMission.MissionName
            });
        }
        catch (SavedCustomMissionConflictException ex)
        {
            _logger.LogWarning(ex, "Conflict while saving mission as template '{MissionName}'", request.MissionName);
            return Conflict(new SaveMissionAsTemplateResponse
            {
                Success = false,
                Message = ex.Message
            });
        }
        catch (SavedCustomMissionValidationException ex)
        {
            _logger.LogWarning(ex, "Validation error while saving mission as template '{MissionName}'", request.MissionName);
            return BadRequest(new SaveMissionAsTemplateResponse
            {
                Success = false,
                Message = ex.Message
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving mission as template '{MissionName}'", request.MissionName);
            return StatusCode(StatusCodes.Status500InternalServerError, new SaveMissionAsTemplateResponse
            {
                Success = false,
                Message = "An error occurred while saving the mission as a template"
            });
        }
    }

    [HttpPost("submit")]
    public async Task<ActionResult<SubmitMissionResponse>> SubmitMissionAsync(
        [FromBody] SubmitMissionRequest request,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Received mission submission - MissionCode={MissionCode}, TemplateCode={TemplateCode}, " +
            "Priority={Priority}, HasMissionData={HasMissionData}",
            request.MissionCode,
            request.TemplateCode,
            request.Priority,
            request.MissionData?.Any() == true
        );

        try
        {
            // DIRECT SUBMISSION TO AMR (BYPASSING QUEUE)
            _logger.LogInformation("Submitting mission {MissionCode} directly to AMR system", request.MissionCode);

            var httpClient = _httpClientFactory.CreateClient();
            httpClient.DefaultRequestHeaders.Clear();
            httpClient.DefaultRequestHeaders.Add("language", "en");
            httpClient.DefaultRequestHeaders.Add("accept", "*/*");
            httpClient.DefaultRequestHeaders.Add("wizards", "FRONT_END");

            var amrResponse = await httpClient.PostAsJsonAsync(
                _missionOptions.SubmitMissionUrl,
                request,
                cancellationToken
            );

            var responseContent = await amrResponse.Content.ReadAsStringAsync(cancellationToken);

            _logger.LogInformation(
                "AMR submission response - Status: {StatusCode}, Body: {Response}",
                amrResponse.StatusCode,
                responseContent
            );

            if (amrResponse.IsSuccessStatusCode)
            {
                var amrResult = JsonSerializer.Deserialize<SubmitMissionResponse>(responseContent);

                _logger.LogInformation(
                    "Mission {MissionCode} submitted successfully to AMR",
                    request.MissionCode
                );

                return Ok(new SubmitMissionResponse
                {
                    Success = true,
                    Code = "SUBMITTED",
                    Message = "Mission submitted successfully to AMR",
                    RequestId = request.RequestId,
                    Data = amrResult?.Data
                });
            }
            else
            {
                _logger.LogWarning(
                    "AMR submission failed - Status: {StatusCode}, Response: {Response}",
                    amrResponse.StatusCode,
                    responseContent
                );

                return StatusCode((int)amrResponse.StatusCode, new SubmitMissionResponse
                {
                    Success = false,
                    Code = "AMR_ERROR",
                    Message = $"AMR system returned error: {amrResponse.StatusCode}"
                });
            }
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "HTTP error submitting mission {MissionCode} to AMR", request.MissionCode);
            return StatusCode(StatusCodes.Status502BadGateway, new SubmitMissionResponse
            {
                Success = false,
                Code = "AMR_CONNECTION_ERROR",
                Message = "Failed to connect to AMR system"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error submitting mission {MissionCode}", request.MissionCode);
            return StatusCode(StatusCodes.Status500InternalServerError, new SubmitMissionResponse
            {
                Success = false,
                Code = "SUBMISSION_ERROR",
                Message = "An error occurred while submitting the mission"
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
