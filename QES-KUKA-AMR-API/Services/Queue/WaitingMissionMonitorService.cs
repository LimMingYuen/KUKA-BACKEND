using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using QES_KUKA_AMR_API.Data;
using QES_KUKA_AMR_API.Models.Jobs;
using QES_KUKA_AMR_API.Options;
using System.Net.Http.Json;
using System.Text.Json;

namespace QES_KUKA_AMR_API.Services.Queue;

/// <summary>
/// Background service that monitors missions with "WaitingForRobot" status.
/// These are missions that were submitted to the external AMR system but are waiting
/// for a robot to become available. This service periodically checks their status
/// and updates MissionHistory when they complete.
/// </summary>
public class WaitingMissionMonitorService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<WaitingMissionMonitorService> _logger;
    private readonly TimeSpan _checkInterval = TimeSpan.FromSeconds(30);

    public WaitingMissionMonitorService(
        IServiceProvider serviceProvider,
        ILogger<WaitingMissionMonitorService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("WaitingMissionMonitorService started - monitoring missions waiting for robot assignment");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await CheckWaitingMissionsAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking waiting missions");
            }

            try
            {
                await Task.Delay(_checkInterval, stoppingToken);
            }
            catch (TaskCanceledException)
            {
                break;
            }
        }

        _logger.LogInformation("WaitingMissionMonitorService stopped");
    }

    /// <summary>
    /// Check all missions with WaitingForRobot status and update their status
    /// </summary>
    private async Task CheckWaitingMissionsAsync(CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var httpClientFactory = scope.ServiceProvider.GetRequiredService<IHttpClientFactory>();
        var missionOptions = scope.ServiceProvider.GetRequiredService<IOptions<MissionServiceOptions>>().Value;

        // Find all missions with WaitingForRobot status
        var waitingMissions = await dbContext.MissionHistories
            .Where(m => m.Status == "WaitingForRobot")
            .ToListAsync(cancellationToken);

        if (waitingMissions.Count == 0)
        {
            return; // No waiting missions to check
        }

        _logger.LogDebug("Checking {Count} mission(s) waiting for robot assignment", waitingMissions.Count);

        foreach (var mission in waitingMissions)
        {
            try
            {
                await CheckAndUpdateMissionAsync(dbContext, httpClientFactory, missionOptions, mission, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error checking waiting mission {MissionCode}", mission.MissionCode);
            }
        }
    }

    /// <summary>
    /// Check a single waiting mission and update its status if it has changed
    /// </summary>
    private async Task CheckAndUpdateMissionAsync(
        ApplicationDbContext dbContext,
        IHttpClientFactory httpClientFactory,
        MissionServiceOptions options,
        Data.Entities.MissionHistory mission,
        CancellationToken cancellationToken)
    {
        var jobStatus = await QueryJobStatusAsync(httpClientFactory, options, mission.MissionCode, cancellationToken);

        if (jobStatus == null)
        {
            _logger.LogDebug("No status returned for waiting mission {MissionCode}", mission.MissionCode);
            return;
        }

        // Check if completed (status 30 = Complete, 35 = ManualComplete)
        if (jobStatus.Status == 30 || jobStatus.Status == 35)
        {
            _logger.LogInformation("✓ Waiting mission {MissionCode} has completed (status {Status}, robot {RobotId})",
                mission.MissionCode, jobStatus.Status, jobStatus.RobotId);

            mission.Status = "Completed";
            mission.CompletedDate = DateTime.UtcNow;
            mission.AssignedRobotId = jobStatus.RobotId;
            mission.ErrorMessage = null;

            await dbContext.SaveChangesAsync(cancellationToken);

            // Also update the MissionQueue if it exists
            await UpdateMissionQueueStatusAsync(dbContext, mission.MissionCode, Data.Entities.MissionQueueStatus.Completed, cancellationToken);
        }
        // Check if cancelled (status 31)
        else if (jobStatus.Status == 31)
        {
            _logger.LogInformation("⊘ Waiting mission {MissionCode} was cancelled (status {Status})",
                mission.MissionCode, jobStatus.Status);

            mission.Status = "Cancelled";
            mission.CompletedDate = DateTime.UtcNow;
            mission.AssignedRobotId = jobStatus.RobotId;
            mission.ErrorMessage = jobStatus.WarnCode ?? "Cancelled in external system";

            await dbContext.SaveChangesAsync(cancellationToken);

            await UpdateMissionQueueStatusAsync(dbContext, mission.MissionCode, Data.Entities.MissionQueueStatus.Cancelled, cancellationToken);
        }
        // Check if failed (status 60)
        else if (jobStatus.Status == 60)
        {
            _logger.LogWarning("✗ Waiting mission {MissionCode} failed (status {Status})",
                mission.MissionCode, jobStatus.Status);

            mission.Status = "Failed";
            mission.CompletedDate = DateTime.UtcNow;
            mission.AssignedRobotId = jobStatus.RobotId;
            mission.ErrorMessage = jobStatus.WarnCode ?? "Failed in external system";

            await dbContext.SaveChangesAsync(cancellationToken);

            await UpdateMissionQueueStatusAsync(dbContext, mission.MissionCode, Data.Entities.MissionQueueStatus.Failed, cancellationToken);
        }
        // Check if robot has been assigned (status 20 with robotId)
        else if (jobStatus.Status == 20 && !string.IsNullOrEmpty(jobStatus.RobotId))
        {
            _logger.LogInformation("→ Waiting mission {MissionCode} now executing on robot {RobotId}",
                mission.MissionCode, jobStatus.RobotId);

            // Update status to show it's now executing (not just waiting)
            mission.Status = "Executing";
            mission.AssignedRobotId = jobStatus.RobotId;
            mission.ErrorMessage = null;

            await dbContext.SaveChangesAsync(cancellationToken);
        }
        else
        {
            // Still waiting - log at debug level
            _logger.LogDebug("Mission {MissionCode} still waiting (status {Status}, robotId={RobotId})",
                mission.MissionCode, jobStatus.Status, jobStatus.RobotId ?? "null");
        }
    }

    /// <summary>
    /// Update the corresponding MissionQueue record status
    /// </summary>
    private async Task UpdateMissionQueueStatusAsync(
        ApplicationDbContext dbContext,
        string missionCode,
        Data.Entities.MissionQueueStatus status,
        CancellationToken cancellationToken)
    {
        var queueItem = await dbContext.MissionQueues
            .FirstOrDefaultAsync(q => q.MissionCode == missionCode, cancellationToken);

        if (queueItem != null)
        {
            queueItem.Status = status;
            queueItem.CompletedUtc = DateTime.UtcNow;
            await dbContext.SaveChangesAsync(cancellationToken);

            _logger.LogDebug("Updated MissionQueue {MissionCode} to {Status}", missionCode, status);
        }
    }

    /// <summary>
    /// Query job status from external AMR system
    /// </summary>
    private async Task<JobDto?> QueryJobStatusAsync(
        IHttpClientFactory httpClientFactory,
        MissionServiceOptions options,
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

            var request = new JobQueryRequest { JobCode = missionCode, Limit = 1 };

            var response = await client.PostAsJsonAsync(
                options.JobQueryUrl,
                request,
                cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                return null;
            }

            var rawContent = await response.Content.ReadAsStringAsync(cancellationToken);
            var result = JsonSerializer.Deserialize<JobQueryResponse>(rawContent, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            return result?.Data?.FirstOrDefault();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error querying job status for {MissionCode}", missionCode);
            return null;
        }
    }
}
