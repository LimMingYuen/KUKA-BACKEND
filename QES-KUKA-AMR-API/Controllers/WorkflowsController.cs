using System.Linq;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using QES_KUKA_AMR_API.Data;
using QES_KUKA_AMR_API.Data.Entities;
using QES_KUKA_AMR_API.Models.Config;
using QES_KUKA_AMR_API.Models.Workflow;
using QES_KUKA_AMR_API.Options;

namespace QES_KUKA_AMR_API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class WorkflowsController : ControllerBase
{
    private const int SyncPageNumber = 1;
    private const int SyncPageSize = 10_000;

    private readonly ApplicationDbContext _dbContext;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<WorkflowsController> _logger;
    private readonly MissionServiceOptions _missionOptions;

    public WorkflowsController(
        ApplicationDbContext dbContext,
        IHttpClientFactory httpClientFactory,
        ILogger<WorkflowsController> logger,
        IOptions<MissionServiceOptions> missionOptions)
    {
        _dbContext = dbContext;
        _httpClientFactory = httpClientFactory;
        _logger = logger;
        _missionOptions = missionOptions.Value;
    }

    [HttpPost("sync")]
    public async Task<ActionResult<WorkflowSyncResultDto>> SyncAsync(CancellationToken cancellationToken)
    {
        if (!AuthenticationHeaderValue.TryParse(Request.Headers.Authorization, out var authHeader) ||
            string.IsNullOrWhiteSpace(authHeader.Parameter))
        {
            return StatusCode(StatusCodes.Status401Unauthorized, new
            {
                Code = StatusCodes.Status401Unauthorized,
                Message = "Missing or invalid Authorization header."
            });
        }

        // Strip "Bearer " prefix if it was accidentally included in the parameter
        var token = authHeader.Parameter;
        if (token.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
        {
            token = token.Substring(7).Trim();
        }

        if (string.IsNullOrWhiteSpace(_missionOptions.WorkflowQueryUrl) ||
            !Uri.TryCreate(_missionOptions.WorkflowQueryUrl, UriKind.Absolute, out var requestUri))
        {
            _logger.LogError("Workflow query URL is not configured correctly.");
            return StatusCode(StatusCodes.Status500InternalServerError, new
            {
                Code = StatusCodes.Status500InternalServerError,
                Message = "Workflow query URL is not configured."
            });
        }

        var httpClient = _httpClientFactory.CreateClient();

        var apiRequest = new HttpRequestMessage(HttpMethod.Post, requestUri)
        {
            Content = JsonContent.Create(new QueryWorkflowDiagramsRequest
            {
                PageNum = SyncPageNumber,
                PageSize = SyncPageSize
            })
        };
        apiRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Add custom headers required by real backend
        apiRequest.Content.Headers.ContentType = new MediaTypeHeaderValue("application/json")
        {
            CharSet = "UTF-8"
        };
        apiRequest.Headers.Add("language", "en");
        apiRequest.Headers.Add("accept", "*/*");
        apiRequest.Headers.Add("wizards", "FRONT_END");

        // Forward any cookies from the original request
        if (Request.Headers.TryGetValue("Cookie", out var cookies))
        {
            apiRequest.Headers.Add("Cookie", cookies.ToString());
        }

        // Log request details for debugging
        _logger.LogInformation("=== Workflow Sync Request Debug ===");
        _logger.LogInformation("Target URI: {Uri}", requestUri);
        _logger.LogInformation("Auth Scheme: {Scheme}", authHeader.Scheme);
        _logger.LogInformation("Original Token Parameter: {Token}", authHeader.Parameter);
        _logger.LogInformation("Cleaned Token: {Token}", token);
        _logger.LogInformation("Token Length: {Length}", token.Length);
        _logger.LogInformation("Request Headers:");
        foreach (var header in apiRequest.Headers)
        {
            _logger.LogInformation("  {Key}: {Value}", header.Key, string.Join(", ", header.Value));
        }
        _logger.LogInformation("Content Headers:");
        if (apiRequest.Content?.Headers != null)
        {
            foreach (var header in apiRequest.Content.Headers)
            {
                _logger.LogInformation("  {Key}: {Value}", header.Key, string.Join(", ", header.Value));
            }
        }
        _logger.LogInformation("Request Body: {Body}", await apiRequest.Content!.ReadAsStringAsync(cancellationToken));
        _logger.LogInformation("=== End Request Debug ===");

        try
        {
            using var response = await httpClient.SendAsync(apiRequest, cancellationToken);

            // Log response for debugging
            var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);

            _logger.LogInformation("=== Workflow Sync Response Debug ===");
            _logger.LogInformation("Response Status: {StatusCode} ({StatusCodeInt})", response.StatusCode, (int)response.StatusCode);
            _logger.LogInformation("Response Headers:");
            foreach (var header in response.Headers)
            {
                _logger.LogInformation("  {Key}: {Value}", header.Key, string.Join(", ", header.Value));
            }
            _logger.LogInformation("Response Content Headers:");
            if (response.Content?.Headers != null)
            {
                foreach (var header in response.Content.Headers)
                {
                    _logger.LogInformation("  {Key}: {Value}", header.Key, string.Join(", ", header.Value));
                }
            }
            _logger.LogInformation("Response Body: {Content}", responseContent);
            _logger.LogInformation("=== End Response Debug ===");

            if (string.IsNullOrWhiteSpace(responseContent) || responseContent.TrimStart().StartsWith('<'))
            {
                _logger.LogError("API returned HTML instead of JSON. Status: {StatusCode}, Content preview: {Preview}",
                    response.StatusCode, responseContent.Length > 200 ? responseContent.Substring(0, 200) : responseContent);
                return StatusCode(StatusCodes.Status502BadGateway, new
                {
                    Code = (int)HttpStatusCode.BadGateway,
                    Message = $"Backend returned HTML instead of JSON. Status: {response.StatusCode}"
                });
            }

            var jsonOptions = new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true };

            // Try parsing as real backend format first (has "success" field)
            if (responseContent.Contains("\"success\""))
            {
                var realBackendResponse = System.Text.Json.JsonSerializer.Deserialize<RealBackendApiResponse<WorkflowDiagramPage>>(
                    responseContent, jsonOptions);

                if (realBackendResponse is null)
                {
                    _logger.LogError("Failed to parse real backend response. Status Code: {StatusCode}", response.StatusCode);
                    return StatusCode(StatusCodes.Status502BadGateway, new
                    {
                        Code = (int)HttpStatusCode.BadGateway,
                        Message = "Failed to parse response from the backend."
                    });
                }

                if (!realBackendResponse.Success)
                {
                    var message = realBackendResponse.Message ?? realBackendResponse.Code ?? "Authentication failed";
                    _logger.LogWarning("Workflow sync failed. Status: {Status}, Code: {Code}, Message: {Message}",
                        response.StatusCode, realBackendResponse.Code, message);

                    return StatusCode((int)response.StatusCode, new
                    {
                        Code = realBackendResponse.Code ?? "ERROR",
                        Message = message
                    });
                }

                var workflows = realBackendResponse.Data?.Content;

                if (workflows is null || workflows.Count == 0)
                {
                    return Ok(new WorkflowSyncResultDto
                    {
                        Total = 0,
                        Inserted = 0,
                        Updated = 0
                    });
                }

                // Process workflows (code below)
                return await ProcessWorkflowsAsync(workflows, cancellationToken);
            }
            else
            {
                // Parse as simulator format (has "succ" field)
                var simulatorResponse = System.Text.Json.JsonSerializer.Deserialize<SimulatorApiResponse<WorkflowDiagramPage>>(
                    responseContent, jsonOptions);

                if (simulatorResponse is null)
                {
                    _logger.LogError("API simulator returned no content. Status Code: {StatusCode}", response.StatusCode);
                    return StatusCode(StatusCodes.Status502BadGateway, new
                    {
                        Code = (int)HttpStatusCode.BadGateway,
                        Message = "Failed to retrieve a response from the API simulator."
                    });
                }

                if (!response.IsSuccessStatusCode || simulatorResponse.Succ is not true)
                {
                    var message = simulatorResponse.Msg ?? "Failed to sync workflows from the API simulator.";
                    _logger.LogWarning("Workflow sync failed. Status: {Status}, Message: {Message}", response.StatusCode, message);

                    return StatusCode((int)response.StatusCode, new
                    {
                        Code = (int)response.StatusCode,
                        Message = message
                    });
                }

                var workflows = simulatorResponse.Data?.Content;

                if (workflows is null || workflows.Count == 0)
                {
                    return Ok(new WorkflowSyncResultDto
                    {
                        Total = 0,
                        Inserted = 0,
                        Updated = 0
                    });
                }

                // Process workflows (code below)
                return await ProcessWorkflowsAsync(workflows, cancellationToken);
            }
        }
        catch (HttpRequestException httpRequestException)
        {
            _logger.LogError(httpRequestException, "Error while calling API simulator at {BaseUrl}", requestUri);
            return StatusCode(StatusCodes.Status502BadGateway, new
            {
                Code = (int)HttpStatusCode.BadGateway,
                Message = "Unable to reach the API simulator."
            });
        }
    }

    private async Task<ActionResult<WorkflowSyncResultDto>> ProcessWorkflowsAsync(
        IEnumerable<WorkflowDiagramDto> workflows,
        CancellationToken cancellationToken)
    {
        var workflowList = workflows.ToList();
        var workflowCodes = workflowList.Select(w => w.WorkflowCode).Where(code => !string.IsNullOrWhiteSpace(code)).ToArray();
        var existingWorkflows = await _dbContext.WorkflowDiagrams
            .Where(w => workflowCodes.Contains(w.WorkflowCode))
            .ToDictionaryAsync(w => w.WorkflowCode, cancellationToken);

        var inserted = 0;
        var updated = 0;

        foreach (var workflow in workflowList)
        {
            if (string.IsNullOrWhiteSpace(workflow.WorkflowCode))
            {
                _logger.LogWarning("Skipped workflow with missing workflow code.");
                continue;
            }

            if (!existingWorkflows.TryGetValue(workflow.WorkflowCode, out var entity))
            {
                entity = new WorkflowDiagram
                {
                    WorkflowCode = workflow.WorkflowCode
                };
                _dbContext.WorkflowDiagrams.Add(entity);
                existingWorkflows[workflow.WorkflowCode] = entity;
                inserted++;
            }
            else
            {
                updated++;
            }

            entity.WorkflowOuterCode = workflow.WorkflowOuterCode ?? string.Empty;
            entity.WorkflowName = workflow.WorkflowName ?? string.Empty;
            entity.WorkflowModel = workflow.WorkflowModel;
            entity.RobotTypeClass = workflow.RobotTypeClass;
            entity.MapCode = workflow.MapCode ?? string.Empty;
            entity.ButtonName = workflow.ButtonName;
            entity.CreateUsername = workflow.CreateUsername ?? string.Empty;
            entity.CreateTime = ParseDateTime(workflow.CreateTime) ??
                                (entity.CreateTime == default ? DateTime.UtcNow : entity.CreateTime);
            entity.UpdateUsername = workflow.UpdateUsername ?? string.Empty;
            entity.UpdateTime = ParseDateTime(workflow.UpdateTime) ?? DateTime.UtcNow;
            entity.Status = workflow.Status;
            entity.NeedConfirm = workflow.NeedConfirm;
            entity.LockRobotAfterFinish = workflow.LockRobotAfterFinish;
            entity.WorkflowPriority = workflow.WorkflowPriority;
            entity.TargetAreaCode = workflow.TargetAreaCode;
            entity.PreSelectedRobotCellCode = workflow.PreSelectedRobotCellCode;
            entity.PreSelectedRobotId = workflow.PreSelectedRobotId;
        }

        await _dbContext.SaveChangesAsync(cancellationToken);

        return Ok(new WorkflowSyncResultDto
        {
            Total = workflowList.Count,
            Inserted = inserted,
            Updated = updated
        });
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<WorkflowSummaryDto>>> GetAsync(CancellationToken cancellationToken)
    {
        var workflows = await _dbContext.WorkflowDiagrams
            .AsNoTracking()
            .OrderBy(w => w.WorkflowName)
            .Select(w => new WorkflowSummaryDto
            {
                Id = w.Id,
                Name = w.WorkflowName,
                Number = w.WorkflowCode,
                ExternalCode = w.WorkflowCode,
                Status = w.Status,
                LayoutCode = w.MapCode,
                ActiveSchedulesCount = 0  // Workflow schedule functionality removed
            })
            .ToListAsync(cancellationToken);

        _logger.LogInformation("=== WorkflowsController.GetAsync DEBUG ===");
        _logger.LogInformation("Total workflows retrieved: {Count}", workflows.Count);
        foreach (var workflow in workflows.Take(5))
        {
            _logger.LogInformation("Workflow ID={Id}, Name={Name}, Number={Number}, ExternalCode={ExternalCode}",
                workflow.Id, workflow.Name, workflow.Number, workflow.ExternalCode);
        }
        if (workflows.Count > 5)
        {
            _logger.LogInformation("... and {More} more workflows", workflows.Count - 5);
        }
        _logger.LogInformation("=== END WorkflowsController.GetAsync DEBUG ===");

        return Ok(workflows);
    }

    private static DateTime? ParseDateTime(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        if (DateTime.TryParse(value, out var parsed))
        {
            return parsed;
        }

        return null;
    }
}
