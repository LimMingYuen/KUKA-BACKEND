using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using QES_KUKA_AMR_API.Data;
using QES_KUKA_AMR_API.Data.Entities;
using QES_KUKA_AMR_API.Models.Jobs;
using QES_KUKA_AMR_API.Options;

namespace QES_KUKA_AMR_API.Services;

/// <summary>
/// Background service that polls AMR job status for processing missions and updates queue accordingly
/// </summary>
public class JobStatusPollerBackgroundService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<JobStatusPollerBackgroundService> _logger;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly MissionQueueSettings _queueSettings;
    private readonly MissionServiceOptions _missionServiceOptions;
    private readonly TimeSpan _pollingInterval;

    // Status codes that indicate mission completion (terminal states)
    private static readonly HashSet<int> TerminalStatusCodes = new() { 30, 31, 35, 60 };
    // Status code 30 = Complete, 35 = ManualComplete (success)
    private static readonly HashSet<int> SuccessStatusCodes = new() { 30, 35 };

    public JobStatusPollerBackgroundService(
        IServiceProvider serviceProvider,
        ILogger<JobStatusPollerBackgroundService> logger,
        IHttpClientFactory httpClientFactory,
        IOptions<MissionQueueSettings> queueSettings,
        IOptions<MissionServiceOptions> missionServiceOptions)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
        _httpClientFactory = httpClientFactory;
        _queueSettings = queueSettings.Value;
        _missionServiceOptions = missionServiceOptions.Value;
        _pollingInterval = TimeSpan.FromSeconds(_queueSettings.JobStatusPollingIntervalSeconds);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("=== JobStatusPollerBackgroundService Starting ===");
        _logger.LogInformation("Polling interval: {Interval} seconds", _queueSettings.JobStatusPollingIntervalSeconds);

        // Wait a bit before starting to ensure all services are ready
        await Task.Delay(TimeSpan.FromSeconds(3), stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await PollJobStatusesAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in job status poller background service");
            }

            // Wait for the next polling cycle
            await Task.Delay(_pollingInterval, stoppingToken);
        }

        _logger.LogInformation("=== JobStatusPollerBackgroundService Stopping ===");
    }

    private async Task PollJobStatusesAsync(CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var queueService = scope.ServiceProvider.GetRequiredService<IQueueService>();

        try
        {
            // Get all Processing missions (excluding those waiting for manual resume)
            var processingMissions = await context.MissionQueues
                .Where(m => m.Status == QueueStatus.Processing && !m.IsWaitingForManualResume)
                .ToListAsync(cancellationToken);

            if (processingMissions.Count == 0)
            {
                return;
            }

            _logger.LogInformation("Polling job status for {Count} processing mission(s)", processingMissions.Count);

            var httpClient = _httpClientFactory.CreateClient();
            int completedCount = 0;

            foreach (var mission in processingMissions)
            {
                try
                {
                    var jobStatus = await QueryJobStatusAsync(httpClient, mission.MissionCode, cancellationToken);

                    if (jobStatus == null)
                    {
                        _logger.LogWarning("Could not retrieve job status for mission {MissionCode}", mission.MissionCode);
                        continue;
                    }
                    else
                    {
                        _logger.LogInformation("Job status payload for {MissionCode}: Status={Status} ({StatusName}), RobotId={RobotId}, WorkflowName={WorkflowName}, TargetCell={TargetCell}",
                            mission.MissionCode,
                            jobStatus.Status,
                            GetStatusName(jobStatus.Status),
                            jobStatus.RobotId ?? "null",
                            jobStatus.WorkflowName ?? "null",
                            jobStatus.TargetCellCode ?? "null");
                    }

                    // CRITICAL FIX: Set AssignedRobotId immediately when we get it from jobQuery
                    // This ensures robotId is persisted even if the robotQuery API call fails
                    if (!string.IsNullOrWhiteSpace(jobStatus.RobotId) && string.IsNullOrWhiteSpace(mission.AssignedRobotId))
                    {
                        mission.AssignedRobotId = jobStatus.RobotId;
                        _logger.LogInformation("✓ Set AssignedRobotId for mission {MissionCode}: {RobotId}",
                            mission.MissionCode, jobStatus.RobotId);
                    }

                    // Query robot status if robotId is available (for additional details like battery, node position)
                    if (!string.IsNullOrWhiteSpace(jobStatus.RobotId))
                    {
                        try
                        {
                            var robotData = await QueryRobotStatusAsync(httpClient, jobStatus.RobotId, cancellationToken);
                            if (robotData != null)
                            {
                                _logger.LogInformation("Robot status payload for {MissionCode}: RobotId={RobotId}, NodeCode={NodeCode}, Status={Status}, Battery={Battery}, MissionCode={RobotMissionCode}",
                                    mission.MissionCode,
                                    robotData.RobotId ?? "null",
                                    robotData.NodeCode ?? "null",
                                    robotData.Status,
                                    robotData.BatteryLevel,
                                    robotData.MissionCode ?? "null");

                                // Update mission with additional robot data (robotId already set above)
                                mission.RobotNodeCode = robotData.NodeCode;
                                mission.RobotStatusCode = robotData.Status;
                                mission.RobotBatteryLevel = robotData.BatteryLevel;
                                mission.LastRobotQueryTime = DateTime.UtcNow;

                                _logger.LogInformation("🤖 Robot data for {MissionCode}: Robot={RobotId}, Node={NodeCode}, Battery={Battery}%, Status={Status}",
                                    mission.MissionCode, robotData.RobotId, robotData.NodeCode, robotData.BatteryLevel, robotData.Status);

                                // Check if robot reached a manual waypoint
                                var reachedManualWaypoint = await CheckManualWaypointAsync(mission, robotData, context, cancellationToken);
                                if (reachedManualWaypoint)
                                {
                                    _logger.LogWarning("⏸️ Mission {MissionCode} PAUSED at manual waypoint. IsWaitingForManualResume={IsWaiting}",
                                        mission.MissionCode, mission.IsWaitingForManualResume);
                                    // Skip further processing - mission is now paused
                                    continue;
                                }
                            }
                        }
                        catch (Exception robotEx)
                        {
                            _logger.LogWarning(robotEx, "Failed to query robot status for robot {RobotId} (mission {MissionCode})",
                                jobStatus.RobotId, mission.MissionCode);
                            // Continue processing even if robot query fails
                        }
                    }

                    // Check if job has reached a terminal state
                    if (TerminalStatusCodes.Contains(jobStatus.Status))
                    {
                        var isSuccess = SuccessStatusCodes.Contains(jobStatus.Status);
                        var statusName = GetStatusName(jobStatus.Status);
                        var errorMessage = isSuccess ? null : $"Mission ended with status: {statusName} ({jobStatus.Status})";

                        _logger.LogInformation("Mission {MissionCode} reached terminal status {Status} ({StatusName}). Marking as {Result}",
                            mission.MissionCode, jobStatus.Status, statusName, isSuccess ? "Completed" : "Failed");

                        // Call QueueService to delete mission from queue and record in history
                        await queueService.OnMissionCompletedAsync(
                            mission.MissionCode,
                            isSuccess,
                            errorMessage,
                            cancellationToken);

                        completedCount++;
                    }
                    else
                    {
                        // Only save robot data updates if mission didn't complete (and get deleted)
                        // Check if mission still exists in the database before saving changes
                        try
                        {
                            await context.SaveChangesAsync(cancellationToken);
                            _logger.LogInformation("✓ Saved robot data updates for mission {MissionCode}", mission.MissionCode);
                        }
                        catch (DbUpdateConcurrencyException ex)
                        {
                            // Mission might have been deleted by another process (e.g. completion), ignore
                            _logger.LogInformation("Concurrency exception updating mission {MissionCode}, possibly already deleted: {Message}", mission.MissionCode, ex.Message);
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error polling status for mission {MissionCode}", mission.MissionCode);
                }
            }

            if (completedCount > 0)
            {
                _logger.LogInformation("✓ Marked {Count} mission(s) as completed based on AMR job status", completedCount);
            }

            // The save for robot data updates is now handled individually above to avoid issues when missions are deleted
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error polling job statuses");
        }
    }

    private async Task<JobDto?> QueryJobStatusAsync(HttpClient httpClient, string missionCode, CancellationToken cancellationToken)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(_missionServiceOptions.JobQueryUrl))
            {
                _logger.LogWarning("JobQueryUrl is not configured");
                return null;
            }

            var request = new JobQueryRequest
            {
                JobCode = missionCode,
                Limit = 1
            };

            // Log request details
            var requestBody = JsonSerializer.Serialize(request);
            _logger.LogInformation("QueryJobStatusAsync Request - MissionCode: {MissionCode}, URL: {Url}, Body: {Body}",
                missionCode, _missionServiceOptions.JobQueryUrl, requestBody);

            using var response = await httpClient.PostAsJsonAsync(_missionServiceOptions.JobQueryUrl, request, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Job query failed for {MissionCode}. Status: {StatusCode}",
                    missionCode, response.StatusCode);
                return null;
            }

            var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);
            _logger.LogInformation("QueryJobStatusAsync Response - MissionCode: {MissionCode}, Status: {StatusCode}, Body: {Body}",
                missionCode, response.StatusCode, responseBody);

            var queryResponse = JsonSerializer.Deserialize<JobQueryResponse>(responseBody);

            if (queryResponse?.Data == null || queryResponse.Data.Count == 0)
            {
                _logger.LogInformation("No job found for mission code {MissionCode}", missionCode);
                return null;
            }

            return queryResponse.Data[0];
        }
        catch (HttpRequestException ex)
        {
            _logger.LogWarning(ex, "HTTP error querying job status for {MissionCode}", missionCode);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error querying job status for {MissionCode}", missionCode);
            return null;
        }
    }

    private async Task<Models.Missions.RobotDataDto?> QueryRobotStatusAsync(HttpClient httpClient, string robotId, CancellationToken cancellationToken)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(_missionServiceOptions.RobotQueryUrl))
            {
                _logger.LogWarning("RobotQueryUrl is not configured");
                return null;
            }

            var request = new Models.Missions.RobotQueryRequest
            {
                RobotId = robotId
            };

            // Log request details
            var requestBody = JsonSerializer.Serialize(request);
            _logger.LogInformation("QueryRobotStatusAsync Request - RobotId: {RobotId}, URL: {Url}, Body: {Body}",
                robotId, _missionServiceOptions.RobotQueryUrl, requestBody);

            using var response = await httpClient.PostAsJsonAsync(_missionServiceOptions.RobotQueryUrl, request, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Robot query failed for {RobotId}. Status: {StatusCode}",
                    robotId, response.StatusCode);
                return null;
            }

            var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);
            _logger.LogInformation("QueryRobotStatusAsync Response - RobotId: {RobotId}, Status: {StatusCode}, Body: {Body}",
                robotId, response.StatusCode, responseBody);

            var queryResponse = JsonSerializer.Deserialize<Models.Missions.RobotQueryResponse>(responseBody);

            if (queryResponse?.Data == null || queryResponse.Data.Count == 0)
            {
                _logger.LogInformation("No robot data found for robot {RobotId}", robotId);
                return null;
            }

            return queryResponse.Data[0];
        }
        catch (HttpRequestException ex)
        {
            _logger.LogWarning(ex, "HTTP error querying robot status for {RobotId}", robotId);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error querying robot status for {RobotId}", robotId);
            return null;
        }
    }

    private static string GetStatusName(int status)
    {
        return status switch
        {
            10 => "Created",
            20 => "Executing",
            25 => "Waiting",
            28 => "Cancelling",
            30 => "Complete",
            31 => "Cancelled",
            35 => "ManualComplete",
            50 => "Warning",
            60 => "StartupError",
            _ => $"Unknown ({status})"
        };
    }

    private static string GetMissionDataSnippet(string? missionDataJson)
    {
        if (string.IsNullOrWhiteSpace(missionDataJson))
        {
            return "null";
        }

        const int maxLength = 300;
        var normalized = missionDataJson.Replace(Environment.NewLine, string.Empty);
        if (normalized.Length <= maxLength)
        {
            return normalized;
        }

        return normalized[..maxLength] + "...";
    }

    /// <summary>
    /// Parses manual waypoints from MissionDataJson and caches them in ManualWaypointsJson
    /// </summary>
    private static List<string> ParseManualWaypoints(string? missionDataJson)
    {
        var manualWaypoints = new List<string>();

        if (string.IsNullOrWhiteSpace(missionDataJson))
        {
            return manualWaypoints;
        }

        try
        {
            var missionData = JsonSerializer.Deserialize<List<Models.Missions.MissionDataItem>>(
                missionDataJson,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            if (missionData == null)
            {
                return manualWaypoints;
            }

            // Extract positions where passStrategy is "MANUAL"
            manualWaypoints = missionData
                .Where(step => !string.IsNullOrWhiteSpace(step.Position) &&
                               string.Equals(step.PassStrategy, "MANUAL", StringComparison.OrdinalIgnoreCase))
                .Select(step => step.Position!)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
        }
        catch (JsonException)
        {
            // Log error but don't throw - mission can continue without manual waypoint detection
            return manualWaypoints;
        }

        return manualWaypoints;
    }

    /// <summary>
    /// Resolves manual waypoints to their constituent node codes, handling both direct nodes and area codes
    /// </summary>
    /// <returns>Dictionary mapping original waypoint code to list of resolved node codes</returns>
    private async Task<Dictionary<string, List<string>>> ResolveWaypointsToNodesAsync(
        List<string> manualWaypoints,
        List<string> visitedWaypoints,
        ApplicationDbContext context,
        CancellationToken cancellationToken)
    {
        var result = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);

        _logger.LogInformation("🔧 ResolveWaypointsToNodesAsync - Input: {TotalWaypoints} manual waypoint(s), {VisitedCount} visited",
            manualWaypoints.Count, visitedWaypoints.Count);
        _logger.LogInformation("🔧 All manual waypoints: [{AllWaypoints}]", string.Join(", ", manualWaypoints));
        _logger.LogInformation("🔧 Visited waypoints: [{VisitedWaypoints}]",
            visitedWaypoints.Count > 0 ? string.Join(", ", visitedWaypoints) : "none");

        // Filter out visited waypoints
        var unvisitedWaypoints = manualWaypoints
            .Where(wp => !visitedWaypoints.Contains(wp, StringComparer.OrdinalIgnoreCase))
            .ToList();

        _logger.LogInformation("🔧 Unvisited waypoints to resolve: {Count} [{Waypoints}]",
            unvisitedWaypoints.Count,
            unvisitedWaypoints.Count > 0 ? string.Join(", ", unvisitedWaypoints) : "none");

        foreach (var waypoint in unvisitedWaypoints)
        {
            _logger.LogInformation("🔍 Resolving waypoint: '{Waypoint}'", waypoint);

            // Check if this waypoint is an area code in MapZones
            var mapZone = await context.MapZones
                .FirstOrDefaultAsync(mz => mz.ZoneCode == waypoint, cancellationToken);

            if (mapZone != null)
            {
                _logger.LogInformation("✅ Found MapZone for '{Waypoint}': ZoneName={ZoneName}, ZoneType={ZoneType}, HasConfigs={HasConfigs}",
                    waypoint, mapZone.ZoneName, mapZone.ZoneType, !string.IsNullOrWhiteSpace(mapZone.Configs));

                if (!string.IsNullOrWhiteSpace(mapZone.Configs))
                {
                    _logger.LogInformation("📄 MapZone Configs JSON: {Configs}", mapZone.Configs);

                    // Try to parse the Configs JSON to extract areaNodeList
                    try
                    {
                        var configs = JsonSerializer.Deserialize<Models.MapZone.MapZoneConfigsDto>(
                            mapZone.Configs,
                            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                        if (configs != null)
                        {
                            _logger.LogInformation("✅ Parsed Configs. AreaNodeList present: {HasAreaNodeList}",
                                !string.IsNullOrWhiteSpace(configs.AreaNodeList));

                            if (!string.IsNullOrWhiteSpace(configs.AreaNodeList))
                            {
                                _logger.LogInformation("📄 AreaNodeList JSON: {AreaNodeList}", configs.AreaNodeList);

                                var areaNodes = JsonSerializer.Deserialize<List<Models.MapZone.AreaNodeDto>>(
                                    configs.AreaNodeList,
                                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                                if (areaNodes != null && areaNodes.Count > 0)
                                {
                                    var nodeCodes = areaNodes.Select(n => n.CellCode).ToList();
                                    result[waypoint] = nodeCodes;
                                    _logger.LogInformation("✅ Resolved AREA '{Waypoint}' → {Count} nodes: [{Nodes}]",
                                        waypoint, nodeCodes.Count, string.Join(", ", nodeCodes));
                                    continue;
                                }
                                else
                                {
                                    _logger.LogInformation("⚠️ AreaNodeList is empty or null for waypoint '{Waypoint}'", waypoint);
                                }
                            }
                            else
                            {
                                _logger.LogInformation("⚠️ Configs.AreaNodeList is null/empty for waypoint '{Waypoint}'", waypoint);
                            }
                        }
                        else
                        {
                            _logger.LogInformation("⚠️ Failed to deserialize Configs JSON for waypoint '{Waypoint}'", waypoint);
                        }
                    }
                    catch (JsonException ex)
                    {
                        _logger.LogInformation(ex, "❌ JSON parsing failed for MapZone configs for waypoint '{Waypoint}'. Configs: {Configs}",
                            waypoint, mapZone.Configs);
                    }
                }
                else
                {
                    _logger.LogInformation("ℹ️ MapZone '{Waypoint}' has no Configs - treating as direct node", waypoint);
                }
            }
            else
            {
                _logger.LogInformation("ℹ️ No MapZone found for '{Waypoint}' - treating as direct NODE code", waypoint);
            }

            // Not an area or failed to parse - treat as direct node code
            result[waypoint] = new List<string> { waypoint };
            _logger.LogInformation("✅ Treating '{Waypoint}' as direct NODE → [{Node}]", waypoint, waypoint);
        }

        _logger.LogInformation("🔧 ResolveWaypointsToNodesAsync - Output: {Count} resolved waypoint(s)", result.Count);
        foreach (var kvp in result)
        {
            _logger.LogInformation("    '{Original}' → [{Resolved}]", kvp.Key, string.Join(", ", kvp.Value));
        }

        return result;
    }

    /// <summary>
    /// Checks if robot has reached a manual waypoint and pauses mission if so
    /// </summary>
    private async Task<bool> CheckManualWaypointAsync(
        MissionQueue mission,
        Models.Missions.RobotDataDto robotData,
        ApplicationDbContext context,
        CancellationToken cancellationToken)
    {
        // Parse manual waypoints if not cached
        List<string>? manualWaypoints = null;
        if (!string.IsNullOrWhiteSpace(mission.ManualWaypointsJson))
        {
            try
            {
                manualWaypoints = JsonSerializer.Deserialize<List<string>>(mission.ManualWaypointsJson);
            }
            catch (JsonException)
            {
                // Re-parse from mission data
                manualWaypoints = null;
            }
        }

        if (manualWaypoints == null)
        {
            manualWaypoints = ParseManualWaypoints(mission.MissionDataJson);

            // Cache for future checks
            if (manualWaypoints.Count > 0)
            {
                mission.ManualWaypointsJson = JsonSerializer.Serialize(manualWaypoints);
            }
        }

        // No manual waypoints in this mission
        if (manualWaypoints.Count == 0)
        {
            _logger.LogInformation("Mission {MissionCode} has no MANUAL waypoints. MissionDataJson snippet: {Snippet}",
                mission.MissionCode,
                GetMissionDataSnippet(mission.MissionDataJson));
            return false;
        }

        // Parse visited waypoints
        List<string> visitedWaypoints = new();
        if (!string.IsNullOrWhiteSpace(mission.VisitedManualWaypointsJson))
        {
            try
            {
                visitedWaypoints = JsonSerializer.Deserialize<List<string>>(mission.VisitedManualWaypointsJson) ?? new List<string>();
            }
            catch (JsonException ex)
            {
                _logger.LogInformation(ex, "Failed to parse VisitedManualWaypointsJson for mission {MissionCode}", mission.MissionCode);
            }
        }

        _logger.LogInformation("📍 Mission {MissionCode} has {Count} MANUAL waypoint(s): [{Waypoints}]. Visited: {VisitedCount}",
            mission.MissionCode,
            manualWaypoints.Count,
            string.Join(", ", manualWaypoints),
            visitedWaypoints.Count);

        // Check if robot position is available
        if (string.IsNullOrWhiteSpace(robotData.NodeCode))
        {
            _logger.LogInformation("Robot data for mission {MissionCode} does not contain nodeCode. Skipping manual waypoint check.", mission.MissionCode);
            return false;
        }

        // Resolve waypoints to node codes (handling areas and visited waypoints)
        var resolvedWaypoints = await ResolveWaypointsToNodesAsync(manualWaypoints, visitedWaypoints, context, cancellationToken);

        if (resolvedWaypoints.Count == 0)
        {
            _logger.LogInformation("All manual waypoints for mission {MissionCode} have been visited. No more pauses needed.", mission.MissionCode);
            return false;
        }

        _logger.LogInformation("🔍 Comparing robot position '{NodeCode}' with {Count} unvisited waypoint(s)...",
            robotData.NodeCode, resolvedWaypoints.Count);

        // Check if robot is at any unvisited manual waypoint
        foreach (var kvp in resolvedWaypoints)
        {
            var originalWaypoint = kvp.Key;
            var resolvedNodes = kvp.Value;

            _logger.LogInformation("    Checking waypoint '{Original}' → resolved to [{Nodes}]",
                originalWaypoint, string.Join(", ", resolvedNodes));

            foreach (var node in resolvedNodes)
            {
                var matches = string.Equals(node, robotData.NodeCode, StringComparison.OrdinalIgnoreCase);
                _logger.LogInformation("        Compare: '{RobotNode}' == '{WaypointNode}' ? {Result}",
                    robotData.NodeCode, node, matches ? "✅ MATCH!" : "❌ no match");

                if (matches)
                {
                    _logger.LogInformation("🛑 Mission {MissionCode} reached MANUAL waypoint '{OriginalWaypoint}' (robot at node {NodeCode}). " +
                        "Robot {RobotId} is now waiting for user to resume.",
                        mission.MissionCode, originalWaypoint, robotData.NodeCode, robotData.RobotId);
                    _logger.LogInformation("Updating mission {MissionCode} queue record: setting IsWaitingForManualResume=true, CurrentManualWaypointPosition={Waypoint}",
                        mission.MissionCode, originalWaypoint);

                    // Store the ORIGINAL waypoint code (not the resolved node)
                    mission.IsWaitingForManualResume = true;
                    mission.CurrentManualWaypointPosition = originalWaypoint;

                    var robotId = robotData.RobotId ?? mission.AssignedRobotId;
                    if (!string.IsNullOrWhiteSpace(robotId))
                    {
                        var hasOpenPause = await context.RobotManualPauses
                            .AnyAsync(
                                pause => pause.MissionCode == mission.MissionCode && pause.PauseEndUtc == null,
                                cancellationToken);

                        if (!hasOpenPause)
                        {
                            var timestamp = DateTime.UtcNow;
                            context.RobotManualPauses.Add(new RobotManualPause
                            {
                                RobotId = robotId.Trim(),
                                MissionCode = mission.MissionCode,
                                WaypointCode = originalWaypoint,
                                PauseStartUtc = timestamp,
                                CreatedUtc = timestamp,
                                UpdatedUtc = timestamp,
                                Reason = "Manual waypoint pause detected"
                            });
                        }
                    }

                    await context.SaveChangesAsync(cancellationToken);
                    _logger.LogInformation("✅ Mission {MissionCode} state persisted. IsWaitingForManualResume={IsWaiting}, CurrentManualWaypointPosition={Waypoint}",
                        mission.MissionCode, mission.IsWaitingForManualResume, mission.CurrentManualWaypointPosition);

                    return true;
                }
            }
        }

        _logger.LogInformation("❌ Robot at '{NodeCode}' - NOT at any unvisited manual waypoint",
            robotData.NodeCode);

        return false;
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("JobStatusPollerBackgroundService is stopping");
        await base.StopAsync(cancellationToken);
    }
}
