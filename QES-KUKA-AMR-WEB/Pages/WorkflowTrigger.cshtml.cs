using System.Net;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace QES_KUKA_AMR_WEB.Pages;

[IgnoreAntiforgeryToken]
public class WorkflowTriggerModel : PageModel
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IConfiguration _configuration;
    private readonly ILogger<WorkflowTriggerModel> _logger;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    public List<WorkflowSummary> Workflows { get; private set; } = new();

    public Dictionary<string, List<WorkflowSummary>> GroupedWorkflows { get; private set; } = new();

    public string? StatusMessage { get; private set; }

    public string StatusType { get; private set; } = "info";

    [BindProperty]
    public Dictionary<string, TriggerFormInput> FormValues { get; set; } = new();

    public int PollingIntervalSeconds => _configuration.GetValue<int>("JobStatusPolling:PollingIntervalSeconds", 5);
    public int MaxPollingAttempts => _configuration.GetValue<int>("JobStatusPolling:MaxPollingAttempts", 120);

    public WorkflowTriggerModel(
        IHttpClientFactory httpClientFactory,
        IConfiguration configuration,
        ILogger<WorkflowTriggerModel> logger)
    {
        _httpClientFactory = httpClientFactory;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<IActionResult> OnGetAsync(CancellationToken cancellationToken)
    {
        if (!await LoadWorkflowsAsync(cancellationToken))
        {
            return RedirectToPage("/Login");
        }

        return Page();
    }

    public async Task<IActionResult> OnPostTriggerAsync(int workflowId, CancellationToken cancellationToken)
    {
        if (!await LoadWorkflowsAsync(cancellationToken))
        {
            return RedirectToPage("/Login");
        }

        var workflow = Workflows.FirstOrDefault(w => w.Id == workflowId);
        if (workflow is null)
        {
            StatusMessage = "Selected workflow could not be found.";
            StatusType = "danger";
            return Page();
        }

        var identifier = GetFormIdentifier(workflow);
        if (!FormValues.TryGetValue(identifier, out var formInput))
        {
            formInput = BuildDefaultForm(workflow);
        }

        try
        {
            var queueResponse = await SubmitMissionAsync(workflow, formInput, cancellationToken);
            if (queueResponse.ExecuteImmediately)
            {
                StatusMessage = $"Workflow '{workflow.Name}' is executing now. Mission Code: {formInput.MissionCode}";
            }
            else
            {
                StatusMessage = $"Workflow '{workflow.Name}' queued at position {queueResponse.QueuePosition}. Mission Code: {formInput.MissionCode}";
            }
            StatusType = "success";
        }
        catch (HttpRequestException httpEx)
        {
            _logger.LogError(httpEx, "HTTP error triggering workflow {WorkflowId}", workflowId);
            StatusMessage = "Unable to reach mission service. Please try again.";
            StatusType = "danger";
        }
        catch (MissionTriggerException missionEx)
        {
            StatusMessage = missionEx.Message;
            StatusType = "warning";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error triggering workflow {WorkflowId}", workflowId);
            StatusMessage = "Unexpected error while triggering workflow.";
            StatusType = "danger";
        }

        return Page();
    }

    public async Task<IActionResult> OnPostTriggerAjaxAsync([FromBody] TriggerAjaxRequest request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("=== OnPostTriggerAjaxAsync DEBUG ===");
        _logger.LogInformation("Incoming AJAX request - WorkflowId={WorkflowId}, TemplateCode from JS={TemplateCode}",
            request.WorkflowId, request.TemplateCode);

        var token = HttpContext.Session.GetString("JwtToken");
        if (string.IsNullOrEmpty(token))
        {
            return new JsonResult(new { success = false, message = "Session expired. Please login again." });
        }

        try
        {
            // Load workflows to get workflow details
            if (!await LoadWorkflowsAsync(cancellationToken))
            {
                return new JsonResult(new { success = false, message = "Unable to load workflows." });
            }

            var workflow = Workflows.FirstOrDefault(w => w.Id == request.WorkflowId);
            if (workflow is null)
            {
                _logger.LogWarning("Workflow not found for ID={WorkflowId}", request.WorkflowId);
                return new JsonResult(new { success = false, message = "Workflow not found." });
            }

            _logger.LogInformation("Found workflow - Id={Id}, Name={Name}, ExternalCode={ExternalCode}",
                workflow.Id, workflow.Name, workflow.ExternalCode);

            var formInput = new TriggerFormInput
            {
                MissionCode = request.MissionCode,
                TemplateCode = request.TemplateCode?.Trim() ?? string.Empty,
                Priority = request.Priority,
                RequestId = request.RequestId
            };

            _logger.LogInformation("FormInput created - TemplateCode={TemplateCode}", formInput.TemplateCode);
            _logger.LogInformation("=== END OnPostTriggerAjaxAsync DEBUG ===");

            var queueResponse = await SubmitMissionAsync(workflow, formInput, cancellationToken);

            var message = queueResponse.ExecuteImmediately
                ? $"Workflow '{workflow.Name}' is executing now."
                : $"Workflow '{workflow.Name}' queued at position {queueResponse.QueuePosition}.";

            return new JsonResult(new
            {
                success = true,
                message = message,
                missionCode = formInput.MissionCode,
                workflowId = workflow.Id,
                executeImmediately = queueResponse.ExecuteImmediately,
                queued = !queueResponse.ExecuteImmediately,
                queuePosition = queueResponse.QueuePosition,
                queueId = queueResponse.QueueId
            });
        }
        catch (HttpRequestException)
        {
            return new JsonResult(new { success = false, message = "Unable to reach mission service." });
        }
        catch (MissionTriggerException missionEx)
        {
            return new JsonResult(new { success = false, message = missionEx.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error triggering workflow {WorkflowId}", request.WorkflowId);
            return new JsonResult(new { success = false, message = "Unexpected error occurred." });
        }
    }

    public async Task<IActionResult> OnPostCancelAjaxAsync([FromBody] CancelAjaxRequest request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("=== OnPostCancelAjaxAsync DEBUG ===");
        _logger.LogInformation("Received cancel request - MissionCode={MissionCode}, CancelMode={CancelMode}",
            request.MissionCode, request.CancelMode);

        var token = HttpContext.Session.GetString("JwtToken");
        if (string.IsNullOrEmpty(token))
        {
            return new JsonResult(new { success = false, message = "Session expired. Please login again." });
        }

        try
        {
            using var client = CreateApiClient(token);
            var cancelRequest = new
            {
                RequestId = $"cancel{DateTime.UtcNow:yyyyMMddHHmmss}",
                MissionCode = request.MissionCode,
                ContainerCode = "",
                Position = "",
                CancelMode = request.CancelMode,
                Reason = request.Reason ?? ""
            };

            var content = new StringContent(
                JsonSerializer.Serialize(cancelRequest, JsonOptions),
                Encoding.UTF8,
                "application/json");

            using var response = await client.PostAsync("api/missions/cancel", content, cancellationToken);
            var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);

            var cancelResponse = JsonSerializer.Deserialize<MissionCancelResponseDto>(responseContent, JsonOptions);

            if (cancelResponse?.Success == true)
            {
                _logger.LogInformation("✓ Mission {MissionCode} cancellation request sent to AMR successfully.", request.MissionCode);

                // Find queue item and mark as Cancelled (not Failed)
                var queueId = await GetQueueIdByMissionCodeAsync(request.MissionCode, token, cancellationToken);
                if (queueId.HasValue)
                {
                    using var cancelClient = CreateApiClient(token);
                    var cancelQueueResponse = await cancelClient.PostAsync($"api/mission-queue/{queueId.Value}/cancel", null, cancellationToken);

                    if (cancelQueueResponse.IsSuccessStatusCode)
                    {
                        _logger.LogInformation("✓ Queue item {QueueId} marked as Cancelled for mission {MissionCode}", queueId.Value, request.MissionCode);
                    }
                    else
                    {
                        _logger.LogWarning("Failed to mark queue item {QueueId} as Cancelled. Status: {StatusCode}",
                            queueId.Value, cancelQueueResponse.StatusCode);
                    }
                }
                else
                {
                    _logger.LogWarning("Could not find queue item for mission {MissionCode} to mark as Cancelled", request.MissionCode);
                }

                _logger.LogInformation("=== END OnPostCancelAjaxAsync DEBUG ===");

                return new JsonResult(new
                {
                    success = true,
                    message = $"Mission {request.MissionCode} cancelled successfully.",
                    missionCode = request.MissionCode
                });
            }
            else
            {
                _logger.LogWarning("✗ Mission cancellation failed: {Message}", cancelResponse?.Message);
                _logger.LogInformation("=== END OnPostCancelAjaxAsync DEBUG ===");

                return new JsonResult(new
                {
                    success = false,
                    message = cancelResponse?.Message ?? "Failed to cancel mission."
                });
            }
        }
        catch (HttpRequestException httpEx)
        {
            _logger.LogError(httpEx, "✗ HTTP error cancelling mission {MissionCode}", request.MissionCode);
            return new JsonResult(new { success = false, message = "Unable to reach mission service." });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "✗ Error cancelling mission {MissionCode}", request.MissionCode);
            return new JsonResult(new { success = false, message = "Unexpected error occurred." });
        }
    }


    public async Task<IActionResult> OnGetStatusAsync(string jobCode, CancellationToken cancellationToken)
    {
        var token = HttpContext.Session.GetString("JwtToken");
        if (string.IsNullOrEmpty(token))
        {
            return new JsonResult(new { success = false, message = "Session expired" }) { StatusCode = 401 };
        }

        try
        {
            using var client = CreateApiClient(token);
            var queryRequest = new { JobCode = jobCode, Limit = 1 };
            var content = new StringContent(
                JsonSerializer.Serialize(queryRequest, JsonOptions),
                Encoding.UTF8,
                "application/json");

            using var response = await client.PostAsync("api/missions/jobs/query", content, cancellationToken);
            var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);

            var jobQueryResponse = JsonSerializer.Deserialize<JobQueryResponseDto>(responseContent, JsonOptions);
            var job = jobQueryResponse?.Data?.FirstOrDefault();

            if (job == null)
            {
                var queueItem = await TryGetQueueItemByMissionCodeAsync(jobCode, token, cancellationToken);
                if (queueItem != null)
                {
                    var queueStatusInfo = GetQueueStatusDisplayInfo(queueItem.Status);
                    var spendTimeSeconds = CalculateSpendTimeSeconds(queueItem.ProcessedDate, queueItem.CompletedDate);
                    var completedAt = queueItem.CompletedDate?.ToString("yyyy-MM-dd HH:mm:ss");

                    return new JsonResult(new
                    {
                        success = true,
                        status = queueStatusInfo.StatusCode,
                        statusName = queueStatusInfo.Name,
                        cssClass = queueStatusInfo.CssClass,
                        isTerminal = queueStatusInfo.IsTerminal,
                        robotId = (string?)null,
                        workflowName = queueItem.WorkflowName ?? "Workflow",
                        completeTime = completedAt,
                        spendTime = spendTimeSeconds
                    });
                }

                return NotFound(new { success = false, message = "Job not found" });
            }

            var statusInfo = GetStatusDisplayInfo(job.Status);

            // Auto-save to mission history if terminal status
            if (statusInfo.IsTerminal)
            {
                await SaveToMissionHistoryAsync(jobCode, job.WorkflowName ?? "Unknown", statusInfo.Name, token, cancellationToken);

                // Mark mission as completed in queue
                var isSuccess = statusInfo.Name == "Complete" || statusInfo.Name == "Manual Complete";
                var errorMessage = isSuccess ? null : $"Mission ended with status: {statusInfo.Name}";
                await CompleteMissionInQueueAsync(jobCode, isSuccess, errorMessage, token, cancellationToken);
            }

            return new JsonResult(new
            {
                success = true,
                status = job.Status,
                statusName = statusInfo.Name,
                cssClass = statusInfo.CssClass,
                isTerminal = statusInfo.IsTerminal,
                robotId = job.RobotId,
                workflowName = job.WorkflowName,
                completeTime = job.CompleteTime,
                spendTime = job.SpendTime
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error querying job status for {JobCode}", jobCode);
            return StatusCode(500, new { success = false, message = "Error querying job status" });
        }
    }

    public async Task<IActionResult> OnGetActiveMissionAsync(int workflowId, CancellationToken cancellationToken)
    {
        var token = HttpContext.Session.GetString("JwtToken");
        if (string.IsNullOrEmpty(token))
        {
            return new JsonResult(new { success = false, message = "Session expired" }) { StatusCode = 401 };
        }

        try
        {
            using var client = CreateApiClient(token);
            var response = await client.GetAsync($"api/mission-queue/by-workflow/{workflowId}", cancellationToken);

            // 404 NotFound is expected when there's no active mission - not an error
            if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                return new JsonResult(new { success = false, message = "No active mission found" });
            }

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("API returned {StatusCode} for workflow {WorkflowId}", response.StatusCode, workflowId);
                return new JsonResult(new { success = false, message = "No active mission found" });
            }

            var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);
            _logger.LogDebug("API Response for workflow {WorkflowId}: {Response}", workflowId, responseContent);

            // The API returns a single object, not an array
            var activeMission = JsonSerializer.Deserialize<MissionQueueItemDto>(responseContent, JsonOptions);

            if (activeMission != null)
            {
                return new JsonResult(new
                {
                    success = true,
                    data = new
                    {
                        missionCode = activeMission.MissionCode,
                        status = activeMission.Status,
                        triggerSource = activeMission.TriggerSource,
                        triggerSourceName = activeMission.TriggerSourceName
                    }
                });
            }

            return new JsonResult(new { success = false, message = "No active mission found" });
        }
        catch (TaskCanceledException)
        {
            // Request was cancelled - likely due to page navigation or timeout. Don't log as error.
            return new JsonResult(new { success = false, message = "Request cancelled" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error querying active mission for workflow {WorkflowId}", workflowId);
            return new JsonResult(new { success = false, message = $"Error: {ex.Message}" }) { StatusCode = 500 };
        }
    }

    private async Task<JobDto?> QueryJobDetailsAsync(string jobCode, HttpClient client, CancellationToken cancellationToken)
    {
        try
        {
            var queryRequest = new { JobCode = jobCode, Limit = 1 };
            var content = new StringContent(
                JsonSerializer.Serialize(queryRequest, JsonOptions),
                Encoding.UTF8,
                "application/json");

            using var response = await client.PostAsync("api/missions/jobs/query", content, cancellationToken);
            var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);

            var jobQueryResponse = JsonSerializer.Deserialize<JobQueryResponseDto>(responseContent, JsonOptions);
            return jobQueryResponse?.Data?.FirstOrDefault();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error querying job details for {JobCode}", jobCode);
            return null;
        }
    }

    private async Task SaveToMissionHistoryAsync(string missionCode, string workflowName, string status, string token, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("=== SaveToMissionHistoryAsync DEBUG ===");
            _logger.LogInformation("Attempting to save mission history - MissionCode={MissionCode}, WorkflowName={WorkflowName}, Status={Status}",
                missionCode, workflowName, status);

            using var client = CreateApiClient(token);
            var historyRequest = new
            {
                MissionCode = missionCode,
                RequestId = $"request{DateTime.UtcNow:yyyyMMddHHmmss}",
                WorkflowName = workflowName,
                Status = status
            };

            var content = new StringContent(
                JsonSerializer.Serialize(historyRequest, JsonOptions),
                Encoding.UTF8,
                "application/json");

            _logger.LogInformation("Sending POST request to api/mission-history");

            var response = await client.PostAsync("api/mission-history", content, cancellationToken);

            _logger.LogInformation("Response status: {StatusCode}", response.StatusCode);

            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation("✓ Mission {MissionCode} saved to history successfully with status {Status}", missionCode, status);
            }
            else
            {
                var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogWarning("✗ Failed to save mission {MissionCode} to history. Status: {StatusCode}, Response: {Response}",
                    missionCode, response.StatusCode, errorContent);
            }

            _logger.LogInformation("=== END SaveToMissionHistoryAsync DEBUG ===");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "✗ Exception while saving mission {MissionCode} to history", missionCode);
        }
    }

    public string GetMissionCode(WorkflowSummary workflow)
    {
        FormValues ??= new Dictionary<string, TriggerFormInput>();

        var identifier = GetFormIdentifier(workflow);
        if (FormValues.TryGetValue(identifier, out var value) && !string.IsNullOrWhiteSpace(value.MissionCode))
        {
            return value.MissionCode;
        }

        return $"mission{DateTime.UtcNow:yyyyMMddHHmmss}";
    }

    public string GetTemplateCode(WorkflowSummary workflow)
    {
        FormValues ??= new Dictionary<string, TriggerFormInput>();

        var identifier = GetFormIdentifier(workflow);
        if (FormValues.TryGetValue(identifier, out var value) && !string.IsNullOrWhiteSpace(value.TemplateCode))
        {
            return value.TemplateCode;
        }

        return workflow.ExternalCode;
    }

    public int GetPriority(WorkflowSummary workflow)
    {
        FormValues ??= new Dictionary<string, TriggerFormInput>();

        var identifier = GetFormIdentifier(workflow);
        if (FormValues.TryGetValue(identifier, out var value) && value.Priority > 0)
        {
            return value.Priority;
        }

        return 1;
    }

    public string GetRequestId(WorkflowSummary workflow)
    {
        FormValues ??= new Dictionary<string, TriggerFormInput>();

        var identifier = GetFormIdentifier(workflow);
        if (FormValues.TryGetValue(identifier, out var value) && !string.IsNullOrWhiteSpace(value.RequestId))
        {
            return value.RequestId;
        }

        return $"request{DateTime.UtcNow:yyyyMMddHHmmss}";
    }

    private async Task<QueueEnqueueResponse> SubmitMissionAsync(WorkflowSummary workflow, TriggerFormInput formInput, CancellationToken cancellationToken)
    {
        var token = HttpContext.Session.GetString("JwtToken");
        if (string.IsNullOrEmpty(token))
        {
            throw new MissionTriggerException("Your session has expired. Please login again.");
        }

        using var client = CreateApiClient(token);
        var enqueueRequest = BuildEnqueueRequest(workflow, formInput);

        _logger.LogInformation(
            "Enqueueing mission {MissionCode} with template {TemplateCode} and priority {Priority}",
            enqueueRequest.MissionCode,
            enqueueRequest.TemplateCode,
            enqueueRequest.Priority);

        using var content = new StringContent(JsonSerializer.Serialize(enqueueRequest, JsonOptions), System.Text.Encoding.UTF8, "application/json");

        using var response = await client.PostAsync("api/mission-queue/enqueue", content, cancellationToken);
        var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            var message = ExtractErrorMessage(responseContent) ?? response.ReasonPhrase ?? "Failed to enqueue workflow.";
            throw new MissionTriggerException(message);
        }

        var queueResponse = JsonSerializer.Deserialize<QueueEnqueueResponse>(responseContent, JsonOptions);
        if (queueResponse is null || !queueResponse.Success)
        {
            var message = queueResponse?.Message ?? "Queue service returned an error.";
            throw new MissionTriggerException(message);
        }

        // If executing immediately, also submit to AMR system
        if (queueResponse.ExecuteImmediately)
        {
            var submitRequest = BuildSubmitMissionRequest(workflow, formInput);
            using var submitContent = new StringContent(JsonSerializer.Serialize(submitRequest, JsonOptions), System.Text.Encoding.UTF8, "application/json");
            using var submitResponse = await client.PostAsync("api/missions/submit", submitContent, cancellationToken);
            var submitResponseContent = await submitResponse.Content.ReadAsStringAsync(cancellationToken);

            if (!submitResponse.IsSuccessStatusCode)
            {
                // Mark mission as failed in queue
                await CompleteMissionInQueueAsync(formInput.MissionCode, false, "Failed to submit to AMR system", token, cancellationToken);
                var message = ExtractErrorMessage(submitResponseContent) ?? submitResponse.ReasonPhrase ?? "Failed to submit to AMR system.";
                throw new MissionTriggerException(message);
            }

            var missionResponse = JsonSerializer.Deserialize<MissionSubmitResponse>(submitResponseContent, JsonOptions);
            if (missionResponse is null || !missionResponse.Success)
            {
                // Mark mission as failed in queue
                await CompleteMissionInQueueAsync(formInput.MissionCode, false, missionResponse?.Message ?? "AMR system error", token, cancellationToken);
                var message = missionResponse?.Message ?? "Mission service returned an error.";
                throw new MissionTriggerException(message);
            }

            // Mark mission as submitted to AMR in queue
            if (queueResponse.QueueId.HasValue)
            {
                await MarkMissionAsSubmittedAsync(queueResponse.QueueId.Value, token, cancellationToken);
            }
        }

        // Prepare fresh defaults for the next trigger attempt, retaining user overrides where sensible
        var nextDefaults = BuildDefaultForm(workflow);
        nextDefaults.TemplateCode = enqueueRequest.TemplateCode;
        nextDefaults.Priority = formInput.Priority;
        FormValues[GetFormIdentifier(workflow)] = nextDefaults;

        return queueResponse;
    }

    private SubmitMissionRequest BuildSubmitMissionRequest(WorkflowSummary workflow, TriggerFormInput formInput)
    {
        _logger.LogInformation("=== BuildSubmitMissionRequest DEBUG ===");
        _logger.LogInformation("Input - formInput.TemplateCode={FormInputTemplateCode}, workflow.ExternalCode={WorkflowExternalCode}",
            formInput.TemplateCode, workflow.ExternalCode);

        var templateCode = string.IsNullOrWhiteSpace(formInput.TemplateCode)
            ? workflow.ExternalCode
            : formInput.TemplateCode;

        _logger.LogInformation("After logic - templateCode (before trim)={TemplateCode}", templateCode ?? "(null)");

        templateCode = templateCode?.Trim() ?? string.Empty;

        _logger.LogInformation("Final templateCode={TemplateCode}", templateCode);
        _logger.LogInformation("=== END BuildSubmitMissionRequest DEBUG ===");

        return new SubmitMissionRequest
        {
            OrgId = "UNIVERSAL",
            RequestId = formInput.RequestId,
            MissionCode = formInput.MissionCode,
            MissionType = "RACK_MOVE",
            ViewBoardType = "",
            RobotType = "LIFT",
            Priority = formInput.Priority,
            ContainerModelCode = string.Empty,
            ContainerCode = string.Empty,
            TemplateCode = templateCode,
            LockRobotAfterFinish = false,
            UnlockMissionCode = string.Empty,
            UnlockRobotId = string.Empty,
            IdleNode = string.Empty,
            RobotModels = Array.Empty<string>(),
            RobotIds = Array.Empty<string>()
        };
    }

    private QueueEnqueueRequest BuildEnqueueRequest(WorkflowSummary workflow, TriggerFormInput formInput)
    {
        var templateCode = string.IsNullOrWhiteSpace(formInput.TemplateCode)
            ? workflow.ExternalCode
            : formInput.TemplateCode;
        templateCode = templateCode?.Trim() ?? string.Empty;

        var username = HttpContext.Session.GetString("Username") ?? "Unknown";

        return new QueueEnqueueRequest
        {
            WorkflowId = workflow.Id,
            WorkflowCode = workflow.Number,
            WorkflowName = workflow.Name,
            MissionCode = formInput.MissionCode,
            TemplateCode = templateCode,
            Priority = formInput.Priority,
            RequestId = formInput.RequestId,
            CreatedBy = username
        };
    }

    private async Task CompleteMissionInQueueAsync(string missionCode, bool success, string? errorMessage, string token, CancellationToken cancellationToken)
    {
        try
        {
            using var client = CreateApiClient(token);
            var completeRequest = new
            {
                MissionCode = missionCode,
                Success = success,
                ErrorMessage = errorMessage
            };

            var content = new StringContent(
                JsonSerializer.Serialize(completeRequest, JsonOptions),
                Encoding.UTF8,
                "application/json");

            var response = await client.PostAsync("api/mission-queue/complete", content, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Failed to mark mission {MissionCode} as completed in queue. Status: {StatusCode}",
                    missionCode, response.StatusCode);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error completing mission {MissionCode} in queue", missionCode);
        }
    }

    private async Task MarkMissionAsSubmittedAsync(int queueId, string token, CancellationToken cancellationToken)
    {
        try
        {
            using var client = CreateApiClient(token);
            var response = await client.PutAsync($"api/mission-queue/{queueId}/mark-submitted", null, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Failed to mark queue item {QueueId} as submitted to AMR. Status: {StatusCode}",
                    queueId, response.StatusCode);
            }
            else
            {
                _logger.LogInformation("✓ Queue item {QueueId} marked as submitted to AMR", queueId);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error marking queue item {QueueId} as submitted to AMR", queueId);
        }
    }

    private async Task<int?> GetQueueIdByMissionCodeAsync(string missionCode, string token, CancellationToken cancellationToken)
    {
        try
        {
            using var client = CreateApiClient(token);
            var response = await client.GetAsync("api/mission-queue?status=1", cancellationToken);
            if (!response.IsSuccessStatusCode) return null;

            var content = await response.Content.ReadAsStringAsync(cancellationToken);
            var queueItems = JsonSerializer.Deserialize<List<QueueItemDto>>(content, JsonOptions);

            return queueItems?.FirstOrDefault(q => q.MissionCode == missionCode)?.Id;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error finding queue item for mission {MissionCode}", missionCode);
            return null;
        }
    }

    private async Task<QueueItemDto?> TryGetQueueItemByMissionCodeAsync(string missionCode, string token, CancellationToken cancellationToken)
    {
        try
        {
            using var client = CreateApiClient(token);
            using var response = await client.GetAsync($"api/mission-queue/by-mission/{Uri.EscapeDataString(missionCode)}", cancellationToken);

            if (response.StatusCode == HttpStatusCode.NotFound)
            {
                return null;
            }

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Failed to retrieve queue item for mission {MissionCode}. Status: {StatusCode}", missionCode, response.StatusCode);
                return null;
            }

            var content = await response.Content.ReadAsStringAsync(cancellationToken);
            return JsonSerializer.Deserialize<QueueItemDto>(content, JsonOptions);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogWarning(ex, "HTTP error retrieving queue item for mission {MissionCode}", missionCode);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving queue item for mission {MissionCode}", missionCode);
            return null;
        }
    }

    private (int StatusCode, string Name, string CssClass, bool IsTerminal) GetQueueStatusDisplayInfo(int queueStatus)
    {
        var statusCodes = _configuration.GetSection("JobStatusPolling:StatusCodes");

        return queueStatus switch
        {
            0 => (statusCodes.GetValue<int>("Waiting"), "Queued", "status-waiting", false),
            1 => (statusCodes.GetValue<int>("Executing"), "Processing", "status-executing", false),
            2 => (statusCodes.GetValue<int>("Complete"), "Complete", "status-complete", true),
            3 => (statusCodes.GetValue<int>("StartupError"), "Failed", "status-error", true),
            4 => (statusCodes.GetValue<int>("Cancelled"), "Cancelled", "status-cancelled", true),
            _ => (queueStatus, $"Unknown ({queueStatus})", "status-unknown", false)
        };
    }

    private static int? CalculateSpendTimeSeconds(DateTime? processedDate, DateTime? completedDate)
    {
        if (processedDate.HasValue && completedDate.HasValue && completedDate > processedDate)
        {
            return Math.Max(1, (int)(completedDate.Value - processedDate.Value).TotalSeconds);
        }

        return null;
    }

    private async Task<bool> LoadWorkflowsAsync(CancellationToken cancellationToken)
    {
        FormValues ??= new Dictionary<string, TriggerFormInput>();

        var token = HttpContext.Session.GetString("JwtToken");
        if (string.IsNullOrEmpty(token))
        {
            StatusMessage = "Please login to trigger workflows.";
            StatusType = "warning";
            return false;
        }

        try
        {
            using var client = CreateApiClient(token);
            using var response = await client.GetAsync("api/workflows", cancellationToken);

            if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
            {
                StatusMessage = "Session expired. Please login again.";
                StatusType = "warning";
                return false;
            }

            response.EnsureSuccessStatusCode();

            await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
            var workflows = await JsonSerializer.DeserializeAsync<List<WorkflowSummary>>(stream, JsonOptions, cancellationToken);
            Workflows = workflows ?? new List<WorkflowSummary>();

            _logger.LogInformation("=== LoadWorkflowsAsync DEBUG ===");
            _logger.LogInformation("Total workflows loaded from API: {Count}", Workflows.Count);
            foreach (var workflow in Workflows.Take(5))
            {
                _logger.LogInformation("Workflow ID={Id}, Name={Name}, Number={Number}, ExternalCode={ExternalCode}",
                    workflow.Id, workflow.Name, workflow.Number, workflow.ExternalCode);
            }
            if (Workflows.Count > 5)
            {
                _logger.LogInformation("... and {More} more workflows", Workflows.Count - 5);
            }
            _logger.LogInformation("=== END LoadWorkflowsAsync DEBUG ===");

            // Group workflows by LayoutCode and sort alphabetically
            GroupedWorkflows = Workflows
                .GroupBy(w => string.IsNullOrWhiteSpace(w.LayoutCode) ? "Uncategorized" : w.LayoutCode)
                .OrderBy(g => g.Key)
                .ToDictionary(g => g.Key, g => g.ToList());
        }
        catch (HttpRequestException httpEx)
        {
            _logger.LogError(httpEx, "Unable to load workflows.");
            StatusMessage = "Unable to load workflows from the API.";
            StatusType = "danger";
            Workflows = new List<WorkflowSummary>();
            GroupedWorkflows = new Dictionary<string, List<WorkflowSummary>>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error loading workflows.");
            StatusMessage = "Unexpected error while loading workflows.";
            StatusType = "danger";
            Workflows = new List<WorkflowSummary>();
            GroupedWorkflows = new Dictionary<string, List<WorkflowSummary>>();
        }

        // Prepopulate form values for already loaded workflows
        foreach (var workflow in Workflows)
        {
            var identifier = GetFormIdentifier(workflow);
            if (!FormValues.ContainsKey(identifier))
            {
                FormValues[identifier] = BuildDefaultForm(workflow);
            }
        }

        return true;
    }

    private TriggerFormInput BuildDefaultForm(WorkflowSummary workflow)
    {
        var timestamp = DateTime.UtcNow.ToString("yyyyMMddHHmmss");
        return new TriggerFormInput
        {
            MissionCode = $"mission{timestamp}",
            TemplateCode = (workflow.ExternalCode ?? string.Empty).Trim(),
            Priority = 1,
            RequestId = $"request{timestamp}"
        };
    }

    // Workflow Schedule Management Handlers
    public async Task<IActionResult> OnGetWorkflowSchedulesAsync(int workflowId, CancellationToken cancellationToken)
    {
        var token = HttpContext.Session.GetString("JwtToken");
        if (string.IsNullOrEmpty(token))
        {
            return new JsonResult(new { success = false, message = "Session expired" }) { StatusCode = 401 };
        }

        try
        {
            using var client = CreateApiClient(token);
            var response = await client.GetAsync($"api/workflows/{workflowId}/schedules", cancellationToken);
            var content = await response.Content.ReadAsStringAsync(cancellationToken);
            return new ContentResult
            {
                StatusCode = (int)response.StatusCode,
                Content = content,
                ContentType = "application/json"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving schedules for workflow {WorkflowId}", workflowId);
            return new JsonResult(new { success = false, message = "Error loading schedules." }) { StatusCode = 500 };
        }
    }

    // Removed: Execution History UI section removed
    // public async Task<IActionResult> OnGetWorkflowScheduleLogsAsync(int workflowId, int? scheduleId, int take, CancellationToken cancellationToken)
    // {
    //     var token = HttpContext.Session.GetString("JwtToken");
    //     if (string.IsNullOrEmpty(token))
    //     {
    //         return new JsonResult(new { success = false, message = "Session expired" }) { StatusCode = 401 };
    //     }
    //
    //     try
    //     {
    //         using var client = CreateApiClient(token);
    //         var url = $"api/workflows/{workflowId}/schedules/logs?take={take}";
    //         if (scheduleId.HasValue)
    //         {
    //             url += $"&scheduleId={scheduleId.Value}";
    //         }
    //
    //         var response = await client.GetAsync(url, cancellationToken);
    //         var content = await response.Content.ReadAsStringAsync(cancellationToken);
    //
    //         return new ContentResult
    //         {
    //             StatusCode = (int)response.StatusCode,
    //             Content = content,
    //             ContentType = "application/json"
    //         };
    //     }
    //     catch (Exception ex)
    //     {
    //         _logger.LogError(ex, "Error retrieving schedule logs for workflow {WorkflowId}", workflowId);
    //         return new JsonResult(new { success = false, message = "Error loading schedule logs." }) { StatusCode = 500 };
    //     }
    // }

    public async Task<IActionResult> OnPostWorkflowScheduleUpsertAsync([FromBody] WorkflowScheduleUpsertRequest request, CancellationToken cancellationToken)
    {
        var token = HttpContext.Session.GetString("JwtToken");
        if (string.IsNullOrEmpty(token))
        {
            return new JsonResult(new { success = false, message = "Session expired" }) { StatusCode = 401 };
        }

        try
        {
            using var client = CreateApiClient(token);
            HttpResponseMessage response;
            var payload = JsonSerializer.Serialize(request.Schedule, JsonOptions);
            var content = new StringContent(payload, Encoding.UTF8, "application/json");

            if (request.ScheduleId.HasValue)
            {
                response = await client.PutAsync($"api/workflows/{request.WorkflowId}/schedules/{request.ScheduleId.Value}", content, cancellationToken);
            }
            else
            {
                response = await client.PostAsync($"api/workflows/{request.WorkflowId}/schedules", content, cancellationToken);
            }

            var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);

            return new ContentResult
            {
                StatusCode = (int)response.StatusCode,
                Content = responseContent,
                ContentType = "application/json"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving schedule for workflow {WorkflowId}", request.WorkflowId);
            return new JsonResult(new { success = false, message = "Error saving schedule." }) { StatusCode = 500 };
        }
    }

    public async Task<IActionResult> OnPostWorkflowScheduleDeleteAsync([FromBody] WorkflowScheduleDeleteRequest request, CancellationToken cancellationToken)
    {
        var token = HttpContext.Session.GetString("JwtToken");
        if (string.IsNullOrEmpty(token))
        {
            return new JsonResult(new { success = false, message = "Session expired" }) { StatusCode = 401 };
        }

        try
        {
            using var client = CreateApiClient(token);
            var response = await client.DeleteAsync($"api/workflows/{request.WorkflowId}/schedules/{request.ScheduleId}", cancellationToken);
            var content = await response.Content.ReadAsStringAsync(cancellationToken);

            return new ContentResult
            {
                StatusCode = (int)response.StatusCode,
                Content = content,
                ContentType = "application/json"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting schedule {ScheduleId} for workflow {WorkflowId}", request.ScheduleId, request.WorkflowId);
            return new JsonResult(new { success = false, message = "Error deleting schedule." }) { StatusCode = 500 };
        }
    }

    public async Task<IActionResult> OnPostWorkflowScheduleRunAsync([FromBody] WorkflowScheduleRunRequest request, CancellationToken cancellationToken)
    {
        var token = HttpContext.Session.GetString("JwtToken");
        if (string.IsNullOrEmpty(token))
        {
            return new JsonResult(new { success = false, message = "Session expired" }) { StatusCode = 401 };
        }

        try
        {
            using var client = CreateApiClient(token);
            var response = await client.PostAsync($"api/workflows/{request.WorkflowId}/schedules/{request.ScheduleId}/run-now", null, cancellationToken);
            var content = await response.Content.ReadAsStringAsync(cancellationToken);

            return new ContentResult
            {
                StatusCode = (int)response.StatusCode,
                Content = content,
                ContentType = "application/json"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error running schedule {ScheduleId} for workflow {WorkflowId}", request.ScheduleId, request.WorkflowId);
            return new JsonResult(new { success = false, message = "Error running schedule." }) { StatusCode = 500 };
        }
    }

    private HttpClient CreateApiClient(string token)
    {
        var apiBaseUrl = _configuration["ApiSettings:BaseUrl"] ?? "https://localhost:7255";
        var client = _httpClientFactory.CreateClient();
        client.BaseAddress = new Uri(apiBaseUrl);
        client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
        return client;
    }

    private static string ExtractErrorMessage(string responseContent)
    {
        try
        {
            var error = JsonSerializer.Deserialize<ApiError>(responseContent, JsonOptions);
            return error?.Message ?? error?.Msg ?? string.Empty;
        }
        catch
        {
            return string.Empty;
        }
    }

    private (string Name, string CssClass, bool IsTerminal) GetStatusDisplayInfo(int status)
    {
        var config = _configuration.GetSection("JobStatusPolling:StatusCodes");

        if (status == config.GetValue<int>("Created"))
            return ("Created", "status-created", false);
        if (status == config.GetValue<int>("Executing"))
            return ("Executing", "status-executing", false);
        if (status == config.GetValue<int>("Waiting"))
            return ("Waiting", "status-waiting", false);
        if (status == config.GetValue<int>("Cancelling"))
            return ("Cancelling", "status-cancelling", false);
        if (status == config.GetValue<int>("Complete"))
            return ("Complete", "status-complete", true);
        if (status == config.GetValue<int>("Cancelled"))
            return ("Cancelled", "status-cancelled", true);
        if (status == config.GetValue<int>("ManualComplete"))
            return ("Manual Complete", "status-complete", true);
        if (status == config.GetValue<int>("Warning"))
            return ("Warning", "status-warning", false);
        if (status == config.GetValue<int>("StartupError"))
            return ("Startup Error", "status-error", true);

        return ($"Unknown ({status})", "status-unknown", false);
    }

    private static string GetFormIdentifier(WorkflowSummary workflow) => $"workflow_{workflow.Id}";

    public static string GetLayoutPanelId(string layoutCode) => $"layout-{layoutCode.Replace(" ", "-").ToLower()}";
}

// DTO for job query response
public class JobQueryResponseDto
{
    public List<JobDto>? Data { get; set; }
    public string Code { get; set; } = "0";
    public string? Message { get; set; }
    public bool Success { get; set; } = true;
}

public class JobDto
{
    public string JobCode { get; set; } = string.Empty;
    public long? WorkflowId { get; set; }
    public string? ContainerCode { get; set; }
    public string? RobotId { get; set; }
    public int Status { get; set; }
    public string? WorkflowName { get; set; }
    public string? WorkflowCode { get; set; }
    public int? WorkflowPriority { get; set; }
    public string? MapCode { get; set; }
    public string? CompleteTime { get; set; }
    public int? SpendTime { get; set; }
    public string? CreateUsername { get; set; }
    public string CreateTime { get; set; } = string.Empty;
}


public record WorkflowSummary
{
    public int Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string Number { get; init; } = string.Empty;
    public string ExternalCode { get; init; } = string.Empty;
    public int Status { get; init; }
    public string LayoutCode { get; init; } = string.Empty;
    public int ActiveSchedulesCount { get; init; }
}

public class TriggerFormInput
{
    public string MissionCode { get; set; } = string.Empty;
    public string TemplateCode { get; set; } = string.Empty;
    public int Priority { get; set; } = 1;
    public string RequestId { get; set; } = string.Empty;
}

public class SubmitMissionRequest
{
    public string OrgId { get; set; } = string.Empty;
    public string RequestId { get; set; } = string.Empty;
    public string MissionCode { get; set; } = string.Empty;
    public string MissionType { get; set; } = string.Empty;
    public string ViewBoardType { get; set; } = string.Empty;
    public IReadOnlyList<string> RobotModels { get; set; } = Array.Empty<string>();
    public IReadOnlyList<string> RobotIds { get; set; } = Array.Empty<string>();
    public string RobotType { get; set; } = string.Empty;
    public int Priority { get; set; }
    public string ContainerModelCode { get; set; } = string.Empty;
    public string ContainerCode { get; set; } = string.Empty;
    public string TemplateCode { get; set; } = string.Empty;
    public bool LockRobotAfterFinish { get; set; }
    public string UnlockRobotId { get; set; } = string.Empty;
    public string UnlockMissionCode { get; set; } = string.Empty;
    public string IdleNode { get; set; } = string.Empty;
}

public class MissionSubmitResponse
{
    public object? Data { get; set; }
    public string Code { get; set; } = "0";
    public string? Message { get; set; }
    public bool Success { get; set; }
}

public class ApiError
{
    public string? Message { get; set; }
    public string? Msg { get; set; }
}

public class MissionTriggerException : Exception
{
    public MissionTriggerException(string message) : base(message)
    {
    }
}

public class TriggerAjaxRequest
{
    public int WorkflowId { get; set; }
    public string MissionCode { get; set; } = string.Empty;
    public string TemplateCode { get; set; } = string.Empty;
    public int Priority { get; set; }
    public string RequestId { get; set; } = string.Empty;
}

public class CancelAjaxRequest
{
    public string MissionCode { get; set; } = string.Empty;
    public string CancelMode { get; set; } = "NORMAL";
    public string? Reason { get; set; }
}

public class MissionCancelResponseDto
{
    public object? Data { get; set; }
    public string Code { get; set; } = "0";
    public string? Message { get; set; }
    public bool Success { get; set; }
}

public class QueueEnqueueRequest
{
    public int WorkflowId { get; set; }
    public string WorkflowCode { get; set; } = string.Empty;
    public string WorkflowName { get; set; } = string.Empty;
    public string MissionCode { get; set; } = string.Empty;
    public string TemplateCode { get; set; } = string.Empty;
    public int Priority { get; set; }
    public string RequestId { get; set; } = string.Empty;
    public string CreatedBy { get; set; } = string.Empty;
}

public class QueueEnqueueResponse
{
    public bool Success { get; set; }
    public bool ExecuteImmediately { get; set; }
    public string Message { get; set; } = string.Empty;
    public int? QueuePosition { get; set; }
    public int? QueueId { get; set; }
}

// Workflow Schedule Request Models
public class WorkflowScheduleUpsertRequest
{
    public int WorkflowId { get; set; }
    public int? ScheduleId { get; set; }
    public WorkflowScheduleRequestDto Schedule { get; set; } = new();
}

public class WorkflowScheduleDeleteRequest
{
    public int WorkflowId { get; set; }
    public int ScheduleId { get; set; }
}

public class WorkflowScheduleRunRequest
{
    public int WorkflowId { get; set; }
    public int ScheduleId { get; set; }
}

public class WorkflowScheduleRequestDto
{
    public int TriggerType { get; set; } = 1; // 0 = Once, 1 = Recurring
    public string? CronExpression { get; set; }
    public DateTime? RunAtLocalTime { get; set; }
    public string TimezoneId { get; set; } = "UTC";
    public bool IsEnabled { get; set; } = true;
}

public class MissionQueueItemDto
{
    public int Id { get; set; }
    public string MissionCode { get; set; } = string.Empty;
    public int Status { get; set; }
    public int TriggerSource { get; set; }
    public string? TriggerSourceName { get; set; }
}

