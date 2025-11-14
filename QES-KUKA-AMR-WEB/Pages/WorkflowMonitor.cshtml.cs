using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace QES_KUKA_AMR_WEB.Pages;

[IgnoreAntiforgeryToken]
public class WorkflowMonitorModel : PageModel
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IConfiguration _configuration;
    private readonly ILogger<WorkflowMonitorModel> _logger;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    public WorkflowMonitorModel(
        IHttpClientFactory httpClientFactory,
        IConfiguration configuration,
        ILogger<WorkflowMonitorModel> logger)
    {
        _httpClientFactory = httpClientFactory;
        _configuration = configuration;
        _logger = logger;
    }

    public WorkflowMissionDto? Mission { get; set; }
    public List<WorkflowMonitorMapZoneSummaryDto> MapZones { get; set; } = new();
    public string? ErrorMessage { get; set; }
    public int PollingIntervalSeconds => _configuration.GetValue<int>("JobStatusPolling:PollingIntervalSeconds", 5);
    public bool RobotPositionPollingEnabled => _configuration.GetValue<bool>("RobotPositionPolling:Enabled", true);

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
                    // Convert from MapZoneSummaryDto to WorkflowMonitorMapZoneSummaryDto
                    MapZones = result.Select(dto => new WorkflowMonitorMapZoneSummaryDto
                    {
                        ZoneName = dto.ZoneName,
                        ZoneCode = dto.ZoneCode
                    }).ToList();
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

    public async Task<IActionResult> OnGetAsync(int id)
    {
        var token = HttpContext.Session.GetString("JwtToken");
        if (string.IsNullOrEmpty(token))
        {
            return RedirectToPage("/Login");
        }

        await LoadMissionAsync(id, token);
        await LoadMapZonesAsync(token);

        if (Mission == null)
        {
            ErrorMessage = "Mission not found";
        }

        return Page();
    }

    public async Task<IActionResult> OnPostStartAsync([FromBody] StartMissionRequest request)
    {
        var token = HttpContext.Session.GetString("JwtToken");
        if (string.IsNullOrEmpty(token))
        {
            return new JsonResult(new { success = false, message = "Session expired" });
        }

        try
        {
            var httpClient = _httpClientFactory.CreateClient();
            var apiBaseUrl = _configuration["ApiSettings:BaseUrl"] ?? "http://localhost:5000";
            var apiUrl = $"{apiBaseUrl}/api/saved-custom-missions/{request.SavedMissionId}/trigger";

            httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            _logger.LogInformation("Starting workflow mission {Id}", request.SavedMissionId);

            var response = await httpClient.PostAsync(apiUrl, null);
            var responseContent = await response.Content.ReadAsStringAsync();

            if (response.IsSuccessStatusCode)
            {
                var result = JsonSerializer.Deserialize<ApiResponse<TriggerResponse>>(responseContent, JsonOptions);

                if (result?.Data != null)
                {
                    _logger.LogInformation("Started mission successfully: MissionCode={MissionCode}", result.Data.MissionCode);

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

            _logger.LogError("Failed to start mission {Id}. Status: {StatusCode}", request.SavedMissionId, response.StatusCode);

            return new JsonResult(new { success = false, message = $"Failed to start mission: {response.StatusCode}" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error starting workflow mission {Id}", request.SavedMissionId);
            return new JsonResult(new { success = false, message = "Unexpected error occurred." });
        }
    }

    public async Task<IActionResult> OnPostCancelAsync([FromBody] CancelMissionRequest request)
    {
        var token = HttpContext.Session.GetString("JwtToken");
        if (string.IsNullOrEmpty(token))
        {
            return new JsonResult(new { success = false, message = "Session expired" });
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
                CancelMode = request.CancelMode ?? "NORMAL",
                Reason = request.Reason ?? ""
            };

            var content = new StringContent(
                JsonSerializer.Serialize(cancelRequest, JsonOptions),
                Encoding.UTF8,
                "application/json");

            using var response = await client.PostAsync("api/missions/cancel", content);
            var responseContent = await response.Content.ReadAsStringAsync();

            var cancelResponse = JsonSerializer.Deserialize<MissionCancelResponseDto>(responseContent, JsonOptions);

            if (cancelResponse?.Success == true)
            {
                _logger.LogInformation("Mission {MissionCode} cancellation request sent successfully.", request.MissionCode);

                return new JsonResult(new
                {
                    success = true,
                    message = $"Mission {request.MissionCode} cancelled successfully.",
                    missionCode = request.MissionCode
                });
            }
            else
            {
                _logger.LogWarning("Mission cancellation failed: {Message}", cancelResponse?.Message);
                return new JsonResult(new
                {
                    success = false,
                    message = cancelResponse?.Message ?? "Failed to cancel mission."
                });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error cancelling mission {MissionCode}", request.MissionCode);
            return new JsonResult(new { success = false, message = "Unexpected error occurred." });
        }
    }

    public async Task<IActionResult> OnGetStatusAsync(int id, string? missionCode, string? robotId)
    {
        var token = HttpContext.Session.GetString("JwtToken");
        if (string.IsNullOrEmpty(token))
        {
            return new JsonResult(new { success = false, message = "Session expired" }) { StatusCode = 401 };
        }

        // If missionCode is not provided, this is the first call - return initial state
        if (string.IsNullOrEmpty(missionCode))
        {
            return new JsonResult(new
            {
                success = true,
                status = "idle",
                statusName = "Idle",
                cssClass = "status-idle",
                isTerminal = false,
                currentStepIndex = (int?)null,
                currentNodeCode = (string?)null,
                completedSteps = new List<int>(),
                totalSteps = 0,
                missionCode = (string?)null,
                robotPositionEnabled = RobotPositionPollingEnabled
            });
        }

        try
        {
            using var client = CreateApiClient(token);
            var queryRequest = new { JobCode = missionCode, Limit = 1 };
            var requestBody = JsonSerializer.Serialize(queryRequest, JsonOptions);
            var content = new StringContent(
                requestBody,
                Encoding.UTF8,
                "application/json");

            _logger.LogDebug("Status Polling Request - MissionCode: {MissionCode}, URL: api/missions/jobs/query, Body: {Body}",
                missionCode, requestBody);

            using var response = await client.PostAsync("api/missions/jobs/query", content);
            var responseContent = await response.Content.ReadAsStringAsync();

            _logger.LogDebug("Status Polling Response - MissionCode: {MissionCode}, Status: {StatusCode}, Body: {Body}",
                missionCode, response.StatusCode, responseContent);

            var jobQueryResponse = JsonSerializer.Deserialize<JobQueryResponseDto>(responseContent, JsonOptions);
            var job = jobQueryResponse?.Data?.FirstOrDefault();

            if (job == null)
            {
                return new JsonResult(new
                {
                    success = false,
                    message = "Job not found",
                    status = "not_found"
                });
            }

            var statusInfo = GetStatusDisplayInfo(job.Status);

            // Get robot position if enabled and robotId is available
            string? currentNodeCode = null;
            StepMatchResult? stepMatch = null;

            if (RobotPositionPollingEnabled && !string.IsNullOrEmpty(robotId))
            {
                currentNodeCode = await GetRobotCurrentNodeAsync(robotId, token);

                // Match nodeCode to mission step with enhanced logic
                if (!string.IsNullOrEmpty(currentNodeCode))
                {
                    stepMatch = await GetCurrentStepMatchAsync(id, currentNodeCode, token);
                }
            }

            // Check for manual waypoint (consolidated from separate polling)
            WaitingMissionDto? waitingMission = null;
            try
            {
                var apiBaseUrl = _configuration["ApiSettings:BaseUrl"] ?? "http://localhost:5000";
                var apiUrl = $"{apiBaseUrl}/api/missions/waiting-for-resume";

                _logger.LogDebug("Manual Waypoint Check Request - MissionCode: {MissionCode}, URL: {Url}",
                    missionCode, apiUrl);

                using var waypointResponse = await client.GetAsync(apiUrl);

                if (waypointResponse.IsSuccessStatusCode)
                {
                    var waypointContent = await waypointResponse.Content.ReadAsStringAsync();

                    _logger.LogDebug("Manual Waypoint Check Response - MissionCode: {MissionCode}, Status: {StatusCode}, Body: {Body}",
                        missionCode, waypointResponse.StatusCode, waypointContent);

                    var allWaitingMissions = JsonSerializer.Deserialize<List<WaitingMissionDto>>(waypointContent, JsonOptions);
                    waitingMission = allWaitingMissions?.FirstOrDefault(m => m.MissionCode == missionCode);

                    if (waitingMission != null)
                    {
                        _logger.LogInformation("Mission {MissionCode} is waiting at manual waypoint: {Waypoint}",
                            missionCode, waitingMission.CurrentPosition);
                    }
                }
                else
                {
                    _logger.LogWarning("Manual waypoint check returned {StatusCode} for {MissionCode}",
                        waypointResponse.StatusCode, missionCode);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error checking manual waypoint for {MissionCode}", missionCode);
            }

            // Get battery level from robot query if available
            int? batteryLevel = null;
            if (RobotPositionPollingEnabled && !string.IsNullOrEmpty(robotId))
            {
                try
                {
                    var robotQuery = await GetRobotCurrentNodeAsync(robotId, token);
                    // Battery level will be extracted from robot query response
                    // For now, we'll get it from waiting mission if available
                    batteryLevel = waitingMission?.BatteryLevel;
                }
                catch
                {
                    // Battery level not available
                }
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
                totalSteps = Mission?.Steps.Count ?? 0,
                progressPercentage = stepMatch?.ProgressPercentage ?? 0.0,

                // Node tracking
                currentNodeCode = currentNodeCode,
                missionCode = missionCode,
                robotPositionEnabled = RobotPositionPollingEnabled,

                // Manual waypoint tracking (consolidated)
                isWaitingAtManualWaypoint = waitingMission != null,
                manualWaypointData = waitingMission
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error querying job status for {MissionCode}", missionCode);
            return StatusCode(500, new { success = false, message = "Error querying job status" });
        }
    }

    public async Task<IActionResult> OnGetQueueStatusAsync(int id, CancellationToken cancellationToken)
    {
        var token = HttpContext.Session.GetString("JwtToken");
        if (string.IsNullOrEmpty(token))
        {
            return new JsonResult(new { success = false, message = "Session expired" }) { StatusCode = 401 };
        }

        try
        {
            using var client = CreateApiClient(token);
            using var response = await client.GetAsync($"api/mission-queue/by-workflow/{id}", cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync(cancellationToken);
                // API returns a single queue item directly (not wrapped in ApiResponse)
                var queueItem = JsonSerializer.Deserialize<QueueItemDto>(content, JsonOptions);

                if (queueItem != null && !string.IsNullOrEmpty(queueItem.MissionCode))
                {
                    _logger.LogInformation("Found active queue item for mission {MissionId}: {MissionCode}, Status: {Status}",
                        id, queueItem.MissionCode, queueItem.Status);

                    return new JsonResult(new
                    {
                        success = true,
                        missionCode = queueItem.MissionCode,
                        status = queueItem.Status,
                        isActive = true
                    });
                }
            }
            else if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                // 404 means no active queue item found - this is normal
                _logger.LogInformation("No active queue item found for mission {MissionId}", id);

                return new JsonResult(new
                {
                    success = true,
                    missionCode = (string?)null,
                    status = "idle",
                    isActive = false
                });
            }

            _logger.LogWarning("Failed to retrieve queue status for mission {MissionId}. Status: {StatusCode}", id, response.StatusCode);
            return new JsonResult(new { success = false, message = $"Failed to retrieve queue status. Status: {response.StatusCode}" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving queue status for mission {MissionId}", id);
            return new JsonResult(new { success = false, message = "Error retrieving queue status" }) { StatusCode = 500 };
        }
    }

    private async Task<string?> GetRobotCurrentNodeAsync(string robotId, string token)
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

            using var response = await client.PostAsync("api/robot-query", content);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Robot query failed for {RobotId}. Status: {StatusCode}", robotId, response.StatusCode);
                return null;
            }

            var responseContent = await response.Content.ReadAsStringAsync();
            var robotQueryResponse = JsonSerializer.Deserialize<RobotQueryResponseDto>(responseContent, JsonOptions);

            if (robotQueryResponse?.Data != null && robotQueryResponse.Data.Count > 0)
            {
                var robotData = robotQueryResponse.Data[0];
                _logger.LogInformation("Robot {RobotId} at node: {NodeCode}", robotId, robotData.NodeCode);
                return robotData.NodeCode;
            }

            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting robot position for {RobotId}", robotId);
            return null;
        }
    }

    private async Task<StepMatchResult> GetCurrentStepMatchAsync(int missionId, string nodeCode, string token)
    {
        var matchResult = new StepMatchResult
        {
            NodeCode = nodeCode,
            MatchType = StepMatchType.None
        };

        try
        {
            // Load mission if not already loaded
            if (Mission == null || Mission.Id != missionId)
            {
                await LoadMissionAsync(missionId, token);
            }

            if (Mission?.Steps == null || Mission.Steps.Count == 0)
            {
                return matchResult;
            }

            // 1. EXACT MATCH (highest priority)
            for (int i = 0; i < Mission.Steps.Count; i++)
            {
                if (string.Equals(Mission.Steps[i].Position, nodeCode, StringComparison.OrdinalIgnoreCase))
                {
                    _logger.LogInformation("Robot at step {Index} (Position: {Position}) - EXACT MATCH", i, Mission.Steps[i].Position);
                    matchResult.CurrentStepIndex = i;
                    matchResult.MatchType = StepMatchType.Exact;
                    matchResult.CurrentStep = Mission.Steps[i];
                    PopulateProgressData(matchResult, i, Mission.Steps.Count);
                    return matchResult;
                }
            }

            // 2. AREA MATCH (if nodeCode is within an area/aisle)
            for (int i = 0; i < Mission.Steps.Count; i++)
            {
                var step = Mission.Steps[i];
                if (IsNodeInArea(nodeCode, step.Position))
                {
                    _logger.LogInformation("Robot at step {Index} (Position: {Position}, Area: {AreaCode}) - AREA MATCH", i, step.Position, step.Position);
                    matchResult.CurrentStepIndex = i;
                    matchResult.MatchType = StepMatchType.Area;
                    matchResult.CurrentStep = step;
                    matchResult.IsInArea = true;
                    PopulateProgressData(matchResult, i, Mission.Steps.Count);
                    return matchResult;
                }
            }

            // 3. FUZZY MATCH (similar positions)
            var fuzzyMatch = FindFuzzyMatch(nodeCode, Mission.Steps);
            if (fuzzyMatch.HasValue)
            {
                var match = fuzzyMatch.Value;
                _logger.LogInformation("Robot at step {Index} (Position: {Position}) - FUZZY MATCH ({Confidence:P0})",
                    match.Index, match.Step.Position, match.Confidence);
                matchResult.CurrentStepIndex = match.Index;
                matchResult.MatchType = StepMatchType.Fuzzy;
                matchResult.CurrentStep = match.Step;
                matchResult.Confidence = match.Confidence;
                PopulateProgressData(matchResult, match.Index, Mission.Steps.Count);
                return matchResult;
            }

            _logger.LogWarning("No step found matching nodeCode {NodeCode}", nodeCode);
            matchResult.CurrentStepIndex = null;
            matchResult.CurrentStep = null;
            PopulateProgressData(matchResult, -1, Mission.Steps.Count);
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
            // This is a simplified implementation - in production you might query the API for detailed area node lists
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

    private async Task LoadMissionAsync(int id, string token)
    {
        try
        {
            var httpClient = _httpClientFactory.CreateClient();
            var apiBaseUrl = _configuration["ApiSettings:BaseUrl"] ?? "http://localhost:5000";
            var apiUrl = $"{apiBaseUrl}/api/saved-custom-missions/{id}";

            httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var response = await httpClient.GetAsync(apiUrl);

            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadAsStringAsync();
                var result = JsonSerializer.Deserialize<ApiResponse<SavedMissionDto>>(
                    json,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                if (result?.Data != null)
                {
                    // DEBUG: Log the raw JSON
                    _logger.LogInformation("=== WORKFLOW MONITOR BACKEND DEBUG ===");
                    _logger.LogInformation("Mission ID: {Id}, Name: {Name}", result.Data.Id, result.Data.MissionName);
                    _logger.LogInformation("Raw MissionStepsJson: {Json}", result.Data.MissionStepsJson);

                    // Parse mission steps
                    var steps = new List<MissionStepDto>();
                    if (!string.IsNullOrEmpty(result.Data.MissionStepsJson))
                    {
                        // IMPORTANT: Use PropertyNameCaseInsensitive because CustomMission saves as camelCase
                        steps = JsonSerializer.Deserialize<List<MissionStepDto>>(
                            result.Data.MissionStepsJson,
                            new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
                        ) ?? new List<MissionStepDto>();

                        _logger.LogInformation("Parsed {Count} steps", steps.Count);
                        if (steps.Count > 0)
                        {
                            _logger.LogInformation("First step - Sequence: {Seq}, Position: {Pos}, PutDown: {Put}, Type: {Type}",
                                steps[0].Sequence, steps[0].Position, steps[0].PutDown, steps[0].Type);
                        }
                    }
                    _logger.LogInformation("=====================================");

                    Mission = new WorkflowMissionDto
                    {
                        Id = result.Data.Id,
                        MissionName = result.Data.MissionName,
                        Description = result.Data.Description,
                        MissionType = result.Data.MissionType,
                        RobotType = result.Data.RobotType,
                        Priority = result.Data.Priority,
                        RobotModels = result.Data.RobotModels,
                        RobotIds = result.Data.RobotIds,
                        ContainerModelCode = result.Data.ContainerModelCode,
                        ContainerCode = result.Data.ContainerCode,
                        IdleNode = result.Data.IdleNode,
                        Steps = steps,
                        CreatedBy = result.Data.CreatedBy,
                        CreatedUtc = result.Data.CreatedUtc
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
    }

    private HttpClient CreateApiClient(string token)
    {
        var apiBaseUrl = _configuration["ApiSettings:BaseUrl"] ?? "http://localhost:5000";
        var client = _httpClientFactory.CreateClient();
        client.BaseAddress = new Uri(apiBaseUrl);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return client;
    }

    private (string Name, string CssClass, bool IsTerminal) GetStatusDisplayInfo(int status)
    {
        var config = _configuration.GetSection("JobStatusPolling:StatusCodes");

        if (status == config.GetValue<int>("Created"))
            return ("Queued", "status-created", false);
        if (status == config.GetValue<int>("Executing"))
            return ("Running", "status-executing", false);
        if (status == config.GetValue<int>("Waiting"))
            return ("Waiting", "status-waiting", false);
        if (status == config.GetValue<int>("Cancelling"))
            return ("Cancelling", "status-cancelling", false);
        if (status == config.GetValue<int>("Complete"))
            return ("Completed", "status-complete", true);
        if (status == config.GetValue<int>("Cancelled"))
            return ("Cancelled", "status-cancelled", true);
        if (status == config.GetValue<int>("ManualComplete"))
            return ("Manual Complete", "status-complete", true);
        if (status == config.GetValue<int>("Warning"))
            return ("Warning", "status-warning", false);
        if (status == config.GetValue<int>("StartupError"))
            return ("Failed", "status-error", true);

        return ($"Unknown ({status})", "status-unknown", false);
    }

    public async Task<IActionResult> OnGetCheckManualWaypointAsync(string missionCode)
    {
        var token = HttpContext.Session.GetString("JwtToken");
        if (string.IsNullOrEmpty(token))
        {
            return new JsonResult(new List<object>()) { StatusCode = 401 };
        }

        try
        {
            using var httpClient = CreateApiClient(token);
            var apiBaseUrl = _configuration["ApiSettings:BaseUrl"] ?? "http://localhost:5000";
            var apiUrl = $"{apiBaseUrl}/api/missions/waiting-for-resume";

            _logger.LogInformation("Checking for manual waypoint for mission {MissionCode}", missionCode);

            var response = await httpClient.GetAsync(apiUrl);

            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                var allWaitingMissions = JsonSerializer.Deserialize<List<WaitingMissionDto>>(content, JsonOptions);

                // Filter to only return the mission that matches the current missionCode
                var filteredMissions = allWaitingMissions?
                    .Where(m => m.MissionCode == missionCode)
                    .ToList() ?? new List<WaitingMissionDto>();

                _logger.LogInformation("Found {Count} waiting missions for {MissionCode}", filteredMissions.Count, missionCode);

                return new JsonResult(filteredMissions);
            }
            else
            {
                _logger.LogWarning("Failed to get waiting missions. Status: {StatusCode}", response.StatusCode);
                return new JsonResult(new List<object>());
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking for manual waypoint for mission {MissionCode}", missionCode);
            return new JsonResult(new List<object>());
        }
    }

    public async Task<IActionResult> OnPostResumeManualWaypointAsync([FromBody] ResumeManualWaypointRequest request)
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

            var response = await httpClient.PostAsync(apiUrl, jsonContent);
            var responseContent = await response.Content.ReadAsStringAsync();

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
            // DTOs that are needed in the code but were removed when I cleaned up duplicates
            public class WorkflowMissionDto
            {
                public int Id { get; set; }
                public string MissionName { get; set; } = string.Empty;
                public string? Description { get; set; }
                public string? Type { get; set; }

                public string? RobotModels { get; set; }
                public string? Status { get; set; }
                public DateTime CreatedUtc { get; set; }
                public DateTime? StartedUtc { get; set; }
                public DateTime? CompletedUtc { get; set; }
                public string CreatedBy { get; set; } = string.Empty;  
                public string? AssignedRobotId { get; set; }
                public string? MissionCode { get; set; }
                public string MissionStepsJson { get; set; } = string.Empty;  // JSON representation of mission steps
                public List<MissionStepDto> Steps { get; set; } = new();
                
                // Additional missing properties
                public string? MissionType { get; set; }
                public int? Priority { get; set; }
                public string? RobotType { get; set; }
                public string? RobotIds { get; set; }
                public string? ContainerCode { get; set; }
                public string? ContainerModelCode { get; set; }
                public string? IdleNode { get; set; }
            }

            public class StartMissionRequest
            {
                public string MissionCode { get; set; } = string.Empty;
                public string? RobotId { get; set; }
                public string? MissionType { get; set; }
                public List<MissionStepDto>? Steps { get; set; }
                public int? SavedMissionId { get; set; }
            }

            public class CancelMissionRequest
            {
                public string MissionCode { get; set; } = string.Empty;
                public string? CancelMode { get; set; }
                public string? Reason { get; set; }
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

            public class WorkflowMonitorMapZoneSummaryDto
            {
                public string ZoneName { get; set; } = string.Empty;
                public string ZoneCode { get; set; } = string.Empty;
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
            }

            public enum StepMatchType
            {
                None,
                Exact,
                Area,
                Fuzzy
            }
        }
    



