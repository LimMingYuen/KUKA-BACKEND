using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace QES_KUKA_AMR_WEB.Pages;

[IgnoreAntiforgeryToken]
public class SavedCustomMissionsModel : PageModel
{
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfiguration _configuration;
        private readonly ILogger<SavedCustomMissionsModel> _logger;

        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNameCaseInsensitive = true,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };

        public SavedCustomMissionsModel(
            IHttpClientFactory httpClientFactory,
            IConfiguration configuration,
            ILogger<SavedCustomMissionsModel> logger)
        {
            _httpClientFactory = httpClientFactory;
            _configuration = configuration;
            _logger = logger;
        }

        public List<SavedMissionDto> SavedMissions { get; set; } = new();
        public string? SuccessMessage { get; set; }
        public string? ErrorMessage { get; set; }
        public int PollingIntervalSeconds => _configuration.GetValue<int>("JobStatusPolling:PollingIntervalSeconds", 5);
        public int MaxPollingAttempts => _configuration.GetValue<int>("JobStatusPolling:MaxPollingAttempts", 120);
        public bool RobotPositionPollingEnabled => _configuration.GetValue<bool>("RobotPositionPolling:Enabled", true);
        public List<MapZoneSummaryDto> MapZones { get; set; } = new();

        public async Task<IActionResult> OnGetAsync()
        {
            // Check if user is logged in
            var token = HttpContext.Session.GetString("JwtToken");
            if (string.IsNullOrEmpty(token))
            {
                return RedirectToPage("/Login");
            }

            await LoadSavedMissionsAsync(token);
            await LoadMapZonesAsync(token);

            return Page();
        }

        public async Task<IActionResult> OnPostTriggerAsync(int id)
        {
            var token = HttpContext.Session.GetString("JwtToken");
            if (string.IsNullOrEmpty(token))
            {
                return RedirectToPage("/Login");
            }

            try
            {
                var httpClient = _httpClientFactory.CreateClient();
                var apiBaseUrl = _configuration["ApiSettings:BaseUrl"] ?? "http://localhost:5000";
                var apiUrl = $"{apiBaseUrl}/api/saved-custom-missions/{id}/trigger";

                httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

                _logger.LogInformation("Triggering saved custom mission {Id}", id);

                var response = await httpClient.PostAsync(apiUrl, null);

                if (response.IsSuccessStatusCode)
                {
                    var responseContent = await response.Content.ReadAsStringAsync();
                    var result = JsonSerializer.Deserialize<ApiResponse<TriggerResponse>>(
                        responseContent,
                        new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                    _logger.LogInformation("Triggered mission successfully: {Response}", responseContent);

                    SuccessMessage = result?.Data?.Message ?? "Mission triggered successfully!";
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    _logger.LogError("Failed to trigger mission {Id}. Status: {StatusCode}, Error: {Error}",
                        id, response.StatusCode, errorContent);

                    ErrorMessage = $"Failed to trigger mission: {response.StatusCode}";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error triggering saved mission {Id}", id);
                ErrorMessage = $"An error occurred: {ex.Message}";
            }

            // Reload missions list
            await LoadSavedMissionsAsync(token);

            return Page();
        }

        public async Task<IActionResult> OnPostDeleteAsync(int id)
        {
            var token = HttpContext.Session.GetString("JwtToken");
            if (string.IsNullOrEmpty(token))
            {
                return RedirectToPage("/Login");
            }

            try
            {
                var httpClient = _httpClientFactory.CreateClient();
                var apiBaseUrl = _configuration["ApiSettings:BaseUrl"] ?? "http://localhost:5000";
                var apiUrl = $"{apiBaseUrl}/api/saved-custom-missions/{id}";

                httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

                _logger.LogInformation("Deleting saved custom mission {Id}", id);

                var response = await httpClient.DeleteAsync(apiUrl);

                if (response.IsSuccessStatusCode)
                {
                    _logger.LogInformation("Deleted mission {Id} successfully", id);
                    SuccessMessage = "Mission deleted successfully!";
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    _logger.LogError("Failed to delete mission {Id}. Status: {StatusCode}, Error: {Error}",
                        id, response.StatusCode, errorContent);

                    ErrorMessage = $"Failed to delete mission: {response.StatusCode}";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting saved mission {Id}", id);
                ErrorMessage = $"An error occurred: {ex.Message}";
            }

            // Reload missions list
            await LoadSavedMissionsAsync(token);

            return Page();
        }

        public async Task<IActionResult> OnPostTriggerAjaxAsync([FromBody] SavedMissionTriggerAjaxRequest request, CancellationToken cancellationToken)
        {
            _logger.LogInformation("=== OnPostTriggerAjaxAsync DEBUG ===");
            _logger.LogInformation("Triggering saved custom mission {Id}", request.SavedMissionId);

            var token = HttpContext.Session.GetString("JwtToken");
            if (string.IsNullOrEmpty(token))
            {
                return new JsonResult(new { success = false, message = "Session expired. Please login again." });
            }

            try
            {
                var httpClient = _httpClientFactory.CreateClient();
                var apiBaseUrl = _configuration["ApiSettings:BaseUrl"] ?? "http://localhost:5000";
                var apiUrl = $"{apiBaseUrl}/api/saved-custom-missions/{request.SavedMissionId}/trigger";

                httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

                _logger.LogInformation("Triggering saved custom mission {Id}", request.SavedMissionId);

                var response = await httpClient.PostAsync(apiUrl, null, cancellationToken);
                var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);

                if (response.IsSuccessStatusCode)
                {
                    var result = JsonSerializer.Deserialize<ApiResponse<TriggerResponse>>(responseContent, JsonOptions);

                    if (result?.Data != null)
                    {
                        _logger.LogInformation("Triggered mission successfully: MissionCode={MissionCode}", result.Data.MissionCode);

                        // Parse the message to determine if queued or executing
                        var isQueued = result.Data.Message.Contains("queued", StringComparison.OrdinalIgnoreCase);
                        var executeImmediately = result.Data.Message.Contains("started", StringComparison.OrdinalIgnoreCase);

                        return new JsonResult(new
                        {
                            success = true,
                            message = result.Data.Message,
                            missionCode = result.Data.MissionCode,
                            requestId = result.Data.RequestId,
                            executeImmediately = executeImmediately,
                            queued = isQueued
                        });
                    }
                }

                _logger.LogError("Failed to trigger mission {Id}. Status: {StatusCode}, Error: {Error}",
                    request.SavedMissionId, response.StatusCode, responseContent);

                return new JsonResult(new { success = false, message = $"Failed to trigger mission: {response.StatusCode}" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error triggering saved mission {Id}", request.SavedMissionId);
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

                    // Find queue item and mark as Cancelled
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

        public async Task<IActionResult> OnGetStatusAsync(string jobCode, int? savedMissionId, string? robotId, CancellationToken cancellationToken)
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
                        workflowName = queueItem.WorkflowName ?? "Custom Mission",
                        completeTime = completedAt,
                        spendTime = spendTimeSeconds,
                        robotPositionEnabled = RobotPositionPollingEnabled
                    });
                }

                return NotFound(new { success = false, message = "Job not found" });
            }

            var statusInfo = GetStatusDisplayInfo(job.Status);

                // Get robot position if enabled and robotId is available
                string? currentNodeCode = null;
                StepMatchResult? stepMatch = null;
                int? batteryLevel = null;

                if (RobotPositionPollingEnabled && !string.IsNullOrEmpty(job.RobotId))
                {
                    var robotData = await GetRobotCurrentNodeAsync(job.RobotId, token, cancellationToken);
                    currentNodeCode = robotData?.NodeCode;
                    batteryLevel = robotData?.BatteryLevel;

                    // Match nodeCode to mission step with enhanced logic
                    if (!string.IsNullOrEmpty(currentNodeCode) && savedMissionId.HasValue)
                    {
                        stepMatch = await GetCurrentStepMatchAsync(savedMissionId.Value, currentNodeCode, token, cancellationToken);
                    }
                }

                // Check for manual waypoint (consolidated from separate polling)
                WaitingMissionDto? waitingMission = null;
                try
                {
                    var apiBaseUrl = _configuration["ApiSettings:BaseUrl"] ?? "http://localhost:5000";
                    var apiUrl = $"{apiBaseUrl}/api/missions/waiting-for-resume";

                    _logger.LogDebug("Manual Waypoint Check Request - MissionCode: {MissionCode}, URL: {Url}",
                        jobCode, apiUrl);

                    using var waypointResponse = await client.GetAsync(apiUrl, cancellationToken);

                    if (waypointResponse.IsSuccessStatusCode)
                    {
                        var waypointContent = await waypointResponse.Content.ReadAsStringAsync(cancellationToken);

                        _logger.LogDebug("Manual Waypoint Check Response - MissionCode: {MissionCode}, Status: {StatusCode}, Body: {Body}",
                            jobCode, waypointResponse.StatusCode, waypointContent);

                        var allWaitingMissions = JsonSerializer.Deserialize<List<WaitingMissionDto>>(waypointContent, JsonOptions);
                        waitingMission = allWaitingMissions?.FirstOrDefault(m => m.MissionCode == jobCode);

                        if (waitingMission != null)
                        {
                            _logger.LogInformation("Mission {MissionCode} is waiting at manual waypoint: {Waypoint}",
                                jobCode, waitingMission.CurrentPosition);
                            // Use battery level from waiting mission if not already set
                            batteryLevel ??= waitingMission.BatteryLevel;
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Error checking manual waypoint for {MissionCode}", jobCode);
                }

                // Auto-save to mission history if terminal status
                if (statusInfo.IsTerminal)
                {
                    await SaveToMissionHistoryAsync(jobCode, job.WorkflowName ?? "Custom Mission", statusInfo.Name, token, cancellationToken);

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
                    spendTime = job.SpendTime,
                    batteryLevel = batteryLevel,

                    // Enhanced step tracking
                    currentStepIndex = stepMatch?.CurrentStepIndex,
                    currentStep = stepMatch?.CurrentStep,
                    matchType = stepMatch?.MatchType.ToString(),
                    matchConfidence = stepMatch?.Confidence,
                    isInArea = stepMatch?.IsInArea ?? false,

                    // Progress tracking
                    completedSteps = stepMatch?.CompletedSteps ?? new List<int>(),
                    totalSteps = stepMatch?.TotalSteps ?? 0,
                    progressPercentage = stepMatch?.ProgressPercentage ?? 0.0,

                    // Node tracking
                    currentNodeCode = currentNodeCode,
                    robotPositionEnabled = RobotPositionPollingEnabled,

                    // Manual waypoint tracking (consolidated)
                    isWaitingAtManualWaypoint = waitingMission != null,
                    manualWaypointData = waitingMission
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error querying job status for {JobCode}", jobCode);
                return StatusCode(500, new { success = false, message = "Error querying job status" });
            }
        }


        private async Task LoadSavedMissionsAsync(string token)
        {
            try
            {
                var httpClient = _httpClientFactory.CreateClient();
                var apiBaseUrl = _configuration["ApiSettings:BaseUrl"] ?? "http://localhost:5000";
                var apiUrl = $"{apiBaseUrl}/api/saved-custom-missions";

                httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

                var response = await httpClient.GetAsync(apiUrl);

                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    var result = JsonSerializer.Deserialize<ApiResponse<List<SavedMissionDto>>>(
                        json,
                        new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                    SavedMissions = result?.Data ?? new List<SavedMissionDto>();
                }
                else
                {
                    _logger.LogError("Failed to load saved missions. Status: {StatusCode}", response.StatusCode);
                    SavedMissions = new List<SavedMissionDto>();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading saved missions");
                SavedMissions = new List<SavedMissionDto>();
            }
        }

        public async Task<IActionResult> OnGetMissionSchedulesAsync(int missionId, CancellationToken cancellationToken)
        {
            var token = HttpContext.Session.GetString("JwtToken");
            if (string.IsNullOrEmpty(token))
            {
                return new JsonResult(new { success = false, message = "Session expired" }) { StatusCode = 401 };
            }

            try
            {
                using var client = CreateApiClient(token);
                var response = await client.GetAsync($"api/saved-custom-missions/{missionId}/schedules", cancellationToken);
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
                _logger.LogError(ex, "Error retrieving schedules for mission {MissionId}", missionId);
                return new JsonResult(new { success = false, message = "Error loading schedules." }) { StatusCode = 500 };
            }
        }

        public async Task<IActionResult> OnGetMissionScheduleLogsAsync(int missionId, int? scheduleId, int take, CancellationToken cancellationToken)
        {
            var token = HttpContext.Session.GetString("JwtToken");
            if (string.IsNullOrEmpty(token))
            {
                return new JsonResult(new { success = false, message = "Session expired" }) { StatusCode = 401 };
            }

            try
            {
                using var client = CreateApiClient(token);
                var url = $"api/saved-custom-missions/{missionId}/schedules/logs?take={take}";
                if (scheduleId.HasValue)
                {
                    url += $"&scheduleId={scheduleId.Value}";
                }

                var response = await client.GetAsync(url, cancellationToken);
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
                _logger.LogError(ex, "Error retrieving schedule logs for mission {MissionId}", missionId);
                return new JsonResult(new { success = false, message = "Error loading schedule logs." }) { StatusCode = 500 };
            }
        }

        public async Task<IActionResult> OnPostMissionScheduleUpsertAsync([FromBody] ScheduleUpsertRequest request, CancellationToken cancellationToken)
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
                    response = await client.PutAsync($"api/saved-custom-missions/{request.MissionId}/schedules/{request.ScheduleId.Value}", content, cancellationToken);
                }
                else
                {
                    response = await client.PostAsync($"api/saved-custom-missions/{request.MissionId}/schedules", content, cancellationToken);
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
                _logger.LogError(ex, "Error saving schedule for mission {MissionId}", request.MissionId);
                return new JsonResult(new { success = false, message = "Error saving schedule." }) { StatusCode = 500 };
            }
        }

        public async Task<IActionResult> OnPostMissionScheduleDeleteAsync([FromBody] ScheduleDeleteRequest request, CancellationToken cancellationToken)
        {
            var token = HttpContext.Session.GetString("JwtToken");
            if (string.IsNullOrEmpty(token))
            {
                return new JsonResult(new { success = false, message = "Session expired" }) { StatusCode = 401 };
            }

            try
            {
                using var client = CreateApiClient(token);
                var response = await client.DeleteAsync($"api/saved-custom-missions/{request.MissionId}/schedules/{request.ScheduleId}", cancellationToken);
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
                _logger.LogError(ex, "Error deleting schedule {ScheduleId} for mission {MissionId}", request.ScheduleId, request.MissionId);
                return new JsonResult(new { success = false, message = "Error deleting schedule." }) { StatusCode = 500 };
            }
        }

        public async Task<IActionResult> OnPostMissionScheduleRunAsync([FromBody] ScheduleRunRequest request, CancellationToken cancellationToken)
        {
            var token = HttpContext.Session.GetString("JwtToken");
            if (string.IsNullOrEmpty(token))
            {
                return new JsonResult(new { success = false, message = "Session expired" }) { StatusCode = 401 };
            }

            try
            {
                using var client = CreateApiClient(token);
                var response = await client.PostAsync($"api/saved-custom-missions/{request.MissionId}/schedules/{request.ScheduleId}/run-now", null, cancellationToken);
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
                _logger.LogError(ex, "Error running schedule {ScheduleId} for mission {MissionId}", request.ScheduleId, request.MissionId);
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

        private async Task LoadMapZonesAsync(string token)
        {
            try
            {
                var httpClient = CreateApiClient(token);
                var apiBaseUrl = _configuration["ApiSettings:BaseUrl"] ?? "http://localhost:5000";
                var apiUrl = $"{apiBaseUrl}/api/mapzones";

                var response = await httpClient.GetAsync(apiUrl);

                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    var result = JsonSerializer.Deserialize<List<MapZoneSummaryDto>>(
                        json,
                        new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                    if (result != null)
                    {
                        MapZones = result;
                    }
                }
                else
                {
                    _logger.LogError("Failed to load map zones. Status: {StatusCode}", response.StatusCode);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading map zones");
            }
        }

        private async Task<RobotDataDto?> GetRobotCurrentNodeAsync(string robotId, string token, CancellationToken cancellationToken)
        {
            try
            {
                using var client = CreateApiClient(token);
                var robotQueryRequest = new
                {
                    robotId = robotId,
                    mapCode = "",
                    floorNumber = ""
                };

                var content = new StringContent(
                    JsonSerializer.Serialize(robotQueryRequest, JsonOptions),
                    Encoding.UTF8,
                    "application/json");

                using var response = await client.PostAsync("api/robot-query", content, cancellationToken);

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning("Robot query failed for {RobotId}. Status: {StatusCode}", robotId, response.StatusCode);
                    return null;
                }

                var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);
                var robotQueryResponse = JsonSerializer.Deserialize<RobotQueryResponseDto>(responseContent, JsonOptions);

                if (robotQueryResponse?.Data != null && robotQueryResponse.Data.Count > 0)
                {
                    var robotData = robotQueryResponse.Data[0];
                    _logger.LogInformation("Robot {RobotId} at node: {NodeCode}", robotId, robotData.NodeCode);
                    return robotData;
                }

                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting robot position for {RobotId}", robotId);
                return null;
            }
        }

        private async Task<StepMatchResult> GetCurrentStepMatchAsync(int missionId, string nodeCode, string token, CancellationToken cancellationToken)
        {
            var matchResult = new StepMatchResult
            {
                NodeCode = nodeCode,
                MatchType = StepMatchType.None
            };

            try
            {
                // Load mission
                var mission = await LoadMissionAsync(missionId, token, cancellationToken);

                if (mission?.Steps == null || mission.Steps.Count == 0)
                {
                    return matchResult;
                }

                matchResult.TotalSteps = mission.Steps.Count;

                // 1. EXACT MATCH (highest priority)
                for (int i = 0; i < mission.Steps.Count; i++)
                {
                    if (string.Equals(mission.Steps[i].Position, nodeCode, StringComparison.OrdinalIgnoreCase))
                    {
                        _logger.LogInformation("Robot at step {Index} (Position: {Position}) - EXACT MATCH", i, mission.Steps[i].Position);
                        matchResult.CurrentStepIndex = i;
                        matchResult.MatchType = StepMatchType.Exact;
                        matchResult.CurrentStep = mission.Steps[i];
                        PopulateProgressData(matchResult, i, mission.Steps.Count);
                        return matchResult;
                    }
                }

                // 2. AREA MATCH (if nodeCode is within an area/aisle)
                for (int i = 0; i < mission.Steps.Count; i++)
                {
                    var step = mission.Steps[i];
                    if (IsNodeInArea(nodeCode, step.Position))
                    {
                        _logger.LogInformation("Robot at step {Index} (Position: {Position}, Area: {AreaCode}) - AREA MATCH", i, step.Position, step.Position);
                        matchResult.CurrentStepIndex = i;
                        matchResult.MatchType = StepMatchType.Area;
                        matchResult.CurrentStep = step;
                        matchResult.IsInArea = true;
                        PopulateProgressData(matchResult, i, mission.Steps.Count);
                        return matchResult;
                    }
                }

                // 3. FUZZY MATCH (similar positions)
                var fuzzyMatch = FindFuzzyMatch(nodeCode, mission.Steps);
                if (fuzzyMatch.HasValue)
                {
                    var match = fuzzyMatch.Value;
                    _logger.LogInformation("Robot at step {Index} (Position: {Position}) - FUZZY MATCH ({Confidence:P0})",
                        match.Index, match.Step.Position, match.Confidence);
                    matchResult.CurrentStepIndex = match.Index;
                    matchResult.MatchType = StepMatchType.Fuzzy;
                    matchResult.CurrentStep = match.Step;
                    matchResult.Confidence = match.Confidence;
                    PopulateProgressData(matchResult, match.Index, mission.Steps.Count);
                    return matchResult;
                }

                _logger.LogWarning("No step found matching nodeCode {NodeCode}", nodeCode);
                matchResult.CurrentStepIndex = null;
                matchResult.CurrentStep = null;
                PopulateProgressData(matchResult, -1, mission.Steps.Count);
                return matchResult;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error matching step for nodeCode {NodeCode}", nodeCode);
                return matchResult;
            }
        }

        private bool IsNodeInArea(string nodeCode, string areaCode)
        {
            try
            {
                // Check if areaCode is actually a map zone/area
                if (MapZones == null || !MapZones.Any())
                {
                    return false;
                }

                // Check if the position matches any zone code
                var zone = MapZones.FirstOrDefault(z =>
                    string.Equals(z.ZoneCode, areaCode, StringComparison.OrdinalIgnoreCase));

                if (zone == null)
                {
                    return false; // Not an area, just a node
                }

                // For now, if the nodeCode starts with the area code prefix, consider it in the area
                var areaPrefix = zone.ZoneCode;
                if (nodeCode.StartsWith(areaPrefix, StringComparison.OrdinalIgnoreCase))
                {
                    _logger.LogDebug("Node {NodeCode} is in area {AreaCode} (prefix match)", nodeCode, areaCode);
                    return true;
                }

                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking if node {NodeCode} is in area {AreaCode}", nodeCode, areaCode);
                return false;
            }
        }

        private (int Index, MissionStepDto Step, double Confidence)? FindFuzzyMatch(string nodeCode, List<MissionStepDto> steps)
        {
            double bestConfidence = 0;
            int bestIndex = -1;
            MissionStepDto? bestStep = null;

            foreach (var step in steps.Select((s, i) => new { Step = s, Index = i }))
            {
                var confidence = CalculateSimilarity(nodeCode, step.Step.Position);
                if (confidence > bestConfidence && confidence > 0.7) // Threshold for fuzzy matching
                {
                    bestConfidence = confidence;
                    bestIndex = step.Index;
                    bestStep = step.Step;
                }
            }

            if (bestIndex >= 0)
            {
                return (bestIndex, bestStep!, bestConfidence);
            }

            return null;
        }

        private double CalculateSimilarity(string a, string b)
        {
            if (string.Equals(a, b, StringComparison.OrdinalIgnoreCase))
                return 1.0;

            if (string.IsNullOrEmpty(a) || string.IsNullOrEmpty(b))
                return 0.0;

            // Simple Levenshtein-like similarity based on common prefix/suffix
            var minLength = Math.Min(a.Length, b.Length);
            var maxLength = Math.Max(a.Length, b.Length);

            var prefixMatch = 0;
            for (int i = 0; i < minLength; i++)
            {
                if (char.ToLower(a[i]) == char.ToLower(b[i]))
                    prefixMatch++;
                else
                    break;
            }

            var suffixMatch = 0;
            for (int i = 0; i < minLength; i++)
            {
                if (char.ToLower(a[a.Length - 1 - i]) == char.ToLower(b[b.Length - 1 - i]))
                    suffixMatch++;
                else
                    break;
            }

            // Weight: prefix is more important than suffix
            var similarity = (prefixMatch * 2 + suffixMatch) / (3.0 * maxLength);
            return similarity;
        }

        private void PopulateProgressData(StepMatchResult result, int currentStepIndex, int totalSteps)
        {
            if (currentStepIndex >= 0 && totalSteps > 0)
            {
                // All steps before current are considered completed
                result.CompletedSteps = Enumerable.Range(0, currentStepIndex).ToList();
                // Progress is based on position in the sequence (0-based index)
                result.ProgressPercentage = Math.Round((double)currentStepIndex / totalSteps * 100, 1);
            }
            else
            {
                result.CompletedSteps = new List<int>();
                result.ProgressPercentage = 0.0;
            }
        }

        private async Task<SavedMissionDetailsDto?> LoadMissionAsync(int id, string token, CancellationToken cancellationToken)
        {
            try
            {
                var httpClient = _httpClientFactory.CreateClient();
                var apiBaseUrl = _configuration["ApiSettings:BaseUrl"] ?? "http://localhost:5000";
                var apiUrl = $"{apiBaseUrl}/api/saved-custom-missions/{id}";

                httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

                var response = await httpClient.GetAsync(apiUrl, cancellationToken);

                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync(cancellationToken);
                    var result = JsonSerializer.Deserialize<ApiResponse<SavedMissionDto>>(
                        json,
                        new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                    if (result?.Data != null)
                    {
                        // Parse mission steps
                        var steps = new List<MissionStepDto>();
                        if (!string.IsNullOrEmpty(result.Data.MissionStepsJson))
                        {
                            steps = JsonSerializer.Deserialize<List<MissionStepDto>>(
                                result.Data.MissionStepsJson,
                                new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
                            ) ?? new List<MissionStepDto>();
                        }

                        return new SavedMissionDetailsDto
                        {
                            Id = result.Data.Id,
                            MissionName = result.Data.MissionName,
                            Description = result.Data.Description,
                            MissionType = result.Data.MissionType,
                            RobotType = result.Data.RobotType,
                            Priority = result.Data.Priority,
                            Steps = steps
                        };
                    }
                }
                else
                {
                    _logger.LogError("Failed to load mission {Id}. Status: {StatusCode}", id, response.StatusCode);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading mission {Id}", id);
            }

            return null;
        }

        public async Task<IActionResult> OnPostResumeManualWaypointAsync([FromBody] ResumeManualWaypointRequest request, CancellationToken cancellationToken)
        {
            var token = HttpContext.Session.GetString("JwtToken");
            if (string.IsNullOrEmpty(token))
            {
                return new JsonResult(new { success = false, message = "Session expired" }) { StatusCode = 401 };
            }

            try
            {
                using var httpClient = CreateApiClient(token);
                var apiBaseUrl = _configuration["ApiSettings:BaseUrl"] ?? "http://localhost:5000";
                var apiUrl = $"{apiBaseUrl}/api/missions/resume-manual-waypoint";

                _logger.LogInformation("Resuming manual waypoint for mission {MissionCode}", request.MissionCode);

                var jsonContent = new StringContent(
                    JsonSerializer.Serialize(request, JsonOptions),
                    Encoding.UTF8,
                    "application/json");

                var response = await httpClient.PostAsync(apiUrl, jsonContent, cancellationToken);
                var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);

                if (response.IsSuccessStatusCode)
                {
                    _logger.LogInformation("Mission resumed successfully: {MissionCode}", request.MissionCode);
                    return Content(responseContent, "application/json");
                }
                else
                {
                    _logger.LogError("Failed to resume mission {MissionCode}. Status: {StatusCode}, Response: {Response}",
                        request.MissionCode, response.StatusCode, responseContent);
                    return Content(responseContent, "application/json");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error resuming mission {MissionCode}", request.MissionCode);
                return new JsonResult(new { success = false, message = ex.Message });
            }
        }
    }

    public class SavedMissionDto
    {
        public int Id { get; set; }
        public string MissionName { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string MissionType { get; set; } = string.Empty;
        public string RobotType { get; set; } = string.Empty;
        public int Priority { get; set; }
        public string? RobotModels { get; set; }
        public string? RobotIds { get; set; }
        public string? ContainerModelCode { get; set; }
        public string? ContainerCode { get; set; }
        public string? IdleNode { get; set; }
        public string MissionStepsJson { get; set; } = string.Empty;
        public string CreatedBy { get; set; } = string.Empty;
        public DateTime CreatedUtc { get; set; }
        public DateTime UpdatedUtc { get; set; }
        public SavedMissionScheduleSummaryDto ScheduleSummary { get; set; } = new();

        // Computed property for display
        public int StepCount
        {
            get
            {
                try
                {
                    if (string.IsNullOrWhiteSpace(MissionStepsJson))
                        return 0;

                    var steps = JsonSerializer.Deserialize<List<object>>(MissionStepsJson);
                    return steps?.Count ?? 0;
                }
                catch
                {
                    return 0;
                }
            }
        }
    }

    public class TriggerResponse
    {
        public string MissionCode { get; set; } = string.Empty;
        public string RequestId { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
    }

    public class SavedMissionScheduleSummaryDto
    {
        public int TotalSchedules { get; set; }
        public int ActiveSchedules { get; set; }
        public DateTime? NextRunUtc { get; set; }
        public string? LastStatus { get; set; }
        public DateTime? LastRunUtc { get; set; }
    }

    public class SavedMissionScheduleDto
    {
        public int Id { get; set; }
        public int SavedMissionId { get; set; }
        public SavedMissionTriggerType TriggerType { get; set; }
        public string? CronExpression { get; set; }
        public DateTime? OneTimeRunUtc { get; set; }
        public string TimezoneId { get; set; } = "UTC";
        public bool IsEnabled { get; set; }
        public DateTime CreatedUtc { get; set; }
        public DateTime UpdatedUtc { get; set; }
        public DateTime? LastRunUtc { get; set; }
        public string? LastStatus { get; set; }
        public string? LastError { get; set; }
        public DateTime? NextRunUtc { get; set; }
    }

    public enum SavedMissionTriggerType
    {
        Once = 0,
        Recurring = 1
    }

    public class SavedMissionScheduleRequestDto
    {
        public SavedMissionTriggerType TriggerType { get; set; }
        public string? CronExpression { get; set; }
        public DateTime? RunAtLocalTime { get; set; }
        public string TimezoneId { get; set; } = "UTC";
        public bool IsEnabled { get; set; } = true;
    }

    public class SavedMissionScheduleLogDto
    {
        public int Id { get; set; }
        public int ScheduleId { get; set; }
        public DateTime ScheduledForUtc { get; set; }
        public DateTime? EnqueuedUtc { get; set; }
        public int? QueueId { get; set; }
        public string ResultStatus { get; set; } = "Pending";
        public string? Error { get; set; }
        public DateTime CreatedUtc { get; set; }
    }

    public class ScheduleUpsertRequest
    {
        public int MissionId { get; set; }
        public int? ScheduleId { get; set; }
        public SavedMissionScheduleRequestDto Schedule { get; set; } = new();
    }

    public class ScheduleDeleteRequest
    {
        public int MissionId { get; set; }
        public int ScheduleId { get; set; }
    }

    public class ScheduleRunRequest
    {
        public int MissionId { get; set; }
        public int ScheduleId { get; set; }
    }

    public class SavedMissionTriggerAjaxRequest
    {
        public int SavedMissionId { get; set; }
    }

    // DTOs for robot tracking and step matching
    public class RobotQueryResponseDto
    {
        public List<RobotDataDto>? Data { get; set; }
        public string Code { get; set; } = "0";
        public string? Message { get; set; }
        public bool Success { get; set; }
    }

    public class RobotDataDto
    {
        public string RobotId { get; set; } = string.Empty;
        public string? NodeCode { get; set; }
        public int? BatteryLevel { get; set; }
        public int Status { get; set; }
    }

    public class MapZoneSummaryDto
    {
        public string ZoneName { get; set; } = string.Empty;
        public string ZoneCode { get; set; } = string.Empty;
    }

    public class MissionStepDto
    {
        public int Sequence { get; set; }
        public string Type { get; set; } = string.Empty;
        public string Position { get; set; } = string.Empty;
        public bool PutDown { get; set; }
        public string PassStrategy { get; set; } = string.Empty;
        public int WaitingMillis { get; set; }
    }

    public class SavedMissionDetailsDto
    {
        public int Id { get; set; }
        public string MissionName { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string MissionType { get; set; } = string.Empty;
        public string RobotType { get; set; } = string.Empty;
        public int Priority { get; set; }
        public List<MissionStepDto> Steps { get; set; } = new();
    }

    public enum StepMatchType
    {
        None,
        Exact,
        Area,
        Fuzzy
    }

    public class StepMatchResult
    {
        public string? NodeCode { get; set; }
        public int? CurrentStepIndex { get; set; }
        public MissionStepDto? CurrentStep { get; set; }
        public StepMatchType MatchType { get; set; }
        public bool IsInArea { get; set; }
        public double? Confidence { get; set; }
        public List<int> CompletedSteps { get; set; } = new();
        public double ProgressPercentage { get; set; }
        public int TotalSteps { get; set; }
    }

    public class WaitingMissionDto
    {
        public string MissionCode { get; set; } = string.Empty;
        public string CurrentPosition { get; set; } = string.Empty;
        public string? RobotId { get; set; }
        public int? BatteryLevel { get; set; }
        public DateTime WaitingSince { get; set; }
    }

    public class ResumeManualWaypointRequest
    {
        public string MissionCode { get; set; } = string.Empty;
    }
