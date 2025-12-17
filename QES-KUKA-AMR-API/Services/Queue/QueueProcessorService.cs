using Microsoft.Extensions.Options;
using QES_KUKA_AMR_API.Data;
using QES_KUKA_AMR_API.Data.Entities;
using QES_KUKA_AMR_API.Models;
using QES_KUKA_AMR_API.Models.Jobs;
using QES_KUKA_AMR_API.Models.Missions;
using QES_KUKA_AMR_API.Options;
using QES_KUKA_AMR_API.Services.Auth;
using System.Net.Http.Json;
using System.Text.Json;

namespace QES_KUKA_AMR_API.Services.Queue;

/// <summary>
/// Background service that processes the mission queue with parallel robot assignment
/// Uses Priority + FIFO scheduling strategy
/// </summary>
public class QueueProcessorService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<QueueProcessorService> _logger;
    private readonly TimeSpan _processInterval = TimeSpan.FromSeconds(5);

    public QueueProcessorService(
        IServiceProvider serviceProvider,
        ILogger<QueueProcessorService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Queue Processor Service starting...");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessQueueAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing queue");
            }

            await Task.Delay(_processInterval, stoppingToken);
        }

        _logger.LogInformation("Queue Processor Service stopping...");
    }

    /// <summary>
    /// Main queue processing logic with parallel robot assignment
    /// </summary>
    private async Task ProcessQueueAsync(CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var queueService = scope.ServiceProvider.GetRequiredService<IMissionQueueService>();
        var httpClientFactory = scope.ServiceProvider.GetRequiredService<IHttpClientFactory>();
        var missionOptions = scope.ServiceProvider.GetRequiredService<IOptions<MissionServiceOptions>>().Value;
        var tokenService = scope.ServiceProvider.GetRequiredService<IExternalApiTokenService>();
        var jobOptimizationService = scope.ServiceProvider.GetRequiredService<IJobOptimizationService>();

        // Clean up reservations where triggering JOBOPTIMIZATION mission has completed
        var clearedCount = await jobOptimizationService.ClearCompletedReservationsAsync(
            async (missionCode, ct) =>
            {
                var jobStatus = await QueryJobStatusAsync(httpClientFactory, missionOptions, tokenService, missionCode, ct);
                return jobStatus?.Status;
            },
            cancellationToken);
        if (clearedCount > 0)
        {
            _logger.LogInformation("Cleared {Count} reservation(s) for completed JOBOPTIMIZATION missions", clearedCount);
        }

        // Step 1: Get all queued missions (local DB query - fast)
        var queuedMissions = await queueService.GetQueuedAsync(cancellationToken);
        if (queuedMissions.Count == 0)
        {
            return; // No missions to process - skip external API calls
        }

        _logger.LogDebug("Found {Count} queued mission(s) to process", queuedMissions.Count);

        // Step 2: Collect unique preferred robot IDs from all missions
        var neededRobotIds = queuedMissions
            .Where(m => !string.IsNullOrEmpty(m.PreferredRobotIds))
            .SelectMany(m => m.PreferredRobotIds!.Split(',', StringSplitOptions.RemoveEmptyEntries))
            .Select(id => id.Trim())
            .Where(id => !string.IsNullOrEmpty(id))
            .Distinct()
            .ToList();

        // Step 3: Query robots in parallel (only the ones we need)
        var robotStatuses = new Dictionary<string, RobotDataDto>();

        if (neededRobotIds.Count > 0)
        {
            _logger.LogDebug("Querying {Count} robot(s) in parallel: {RobotIds}",
                neededRobotIds.Count, string.Join(", ", neededRobotIds));

            var queryTasks = neededRobotIds.Select(robotId =>
                QueryRobotAsync(httpClientFactory, missionOptions, tokenService, robotId, cancellationToken));

            var results = await Task.WhenAll(queryTasks);

            foreach (var robot in results.Where(r => r != null))
            {
                robotStatuses[robot!.RobotId] = robot;
            }

            _logger.LogDebug("Received status for {Count} robot(s)", robotStatuses.Count);
        }

        // Step 4: Get available (idle) robots
        var availableRobots = robotStatuses
            .Where(r => r.Value.Status == 3) // Status 3 = Idle
            .ToDictionary(r => r.Key, r => r.Value);

        if (availableRobots.Count == 0 && neededRobotIds.Count > 0)
        {
            _logger.LogDebug("No idle robots available from preferred list, missions stay queued");
            return; // Missions remain in Queued status (no requeue needed)
        }

        // Step 5: Sort missions by Priority + FIFO (Priority ASC, CreatedUtc ASC)
        var sortedMissions = queuedMissions
            .OrderBy(m => m.Priority)
            .ThenBy(m => m.CreatedUtc)
            .ToList();

        // Step 6: Match and assign missions to available robots
        await MatchAndAssignAsync(
            scope.ServiceProvider,
            queueService,
            httpClientFactory,
            missionOptions,
            tokenService,
            jobOptimizationService,
            sortedMissions,
            availableRobots,
            cancellationToken);
    }

    /// <summary>
    /// Query a single robot's status from external AMR API
    /// </summary>
    private async Task<RobotDataDto?> QueryRobotAsync(
        IHttpClientFactory httpClientFactory,
        MissionServiceOptions options,
        IExternalApiTokenService tokenService,
        string robotId,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(robotId))
        {
            return null;
        }

        if (string.IsNullOrEmpty(options.RobotQueryUrl))
        {
            _logger.LogWarning("RobotQueryUrl is not configured");
            return null;
        }

        try
        {
            var client = httpClientFactory.CreateClient();

            // AMR endpoints on port 10870 don't require authentication
            client.DefaultRequestHeaders.Clear();
            client.DefaultRequestHeaders.Add("accept", "*/*");
            client.DefaultRequestHeaders.Add("language", "en");
            client.DefaultRequestHeaders.Add("wizards", "FRONT_END");
            // Note: No Authorization header - AMR endpoints don't require auth

            var request = new RobotQueryRequest { RobotId = robotId };

            // Call EXTERNAL API directly (not self-HTTP call)
            var response = await client.PostAsJsonAsync(
                options.RobotQueryUrl,
                request,
                cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogDebug("Robot query failed for {RobotId}: {StatusCode}", robotId, response.StatusCode);
                return null;
            }

            var result = await response.Content.ReadFromJsonAsync<RobotQueryResponse>(cancellationToken);

            if (result?.Success == true && result.Data != null && result.Data.Count > 0)
            {
                return result.Data[0];
            }

            return null;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error querying robot {RobotId}", robotId);
            return null;
        }
    }

    /// <summary>
    /// Match missions to available robots using Priority + FIFO strategy
    /// Assigns multiple missions in a single cycle
    /// Checks for reserved missions first (job optimization)
    /// </summary>
    private async Task MatchAndAssignAsync(
        IServiceProvider scopedProvider,
        IMissionQueueService queueService,
        IHttpClientFactory httpClientFactory,
        MissionServiceOptions missionOptions,
        IExternalApiTokenService tokenService,
        IJobOptimizationService jobOptimizationService,
        List<MissionQueue> sortedMissions,
        Dictionary<string, RobotDataDto> availableRobots,
        CancellationToken cancellationToken)
    {
        var assignedMissionIds = new HashSet<int>();
        var usedRobotIds = new HashSet<string>();
        var processingTasks = new List<Task>();

        // First, check for reserved missions for each available robot
        foreach (var robot in availableRobots.Values)
        {
            if (usedRobotIds.Contains(robot.RobotId))
                continue;

            var reservedMission = await jobOptimizationService.GetReservedMissionAsync(robot.RobotId, cancellationToken);
            if (reservedMission != null && !assignedMissionIds.Contains(reservedMission.Id))
            {
                _logger.LogInformation(
                    "✓ Assigning RESERVED mission {MissionCode} to Robot {RobotId} (job optimization)",
                    reservedMission.MissionCode, robot.RobotId);

                assignedMissionIds.Add(reservedMission.Id);
                usedRobotIds.Add(robot.RobotId);

                // Clear reservation and process
                await jobOptimizationService.ClearReservationAsync(robot.RobotId, cancellationToken);

                var task = Task.Run(() => ProcessSingleMissionWithScopeAsync(
                    reservedMission.Id,
                    robot.RobotId,
                    cancellationToken), cancellationToken);

                processingTasks.Add(task);
            }
        }

        // Then, process remaining missions normally
        foreach (var mission in sortedMissions)
        {
            if (assignedMissionIds.Contains(mission.Id))
                continue;

            // Get preferred robots for this mission
            List<string> preferredRobots;
            if (string.IsNullOrEmpty(mission.PreferredRobotIds))
            {
                // No preference - can use any available robot
                preferredRobots = availableRobots.Keys.ToList();
            }
            else
            {
                preferredRobots = mission.PreferredRobotIds
                    .Split(',', StringSplitOptions.RemoveEmptyEntries)
                    .Select(r => r.Trim())
                    .ToList();
            }

            // Find first available robot from preferences (not yet used in this cycle)
            var selectedRobotId = preferredRobots
                .FirstOrDefault(r => availableRobots.ContainsKey(r) && !usedRobotIds.Contains(r));

            if (selectedRobotId != null)
            {
                // Mark as used immediately to prevent double-assignment
                assignedMissionIds.Add(mission.Id);
                usedRobotIds.Add(selectedRobotId);

                _logger.LogInformation(
                    "Assigning mission {MissionCode} (Priority={Priority}) to Robot {RobotId}",
                    mission.MissionCode, mission.Priority, selectedRobotId);

                // Process mission asynchronously with its own scope (fire-and-forget)
                // Each task gets its own scope to avoid disposed context issues
                var task = Task.Run(() => ProcessSingleMissionWithScopeAsync(
                    mission.Id,
                    selectedRobotId,
                    cancellationToken), cancellationToken);

                processingTasks.Add(task);
            }
            else if (string.IsNullOrEmpty(mission.PreferredRobotIds))
            {
                // No preferred robots AND no available robots
                // Let external AMR system decide - submit without robot selection
                assignedMissionIds.Add(mission.Id);

                _logger.LogInformation(
                    "Submitting mission {MissionCode} (Priority={Priority}) without robot selection",
                    mission.MissionCode, mission.Priority);

                // Process mission asynchronously with its own scope (fire-and-forget)
                var task = Task.Run(() => ProcessSingleMissionWithScopeAsync(
                    mission.Id,
                    null, // No specific robot
                    cancellationToken), cancellationToken);

                processingTasks.Add(task);
            }
            // else: Mission has preferred robots but none are available - stays queued
        }

        // Log summary of assignments
        if (processingTasks.Count > 0)
        {
            _logger.LogInformation(
                "Cycle complete: {Assigned} mission(s) assigned, {Waiting} waiting for robots",
                assignedMissionIds.Count,
                sortedMissions.Count - assignedMissionIds.Count);
        }

        // Note: We don't await processingTasks - they run in background (fire-and-forget)
        // This allows the next processing cycle to start without waiting for missions to complete
    }

    /// <summary>
    /// Process a single mission with its own DI scope to avoid disposed context issues
    /// </summary>
    private async Task ProcessSingleMissionWithScopeAsync(
        int missionId,
        string? selectedRobotId,
        CancellationToken cancellationToken)
    {
        // Create a new scope for this background task
        using var scope = _serviceProvider.CreateScope();
        var queueService = scope.ServiceProvider.GetRequiredService<IMissionQueueService>();
        var httpClientFactory = scope.ServiceProvider.GetRequiredService<IHttpClientFactory>();
        var missionOptions = scope.ServiceProvider.GetRequiredService<IOptions<MissionServiceOptions>>().Value;
        var tokenService = scope.ServiceProvider.GetRequiredService<IExternalApiTokenService>();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var jobOptimizationService = scope.ServiceProvider.GetRequiredService<IJobOptimizationService>();

        // Reload the mission from DB to get fresh data
        var mission = await queueService.GetByIdAsync(missionId, cancellationToken);
        if (mission == null)
        {
            _logger.LogWarning("Mission {MissionId} not found, skipping processing", missionId);
            return;
        }

        await ProcessSingleMissionAsync(
            queueService,
            httpClientFactory,
            missionOptions,
            tokenService,
            dbContext,
            jobOptimizationService,
            mission,
            selectedRobotId,
            cancellationToken);
    }

    /// <summary>
    /// Process a single mission - update status, submit to AMR, poll for completion
    /// </summary>
    private async Task ProcessSingleMissionAsync(
        IMissionQueueService queueService,
        IHttpClientFactory httpClientFactory,
        MissionServiceOptions missionOptions,
        IExternalApiTokenService tokenService,
        ApplicationDbContext dbContext,
        IJobOptimizationService jobOptimizationService,
        MissionQueue mission,
        string? selectedRobotId,
        CancellationToken cancellationToken)
    {
        MissionHistory? missionHistory = null;

        try
        {
            // Update status to Processing
            await queueService.UpdateStatusAsync(mission.Id, MissionQueueStatus.Processing, cancellationToken: cancellationToken);

            // Assign robot if specified
            if (!string.IsNullOrEmpty(selectedRobotId))
            {
                await queueService.AssignRobotAsync(mission.Id, selectedRobotId, cancellationToken);
            }
            else
            {
                // No specific robot - just mark as Assigned
                await queueService.UpdateStatusAsync(mission.Id, MissionQueueStatus.Assigned, cancellationToken: cancellationToken);
            }

            // Determine trigger source from CreatedBy field
            var triggerSource = MissionTriggerSource.Manual;
            if (mission.CreatedBy?.StartsWith("Scheduler:") == true)
            {
                triggerSource = MissionTriggerSource.Scheduled;
            }

            // Create MissionHistory record
            missionHistory = new MissionHistory
            {
                MissionCode = mission.MissionCode,
                RequestId = mission.RequestId,
                WorkflowName = mission.MissionName,
                SavedMissionId = mission.SavedMissionId,
                TriggerSource = triggerSource,
                Status = "Submitted",
                MissionType = mission.RobotTypeFilter,
                CreatedDate = DateTime.UtcNow,
                ProcessedDate = DateTime.UtcNow,
                SubmittedToAmrDate = DateTime.UtcNow,
                AssignedRobotId = selectedRobotId,
                CreatedBy = mission.CreatedBy
            };

            dbContext.MissionHistories.Add(missionHistory);
            await dbContext.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("✓ MissionHistory created for {MissionCode} (ID={HistoryId})",
                mission.MissionCode, missionHistory.Id);

            // Submit mission to external AMR system
            var submitResult = await SubmitMissionAsync(
                httpClientFactory,
                missionOptions,
                tokenService,
                mission,
                selectedRobotId,
                cancellationToken);

            if (submitResult.Success)
            {
                _logger.LogInformation("Mission {MissionCode} submitted successfully, polling job status...",
                    mission.MissionCode);

                // JOB OPTIMIZATION: If this is a JOBOPTIMIZATION mission, try to reserve next same-map mission
                if (!string.IsNullOrEmpty(selectedRobotId))
                {
                    await TryReserveNextMissionAsync(
                        jobOptimizationService,
                        mission,
                        selectedRobotId,
                        cancellationToken);
                }

                // Poll job status until completion
                var jobResult = await PollJobStatusAsync(
                    httpClientFactory,
                    missionOptions,
                    tokenService,
                    mission.MissionCode,
                    selectedRobotId,
                    cancellationToken);

                if (jobResult.IsCompleted)
                {
                    _logger.LogInformation("✓ Mission {MissionCode} completed with status {Status}",
                        mission.MissionCode, jobResult.FinalStatus);
                    await queueService.UpdateStatusAsync(
                        mission.Id,
                        MissionQueueStatus.Completed,
                        $"Job completed with status {jobResult.FinalStatus}",
                        cancellationToken);

                    // Update MissionHistory to Completed
                    await UpdateMissionHistoryStatusAsync(dbContext, missionHistory.Id, "Completed",
                        jobResult.JobData?.RobotId ?? selectedRobotId, null, cancellationToken);
                }
                else if (jobResult.IsCancelled)
                {
                    _logger.LogInformation("⊘ Mission {MissionCode} was cancelled with status {Status}",
                        mission.MissionCode, jobResult.FinalStatus);
                    await queueService.UpdateStatusAsync(
                        mission.Id,
                        MissionQueueStatus.Cancelled,
                        $"Job cancelled with status {jobResult.FinalStatus}: {jobResult.ErrorMessage}",
                        cancellationToken);

                    // Update MissionHistory to Cancelled
                    await UpdateMissionHistoryStatusAsync(dbContext, missionHistory.Id, "Cancelled",
                        jobResult.JobData?.RobotId ?? selectedRobotId, jobResult.ErrorMessage, cancellationToken);
                }
                else if (jobResult.IsFailed)
                {
                    _logger.LogWarning("✗ Mission {MissionCode} failed with status {Status}",
                        mission.MissionCode, jobResult.FinalStatus);
                    await queueService.UpdateStatusAsync(
                        mission.Id,
                        MissionQueueStatus.Failed,
                        $"Job failed with status {jobResult.FinalStatus}: {jobResult.ErrorMessage}",
                        cancellationToken);

                    // Update MissionHistory to Failed
                    await UpdateMissionHistoryStatusAsync(dbContext, missionHistory.Id, "Failed",
                        jobResult.JobData?.RobotId ?? selectedRobotId, jobResult.ErrorMessage, cancellationToken);
                }
                else if (jobResult.IsWaitingForRobot)
                {
                    _logger.LogInformation("⏳ Mission {MissionCode} is waiting for robot assignment in external AMR system",
                        mission.MissionCode);

                    // Keep queue status as Assigned - the mission is valid, just waiting for a robot
                    // Update MissionHistory to WaitingForRobot status (not an error)
                    await UpdateMissionHistoryStatusAsync(dbContext, missionHistory.Id, "WaitingForRobot",
                        null, "Waiting for robot assignment in external AMR system", cancellationToken);

                    // Note: The WaitingMissionMonitorService will periodically check these missions
                    // and update their status when they complete
                }
                else if (jobResult.IsTimeout)
                {
                    _logger.LogWarning("⏱ Mission {MissionCode} polling timeout - marking as failed",
                        mission.MissionCode);
                    await queueService.UpdateStatusAsync(
                        mission.Id,
                        MissionQueueStatus.Failed,
                        "Job status polling timeout - mission may still be running in external system",
                        cancellationToken);

                    // Update MissionHistory to Timeout
                    await UpdateMissionHistoryStatusAsync(dbContext, missionHistory.Id, "Timeout",
                        selectedRobotId, "Polling timeout", cancellationToken);
                }
            }
            else
            {
                var errorMessage = submitResult.ErrorMessage ?? "Failed to submit to AMR system";
                _logger.LogError("Failed to submit mission {MissionCode} to AMR system: {Error}",
                    mission.MissionCode, errorMessage);
                await queueService.UpdateStatusAsync(
                    mission.Id,
                    MissionQueueStatus.Failed,
                    errorMessage,
                    cancellationToken);

                // Update MissionHistory to Failed with actual error from AMR API
                await UpdateMissionHistoryStatusAsync(dbContext, missionHistory.Id, "Failed",
                    selectedRobotId, errorMessage, cancellationToken);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing mission {MissionCode}", mission.MissionCode);
            try
            {
                await queueService.UpdateStatusAsync(
                    mission.Id,
                    MissionQueueStatus.Failed,
                    ex.Message,
                    cancellationToken);

                // Update MissionHistory if it was created
                if (missionHistory != null)
                {
                    await UpdateMissionHistoryStatusAsync(dbContext, missionHistory.Id, "Failed",
                        selectedRobotId, ex.Message, cancellationToken);
                }
            }
            catch
            {
                // Ignore errors when updating status after failure
            }
        }
    }

    /// <summary>
    /// Try to reserve the next same-map mission for job optimization
    /// Only applies if mission is JOBOPTIMIZATION type
    /// </summary>
    private async Task TryReserveNextMissionAsync(
        IJobOptimizationService jobOptimizationService,
        MissionQueue mission,
        string robotId,
        CancellationToken cancellationToken)
    {
        try
        {
            // Check if this is a JOBOPTIMIZATION mission
            if (!await jobOptimizationService.IsJobOptimizationMissionAsync(mission.SavedMissionId, cancellationToken))
            {
                return; // Not a job optimization mission, skip reservation
            }

            // Get the destination location (where robot will be after completing this mission)
            var destinationLocation = await jobOptimizationService.GetDestinationLocationAsync(mission, cancellationToken);
            if (destinationLocation == null || string.IsNullOrEmpty(destinationLocation.MapCode))
            {
                _logger.LogDebug("Could not determine destination map for mission {MissionCode}, skipping reservation",
                    mission.MissionCode);
                return;
            }

            // Try to reserve the nearest mission in the destination map
            var reserved = await jobOptimizationService.ReserveNextSameMapMissionAsync(
                robotId,
                mission.MissionCode, // The triggering JOBOPTIMIZATION mission
                destinationLocation.MapCode,
                destinationLocation.X ?? 0,
                destinationLocation.Y ?? 0,
                cancellationToken);

            if (reserved != null)
            {
                _logger.LogInformation(
                    "✓ JOB OPTIMIZATION: Reserved mission {ReservedMission} for robot {RobotId} (destination map: {MapCode})",
                    reserved.MissionCode, robotId, destinationLocation.MapCode);
            }
            else
            {
                _logger.LogDebug("No pending missions found in destination map {MapCode} for robot {RobotId}",
                    destinationLocation.MapCode, robotId);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error during job optimization reservation for mission {MissionCode}", mission.MissionCode);
            // Don't fail the main mission processing due to reservation error
        }
    }

    /// <summary>
    /// Update MissionHistory status and completion time
    /// </summary>
    private async Task UpdateMissionHistoryStatusAsync(
        ApplicationDbContext dbContext,
        int historyId,
        string status,
        string? robotId,
        string? errorMessage,
        CancellationToken cancellationToken)
    {
        try
        {
            var history = await dbContext.MissionHistories.FindAsync(new object[] { historyId }, cancellationToken);
            if (history != null)
            {
                history.Status = status;
                history.CompletedDate = DateTime.UtcNow;
                if (!string.IsNullOrEmpty(robotId))
                {
                    history.AssignedRobotId = robotId;
                }
                if (!string.IsNullOrEmpty(errorMessage))
                {
                    history.ErrorMessage = errorMessage.Length > 500 ? errorMessage.Substring(0, 500) : errorMessage;
                }
                await dbContext.SaveChangesAsync(cancellationToken);

                _logger.LogInformation("✓ MissionHistory {HistoryId} updated to {Status}", historyId, status);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to update MissionHistory {HistoryId}", historyId);
        }
    }

    /// <summary>
    /// Submit mission to external AMR system
    /// </summary>
    private async Task<MissionSubmitResult> SubmitMissionAsync(
        IHttpClientFactory httpClientFactory,
        MissionServiceOptions options,
        IExternalApiTokenService tokenService,
        MissionQueue queueItem,
        string? robotId,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(options.SubmitMissionUrl))
        {
            _logger.LogError("SubmitMissionUrl is not configured");
            return MissionSubmitResult.Failed("SubmitMissionUrl is not configured");
        }

        try
        {
            var client = httpClientFactory.CreateClient();

            // AMR endpoints on port 10870 don't require authentication
            client.DefaultRequestHeaders.Clear();
            client.DefaultRequestHeaders.Add("accept", "*/*");
            client.DefaultRequestHeaders.Add("language", "en");
            client.DefaultRequestHeaders.Add("wizards", "FRONT_END");
            // Note: No Authorization header - AMR endpoints don't require auth

            // Parse the stored mission request JSON
            var missionRequest = JsonSerializer.Deserialize<JsonElement>(queueItem.MissionRequestJson);

            // Create modified request with assigned robot
            var requestDict = new Dictionary<string, object>();
            foreach (var prop in missionRequest.EnumerateObject())
            {
                requestDict[prop.Name] = prop.Value;
            }

            // Override robot IDs with selected robot (only if specified)
            if (!string.IsNullOrEmpty(robotId))
            {
                requestDict["robotIds"] = new[] { robotId };
            }

            var response = await client.PostAsJsonAsync(
                options.SubmitMissionUrl,
                requestDict,
                cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogError("Mission submit failed: {Error}", errorContent);
                return MissionSubmitResult.Failed($"HTTP {(int)response.StatusCode}: {errorContent}");
            }

            var result = await response.Content.ReadFromJsonAsync<ApiResponse<object>>(cancellationToken);
            if (result?.Success == true)
            {
                return MissionSubmitResult.Succeeded();
            }

            // API returned success=false with a message
            var apiErrorMessage = result?.Message ?? "AMR API returned success=false";
            _logger.LogError("Mission submit API error: {Error}", apiErrorMessage);
            return MissionSubmitResult.Failed(apiErrorMessage);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error submitting mission {MissionCode}", queueItem.MissionCode);
            return MissionSubmitResult.Failed($"Exception: {ex.Message}");
        }
    }

    /// <summary>
    /// Poll job status until completion, failure, or timeout
    /// </summary>
    private async Task<JobPollResult> PollJobStatusAsync(
        IHttpClientFactory httpClientFactory,
        MissionServiceOptions options,
        IExternalApiTokenService tokenService,
        string missionCode,
        string? robotId,
        CancellationToken cancellationToken)
    {
        const int pollingIntervalMs = 1000; // 1 second
        const int maxAttempts = 120; // 2 minutes max
        const int waitingCheckThreshold = 30; // After 30 seconds, check if job is waiting for robot

        _logger.LogDebug("Starting job status polling for {MissionCode}", missionCode);

        int? lastKnownStatus = null;
        string? lastKnownRobotId = null;

        for (int attempt = 1; attempt <= maxAttempts && !cancellationToken.IsCancellationRequested; attempt++)
        {
            try
            {
                var jobStatus = await QueryJobStatusAsync(httpClientFactory, options, tokenService, missionCode, cancellationToken);

                if (jobStatus != null)
                {
                    lastKnownStatus = jobStatus.Status;
                    lastKnownRobotId = jobStatus.RobotId;

                    // Log every 10 attempts to reduce noise
                    if (attempt % 10 == 0 || attempt == 1)
                    {
                        _logger.LogDebug("Poll #{Attempt}: Job {MissionCode} - Status={Status} ({StatusText}), RobotId={RobotId}",
                            attempt, missionCode, jobStatus.Status, GetJobStatusText(jobStatus.Status), jobStatus.RobotId ?? "null");
                    }

                    // Check if completed (status 5, 30, 35)
                    if (jobStatus.Status == 5 || jobStatus.Status == 30 || jobStatus.Status == 35)
                    {
                        return new JobPollResult
                        {
                            IsCompleted = true,
                            FinalStatus = jobStatus.Status,
                            JobData = jobStatus
                        };
                    }

                    // Check if cancelled (status 31, 32)
                    if (jobStatus.Status == 31 || jobStatus.Status == 32)
                    {
                        _logger.LogInformation("Job {MissionCode} was cancelled (status {Status})", missionCode, jobStatus.Status);
                        return new JobPollResult
                        {
                            IsCancelled = true,
                            FinalStatus = jobStatus.Status,
                            ErrorMessage = jobStatus.WarnCode ?? "Job cancelled",
                            JobData = jobStatus
                        };
                    }

                    // Check if failed (status 99)
                    if (jobStatus.Status == 99)
                    {
                        return new JobPollResult
                        {
                            IsFailed = true,
                            FinalStatus = jobStatus.Status,
                            ErrorMessage = jobStatus.WarnCode ?? "Job failed",
                            JobData = jobStatus
                        };
                    }

                    // Check if job is waiting for robot: status 20 (Executing) but no robot assigned yet
                    // After threshold, if job is still in this state, mark as WaitingForRobot
                    if (attempt >= waitingCheckThreshold && IsWaitingForRobotStatus(jobStatus.Status, jobStatus.RobotId))
                    {
                        _logger.LogInformation(
                            "Job {MissionCode} is waiting for robot assignment (status {Status}, robotId=null) after {Attempt} attempts. " +
                            "Will be monitored by WaitingMissionMonitorService.",
                            missionCode, jobStatus.Status, attempt);
                        return new JobPollResult
                        {
                            IsWaitingForRobot = true,
                            FinalStatus = jobStatus.Status,
                            JobData = jobStatus
                        };
                    }
                }

                await Task.Delay(pollingIntervalMs, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error during polling attempt #{Attempt} for {MissionCode}", attempt, missionCode);
                await Task.Delay(pollingIntervalMs, cancellationToken);
            }
        }

        // If we timed out but the last known status indicates waiting for robot, treat as WaitingForRobot
        if (lastKnownStatus.HasValue && IsWaitingForRobotStatus(lastKnownStatus.Value, lastKnownRobotId))
        {
            _logger.LogInformation(
                "Job {MissionCode} polling ended while waiting for robot (status {Status}, robotId=null). " +
                "Will be monitored by WaitingMissionMonitorService.",
                missionCode, lastKnownStatus.Value);
            return new JobPollResult
            {
                IsWaitingForRobot = true,
                FinalStatus = lastKnownStatus.Value
            };
        }

        _logger.LogWarning("Job status polling timeout for {MissionCode} after {MaxAttempts} attempts (last status: {LastStatus}, robotId: {RobotId})",
            missionCode, maxAttempts, lastKnownStatus?.ToString() ?? "unknown", lastKnownRobotId ?? "null");
        return new JobPollResult { IsTimeout = true };
    }

    /// <summary>
    /// Check if the job is waiting for robot assignment
    /// A job is waiting if it has status 10 (Created), 20 (Executing), or 25 (Waiting) but no robot assigned
    /// </summary>
    private static bool IsWaitingForRobotStatus(int status, string? robotId)
    {
        // Job is waiting for robot if:
        // - Status is 10 (Created), 20 (Executing), or 25 (Waiting)
        // - AND robotId is null or empty (no robot assigned yet)
        var waitingStatuses = new[] { 10, 20, 25 };
        return waitingStatuses.Contains(status) && string.IsNullOrEmpty(robotId);
    }

    /// <summary>
    /// Query job status from external AMR system
    /// </summary>
    private async Task<JobDto?> QueryJobStatusAsync(
        IHttpClientFactory httpClientFactory,
        MissionServiceOptions options,
        IExternalApiTokenService tokenService,
        string missionCode,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(options.JobQueryUrl))
        {
            return null;
        }

        try
        {
            var client = httpClientFactory.CreateClient();

            // AMR endpoints on port 10870 don't require authentication
            client.DefaultRequestHeaders.Clear();
            client.DefaultRequestHeaders.Add("accept", "*/*");
            client.DefaultRequestHeaders.Add("language", "en");
            client.DefaultRequestHeaders.Add("wizards", "FRONT_END");
            // Note: No Authorization header - AMR endpoints don't require auth

            var request = new JobQueryRequest { JobCode = missionCode, Limit = 1 };

            var response = await client.PostAsJsonAsync(
                options.JobQueryUrl,
                request,
                cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Job query HTTP failed for {MissionCode}: {StatusCode}", missionCode, response.StatusCode);
                return null;
            }

            // Log raw response for debugging
            var rawContent = await response.Content.ReadAsStringAsync(cancellationToken);
            _logger.LogDebug("Job query raw response for {MissionCode}: {Response}", missionCode, rawContent);

            var result = JsonSerializer.Deserialize<JobQueryResponse>(rawContent, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            if (result?.Data == null || result.Data.Count == 0)
            {
                _logger.LogWarning("Job query returned no data for {MissionCode}. Raw response: {Response}",
                    missionCode, rawContent.Length > 500 ? rawContent.Substring(0, 500) : rawContent);
                return null;
            }

            var job = result.Data.FirstOrDefault();
            _logger.LogInformation("Job query for {MissionCode}: Status={Status}, RobotId={RobotId}",
                missionCode, job?.Status, job?.RobotId);

            return job;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error querying job status for {MissionCode}", missionCode);
            return null;
        }
    }

    /// <summary>
    /// Get human-readable job status text
    /// </summary>
    private static string GetJobStatusText(int status)
    {
        return status switch
        {
            0 => "Pending",
            2 => "Running",
            3 => "Paused",
            4 => "Resuming",
            5 => "Completed",
            10 => "Created",
            20 => "Executing",
            25 => "Waiting",
            28 => "Cancelling",
            30 => "Complete",
            31 => "Cancelled",
            32 => "Cancelled (Manual)",  // 32 is also a cancelled status
            50 => "Waiting",
            99 => "Failed",
            _ => $"Unknown({status})"
        };
    }
}

/// <summary>
/// Result of mission submission to external AMR system
/// </summary>
public class MissionSubmitResult
{
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }

    public static MissionSubmitResult Succeeded() => new() { Success = true };
    public static MissionSubmitResult Failed(string errorMessage) => new() { Success = false, ErrorMessage = errorMessage };
}

/// <summary>
/// Result of job status polling
/// </summary>
public class JobPollResult
{
    public bool IsCompleted { get; set; }
    public bool IsCancelled { get; set; }
    public bool IsFailed { get; set; }
    public bool IsTimeout { get; set; }
    /// <summary>
    /// True if the job is waiting for a robot to be assigned in the external AMR system.
    /// This is not an error - the mission is queued and will execute when a robot becomes available.
    /// </summary>
    public bool IsWaitingForRobot { get; set; }
    public int FinalStatus { get; set; }
    public string? ErrorMessage { get; set; }
    public JobDto? JobData { get; set; }
}
