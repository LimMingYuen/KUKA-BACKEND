using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using QES_KUKA_AMR_API.Data;
using QES_KUKA_AMR_API.Data.Entities;
using QES_KUKA_AMR_API.Models.Missions;
using QES_KUKA_AMR_API.Options;

namespace QES_KUKA_AMR_API.Services;

/// <summary>
/// Background service that submits queued Processing missions to the AMR system
/// </summary>
public class MissionSubmitterBackgroundService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<MissionSubmitterBackgroundService> _logger;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly MissionQueueSettings _queueSettings;
    private readonly MissionServiceOptions _missionServiceOptions;
    private readonly TimeSpan _submissionInterval;

    public MissionSubmitterBackgroundService(
        IServiceProvider serviceProvider,
        ILogger<MissionSubmitterBackgroundService> logger,
        IHttpClientFactory httpClientFactory,
        IOptions<MissionQueueSettings> queueSettings,
        IOptions<MissionServiceOptions> missionServiceOptions)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
        _httpClientFactory = httpClientFactory;
        _queueSettings = queueSettings.Value;
        _missionServiceOptions = missionServiceOptions.Value;
        _submissionInterval = TimeSpan.FromSeconds(_queueSettings.MissionSubmissionIntervalSeconds);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("=== MissionSubmitterBackgroundService Starting ===");
        _logger.LogInformation("Submission interval: {Interval} seconds", _queueSettings.MissionSubmissionIntervalSeconds);

        // Wait a bit before starting to ensure all services are ready
        await Task.Delay(TimeSpan.FromSeconds(4), stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await SubmitPendingMissionsAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in mission submitter background service");
            }

            // Wait for the next submission cycle
            await Task.Delay(_submissionInterval, stoppingToken);
        }

        _logger.LogInformation("=== MissionSubmitterBackgroundService Stopping ===");
    }

    private async Task SubmitPendingMissionsAsync(CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var queueService = scope.ServiceProvider.GetRequiredService<IQueueService>();

        try
        {
            // Get all Processing missions that haven't been submitted to AMR yet
            var pendingMissions = await context.MissionQueues
                .Where(m => m.Status == QueueStatus.Processing && !m.SubmittedToAmr)
                .ToListAsync(cancellationToken);

            if (pendingMissions.Count == 0)
            {
                return;
            }

            _logger.LogInformation("Found {Count} processing mission(s) pending AMR submission", pendingMissions.Count);

            var httpClient = _httpClientFactory.CreateClient();
            int submittedCount = 0;
            int failedCount = 0;

            foreach (var mission in pendingMissions)
            {
                try
                {
                    var submitRequest = BuildSubmitMissionRequest(mission);
                    var success = await SubmitToAmrSystemAsync(httpClient, submitRequest, cancellationToken);

                    if (success)
                    {
                        // Update mission to mark as submitted
                        mission.SubmittedToAmr = true;
                        mission.SubmittedToAmrDate = DateTime.UtcNow;
                        mission.AmrSubmissionError = null;
                        try
                        {
                            await context.SaveChangesAsync(cancellationToken);
                            _logger.LogInformation("✓ Mission {MissionCode} submitted to AMR system successfully", mission.MissionCode);
                            submittedCount++;
                        }
                        catch (DbUpdateConcurrencyException ex)
                        {
                            // Mission might have been deleted by another process, ignore
                            _logger.LogInformation("Concurrency exception updating mission {MissionCode}, possibly already deleted: {Message}", mission.MissionCode, ex.Message);
                        }
                    }
                    else
                    {
                        // Submission failed - delete mission from queue and record in history
                        var errorMessage = mission.AmrSubmissionError ?? "Failed to submit to AMR system";

                        _logger.LogWarning("✗ Mission {MissionCode} submission failed: {Error}. Deleting from queue and recording in history.",
                            mission.MissionCode, errorMessage);

                        await queueService.OnMissionCompletedAsync(
                            mission.MissionCode,
                            false,
                            errorMessage,
                            cancellationToken);

                        failedCount++;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error submitting mission {MissionCode} to AMR", mission.MissionCode);

                    // Delete mission from queue and record in history
                    var errorMessage = $"Exception during submission: {ex.Message}";
                    try
                    {
                        // Try to save the error message first
                        mission.AmrSubmissionError = errorMessage;
                        await context.SaveChangesAsync(cancellationToken);
                    }
                    catch (DbUpdateConcurrencyException)
                    {
                        // If this fails, the mission might already be deleted, so we'll still try to record it in history
                        _logger.LogInformation("Mission {MissionCode} already deleted during exception handling", mission.MissionCode);
                    }

                    await queueService.OnMissionCompletedAsync(
                        mission.MissionCode,
                        false,
                        errorMessage,
                        cancellationToken);

                    failedCount++;
                }
            }

            if (submittedCount > 0 || failedCount > 0)
            {
                _logger.LogInformation("AMR submission results - Submitted: {Submitted}, Failed: {Failed}",
                    submittedCount, failedCount);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing pending mission submissions");
        }
    }

    private SubmitMissionRequest BuildSubmitMissionRequest(MissionQueue mission)
    {
        // Deserialize JSON arrays if present
        List<string>? robotModels = null;
        List<string>? robotIds = null;
        List<MissionDataItem>? missionData = null;

        if (!string.IsNullOrWhiteSpace(mission.RobotModels))
        {
            try
            {
                robotModels = JsonSerializer.Deserialize<List<string>>(mission.RobotModels);
            }
            catch (JsonException ex)
            {
                _logger.LogWarning(ex, "Failed to deserialize RobotModels for mission {MissionCode}", mission.MissionCode);
            }
        }

        if (!string.IsNullOrWhiteSpace(mission.RobotIds))
        {
            try
            {
                robotIds = JsonSerializer.Deserialize<List<string>>(mission.RobotIds);
            }
            catch (JsonException ex)
            {
                _logger.LogWarning(ex, "Failed to deserialize RobotIds for mission {MissionCode}", mission.MissionCode);
            }
        }

        if (!string.IsNullOrWhiteSpace(mission.MissionDataJson))
        {
            try
            {
                missionData = JsonSerializer.Deserialize<List<MissionDataItem>>(mission.MissionDataJson);
            }
            catch (JsonException ex)
            {
                _logger.LogWarning(ex, "Failed to deserialize MissionData for mission {MissionCode}", mission.MissionCode);
            }
        }

        return new SubmitMissionRequest
        {
            OrgId = "UNIVERSAL",
            RequestId = mission.RequestId,
            MissionCode = mission.MissionCode,

            // Use custom values if provided, otherwise use defaults
            MissionType = mission.MissionType ?? "RACK_MOVE",
            ViewBoardType = mission.ViewBoardType ?? string.Empty,
            RobotType = mission.RobotType ?? "LIFT",
            Priority = mission.Priority, // Use queue priority
            ContainerModelCode = mission.ContainerModelCode ?? string.Empty,
            ContainerCode = mission.ContainerCode ?? string.Empty,
            TemplateCode = mission.TemplateCode ?? string.Empty,
            LockRobotAfterFinish = mission.LockRobotAfterFinish,
            UnlockMissionCode = mission.UnlockMissionCode ?? string.Empty,
            UnlockRobotId = mission.UnlockRobotId ?? string.Empty,
            IdleNode = mission.IdleNode ?? string.Empty,
            RobotModels = robotModels?.ToArray() ?? Array.Empty<string>(),
            RobotIds = robotIds?.ToArray() ?? Array.Empty<string>(),
            MissionData = missionData
        };
    }

    private async Task<bool> SubmitToAmrSystemAsync(HttpClient httpClient, SubmitMissionRequest request, CancellationToken cancellationToken)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(_missionServiceOptions.SubmitMissionUrl))
            {
                _logger.LogError("SubmitMissionUrl is not configured");
                return false;
            }

            if (!Uri.TryCreate(_missionServiceOptions.SubmitMissionUrl, UriKind.Absolute, out var requestUri))
            {
                _logger.LogError("SubmitMissionUrl is not a valid URI: {Url}", _missionServiceOptions.SubmitMissionUrl);
                return false;
            }

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

            using var response = await httpClient.SendAsync(apiRequest, cancellationToken);

            var serviceResponse = await response.Content.ReadFromJsonAsync<SubmitMissionResponse>(cancellationToken: cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("AMR system returned error status {StatusCode} for mission {MissionCode}",
                    response.StatusCode, request.MissionCode);
                return false;
            }

            if (serviceResponse == null)
            {
                _logger.LogWarning("AMR system returned null response for mission {MissionCode}", request.MissionCode);
                return false;
            }

            if (!serviceResponse.Success)
            {
                _logger.LogWarning("AMR system returned success=false for mission {MissionCode}: {Message}",
                    request.MissionCode, serviceResponse.Message);
                return false;
            }

            return true;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "HTTP error submitting mission {MissionCode} to AMR system", request.MissionCode);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error submitting mission {MissionCode} to AMR system", request.MissionCode);
            return false;
        }
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("MissionSubmitterBackgroundService is stopping");
        await base.StopAsync(cancellationToken);
    }
}
