using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using QES_KUKA_AMR_API.Data;
using QES_KUKA_AMR_API.Data.Entities;
using QES_KUKA_AMR_API.Hubs;
using QES_KUKA_AMR_API.Models.Missions;
using QES_KUKA_AMR_API.Options;
using QES_KUKA_AMR_API.Services.Auth;
using System.Net.Http.Json;

namespace QES_KUKA_AMR_API.Services.Queue;

public interface IMissionQueueService
{
    Task<List<MissionQueue>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<List<MissionQueue>> GetQueuedAsync(CancellationToken cancellationToken = default);
    Task<MissionQueue?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<MissionQueue?> GetByMissionCodeAsync(string missionCode, CancellationToken cancellationToken = default);
    Task<MissionQueue> AddToQueueAsync(MissionQueue queueItem, CancellationToken cancellationToken = default);
    Task<MissionQueue?> UpdateStatusAsync(int id, MissionQueueStatus status, string? errorMessage = null, CancellationToken cancellationToken = default);
    Task<MissionQueue?> AssignRobotAsync(int id, string robotId, CancellationToken cancellationToken = default);
    Task<bool> CancelAsync(int id, CancellationToken cancellationToken = default);
    Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default);
    Task<MissionQueue?> RetryAsync(int id, CancellationToken cancellationToken = default);
    Task<bool> MoveUpAsync(int id, CancellationToken cancellationToken = default);
    Task<bool> MoveDownAsync(int id, CancellationToken cancellationToken = default);
    Task<MissionQueue?> ChangePriorityAsync(int id, int newPriority, CancellationToken cancellationToken = default);
    Task<MissionQueueStatistics> GetStatisticsAsync(CancellationToken cancellationToken = default);
    Task<MissionQueue?> GetNextQueuedItemAsync(CancellationToken cancellationToken = default);
    Task UpdateQueuePositionsAsync(CancellationToken cancellationToken = default);
    /// <summary>
    /// Get count of active mission instances for a saved template
    /// </summary>
    Task<int> GetActiveInstanceCountAsync(int savedMissionId, CancellationToken cancellationToken = default);
}

/// <summary>
/// Exception thrown when concurrency validation fails
/// </summary>
public class ConcurrencyViolationException : Exception
{
    public ConcurrencyViolationException(string message) : base(message)
    {
    }
}

public class MissionQueueStatistics
{
    public int TotalQueued { get; set; }
    public int TotalProcessing { get; set; }
    public int TotalAssigned { get; set; }
    public int TotalCompleted { get; set; }
    public int TotalFailed { get; set; }
    public int TotalCancelled { get; set; }
    public double AverageWaitTimeSeconds { get; set; }
    public double SuccessRate { get; set; }
}

public class MissionQueueService : IMissionQueueService
{
    private readonly ApplicationDbContext _dbContext;
    private readonly TimeProvider _timeProvider;
    private readonly ILogger<MissionQueueService> _logger;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IServiceProvider _serviceProvider;
    private readonly MissionServiceOptions _missionOptions;
    private readonly IQueueNotificationService _notificationService;

    public MissionQueueService(
        ApplicationDbContext dbContext,
        TimeProvider timeProvider,
        ILogger<MissionQueueService> logger,
        IHttpClientFactory httpClientFactory,
        IServiceProvider serviceProvider,
        IOptions<MissionServiceOptions> missionOptions,
        IQueueNotificationService notificationService)
    {
        _dbContext = dbContext;
        _timeProvider = timeProvider;
        _logger = logger;
        _httpClientFactory = httpClientFactory;
        _serviceProvider = serviceProvider;
        _missionOptions = missionOptions.Value;
        _notificationService = notificationService;
    }

    public async Task<List<MissionQueue>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _dbContext.MissionQueues
            .AsNoTracking()
            .OrderBy(q => q.Status)
            .ThenBy(q => q.Priority)
            .ThenBy(q => q.CreatedUtc)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<MissionQueue>> GetQueuedAsync(CancellationToken cancellationToken = default)
    {
        return await _dbContext.MissionQueues
            .AsNoTracking()
            .Where(q => q.Status == MissionQueueStatus.Queued || q.Status == MissionQueueStatus.Processing)
            .OrderBy(q => q.Priority)
            .ThenBy(q => q.CreatedUtc)
            .ToListAsync(cancellationToken);
    }

    public async Task<MissionQueue?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        return await _dbContext.MissionQueues
            .AsNoTracking()
            .FirstOrDefaultAsync(q => q.Id == id, cancellationToken);
    }

    public async Task<MissionQueue?> GetByMissionCodeAsync(string missionCode, CancellationToken cancellationToken = default)
    {
        return await _dbContext.MissionQueues
            .AsNoTracking()
            .FirstOrDefaultAsync(q => q.MissionCode == missionCode, cancellationToken);
    }

    public async Task<MissionQueue> AddToQueueAsync(MissionQueue queueItem, CancellationToken cancellationToken = default)
    {
        // Check concurrency mode if this is from a saved template
        if (queueItem.SavedMissionId.HasValue)
        {
            var savedMission = await _dbContext.SavedCustomMissions
                .AsNoTracking()
                .FirstOrDefaultAsync(m => m.Id == queueItem.SavedMissionId.Value, cancellationToken);

            if (savedMission?.ConcurrencyMode == "Wait")
            {
                var activeCount = await GetActiveInstanceCountAsync(savedMission.Id, cancellationToken);
                if (activeCount > 0)
                {
                    _logger.LogWarning("Concurrency violation: Template '{MissionName}' has {ActiveCount} active mission(s)",
                        savedMission.MissionName, activeCount);
                    throw new ConcurrencyViolationException(
                        $"Template '{savedMission.MissionName}' has {activeCount} active mission(s). " +
                        "Please wait for completion before triggering again.");
                }
            }
        }

        queueItem.CreatedUtc = _timeProvider.GetUtcNow().UtcDateTime;
        queueItem.Status = MissionQueueStatus.Queued;

        _dbContext.MissionQueues.Add(queueItem);
        await _dbContext.SaveChangesAsync(cancellationToken);

        // Update queue positions
        await UpdateQueuePositionsAsync(cancellationToken);

        _logger.LogInformation("Mission {MissionCode} added to queue with priority {Priority}",
            queueItem.MissionCode, queueItem.Priority);

        // Notify clients of queue update
        await _notificationService.NotifyQueueUpdatedAsync(cancellationToken);

        return queueItem;
    }

    public async Task<MissionQueue?> UpdateStatusAsync(
        int id,
        MissionQueueStatus status,
        string? errorMessage = null,
        CancellationToken cancellationToken = default)
    {
        var queueItem = await _dbContext.MissionQueues.FindAsync(new object[] { id }, cancellationToken);
        if (queueItem == null) return null;

        var now = _timeProvider.GetUtcNow().UtcDateTime;

        queueItem.Status = status;
        queueItem.ErrorMessage = errorMessage;

        switch (status)
        {
            case MissionQueueStatus.Processing:
                queueItem.ProcessingStartedUtc = now;
                break;
            case MissionQueueStatus.Assigned:
                queueItem.AssignedUtc = now;
                break;
            case MissionQueueStatus.Completed:
            case MissionQueueStatus.Failed:
            case MissionQueueStatus.Cancelled:
                queueItem.CompletedUtc = now;
                break;
        }

        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Mission {MissionCode} status updated to {Status}",
            queueItem.MissionCode, status);

        // Notify clients of status change
        await _notificationService.NotifyMissionStatusChangedAsync(id, status, cancellationToken);

        return queueItem;
    }

    public async Task<MissionQueue?> AssignRobotAsync(int id, string robotId, CancellationToken cancellationToken = default)
    {
        var queueItem = await _dbContext.MissionQueues.FindAsync(new object[] { id }, cancellationToken);
        if (queueItem == null) return null;

        queueItem.AssignedRobotId = robotId;
        queueItem.AssignedUtc = _timeProvider.GetUtcNow().UtcDateTime;
        queueItem.Status = MissionQueueStatus.Assigned;

        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Mission {MissionCode} assigned to robot {RobotId}",
            queueItem.MissionCode, robotId);

        // Notify clients of assignment
        await _notificationService.NotifyMissionStatusChangedAsync(id, MissionQueueStatus.Assigned, cancellationToken);

        return queueItem;
    }

    public async Task<bool> CancelAsync(int id, CancellationToken cancellationToken = default)
    {
        var queueItem = await _dbContext.MissionQueues.FindAsync(new object[] { id }, cancellationToken);
        if (queueItem == null) return false;

        // Cannot cancel if already completed, failed, or cancelled
        if (queueItem.Status == MissionQueueStatus.Completed ||
            queueItem.Status == MissionQueueStatus.Failed ||
            queueItem.Status == MissionQueueStatus.Cancelled)
        {
            _logger.LogWarning("Cannot cancel mission {MissionCode} with status {Status}",
                queueItem.MissionCode, queueItem.Status);
            return false;
        }

        // If mission is Assigned (already submitted to external AMR), call external cancel API first
        if (queueItem.Status == MissionQueueStatus.Assigned)
        {
            _logger.LogInformation("Mission {MissionCode} is Assigned, calling external AMR cancel API...", queueItem.MissionCode);

            var cancelSuccess = await CancelExternalMissionAsync(queueItem.MissionCode, cancellationToken);

            if (!cancelSuccess)
            {
                _logger.LogWarning("Failed to cancel mission {MissionCode} in external AMR system", queueItem.MissionCode);
                // Continue to mark as cancelled in queue anyway
            }
        }

        queueItem.Status = MissionQueueStatus.Cancelled;
        queueItem.CompletedUtc = _timeProvider.GetUtcNow().UtcDateTime;

        await _dbContext.SaveChangesAsync(cancellationToken);
        await UpdateQueuePositionsAsync(cancellationToken);

        _logger.LogInformation("Mission {MissionCode} cancelled", queueItem.MissionCode);

        // Notify clients of cancellation
        await _notificationService.NotifyMissionStatusChangedAsync(id, MissionQueueStatus.Cancelled, cancellationToken);

        return true;
    }

    public async Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var queueItem = await _dbContext.MissionQueues.FindAsync(new object[] { id }, cancellationToken);
        if (queueItem == null) return false;

        // Can only delete items that are Completed, Failed, or Cancelled
        // Cannot delete Queued, Processing, or Assigned (must cancel first)
        if (queueItem.Status == MissionQueueStatus.Queued ||
            queueItem.Status == MissionQueueStatus.Processing ||
            queueItem.Status == MissionQueueStatus.Assigned)
        {
            _logger.LogWarning("Cannot delete mission {MissionCode} with status {Status}. Cancel it first.",
                queueItem.MissionCode, queueItem.Status);
            return false;
        }

        _dbContext.MissionQueues.Remove(queueItem);
        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Mission {MissionCode} deleted from queue", queueItem.MissionCode);

        return true;
    }

    /// <summary>
    /// Cancel mission in external AMR system
    /// </summary>
    private async Task<bool> CancelExternalMissionAsync(string missionCode, CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(_missionOptions.MissionCancelUrl))
        {
            _logger.LogWarning("MissionCancelUrl is not configured");
            return false;
        }

        try
        {
            var client = _httpClientFactory.CreateClient();
            client.DefaultRequestHeaders.Add("accept", "*/*");
            client.DefaultRequestHeaders.Add("language", "en");
            client.DefaultRequestHeaders.Add("wizards", "FRONT_END");

            // Get token
            using var tokenScope = _serviceProvider.CreateScope();
            var tokenService = tokenScope.ServiceProvider.GetRequiredService<IExternalApiTokenService>();
            var token = await tokenService.GetTokenAsync();
            if (!string.IsNullOrEmpty(token))
            {
                client.DefaultRequestHeaders.Add("Authorization", $"Bearer {token}");
            }

            var request = new MissionCancelRequest
            {
                MissionCode = missionCode,
                Reason = "Cancelled by user from queue"
            };

            var response = await client.PostAsJsonAsync(
                _missionOptions.MissionCancelUrl,
                request,
                cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation("✓ Mission {MissionCode} cancelled in external AMR system", missionCode);
                return true;
            }
            else
            {
                _logger.LogWarning("External AMR cancel failed with status {StatusCode}", response.StatusCode);
                return false;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calling external AMR cancel API for {MissionCode}", missionCode);
            return false;
        }
    }

    public async Task<MissionQueue?> RetryAsync(int id, CancellationToken cancellationToken = default)
    {
        var queueItem = await _dbContext.MissionQueues.FindAsync(new object[] { id }, cancellationToken);
        if (queueItem == null) return null;

        // Can only retry failed items
        if (queueItem.Status != MissionQueueStatus.Failed)
        {
            _logger.LogWarning("Cannot retry mission {MissionCode} with status {Status}",
                queueItem.MissionCode, queueItem.Status);
            return null;
        }

        if (queueItem.RetryCount >= queueItem.MaxRetries)
        {
            _logger.LogWarning("Mission {MissionCode} has reached max retries ({MaxRetries})",
                queueItem.MissionCode, queueItem.MaxRetries);
            return null;
        }

        queueItem.Status = MissionQueueStatus.Queued;
        queueItem.RetryCount++;
        queueItem.ErrorMessage = null;
        queueItem.ProcessingStartedUtc = null;
        queueItem.AssignedUtc = null;
        queueItem.CompletedUtc = null;
        queueItem.AssignedRobotId = null;

        await _dbContext.SaveChangesAsync(cancellationToken);
        await UpdateQueuePositionsAsync(cancellationToken);

        _logger.LogInformation("Mission {MissionCode} queued for retry (attempt {RetryCount})",
            queueItem.MissionCode, queueItem.RetryCount);

        return queueItem;
    }

    public async Task<bool> MoveUpAsync(int id, CancellationToken cancellationToken = default)
    {
        var queueItem = await _dbContext.MissionQueues.FindAsync(new object[] { id }, cancellationToken);
        if (queueItem == null || queueItem.Status != MissionQueueStatus.Queued) return false;

        // Get item above (lower position number)
        var itemAbove = await _dbContext.MissionQueues
            .Where(q => q.Status == MissionQueueStatus.Queued && q.QueuePosition < queueItem.QueuePosition)
            .OrderByDescending(q => q.QueuePosition)
            .FirstOrDefaultAsync(cancellationToken);

        if (itemAbove == null) return false;

        // Swap positions
        var tempPosition = queueItem.QueuePosition;
        queueItem.QueuePosition = itemAbove.QueuePosition;
        itemAbove.QueuePosition = tempPosition;

        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Mission {MissionCode} moved up in queue", queueItem.MissionCode);

        return true;
    }

    public async Task<bool> MoveDownAsync(int id, CancellationToken cancellationToken = default)
    {
        var queueItem = await _dbContext.MissionQueues.FindAsync(new object[] { id }, cancellationToken);
        if (queueItem == null || queueItem.Status != MissionQueueStatus.Queued) return false;

        // Get item below (higher position number)
        var itemBelow = await _dbContext.MissionQueues
            .Where(q => q.Status == MissionQueueStatus.Queued && q.QueuePosition > queueItem.QueuePosition)
            .OrderBy(q => q.QueuePosition)
            .FirstOrDefaultAsync(cancellationToken);

        if (itemBelow == null) return false;

        // Swap positions
        var tempPosition = queueItem.QueuePosition;
        queueItem.QueuePosition = itemBelow.QueuePosition;
        itemBelow.QueuePosition = tempPosition;

        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Mission {MissionCode} moved down in queue", queueItem.MissionCode);

        return true;
    }

    public async Task<MissionQueue?> ChangePriorityAsync(int id, int newPriority, CancellationToken cancellationToken = default)
    {
        var queueItem = await _dbContext.MissionQueues.FindAsync(new object[] { id }, cancellationToken);
        if (queueItem == null) return null;

        queueItem.Priority = newPriority;
        await _dbContext.SaveChangesAsync(cancellationToken);
        await UpdateQueuePositionsAsync(cancellationToken);

        _logger.LogInformation("Mission {MissionCode} priority changed to {Priority}",
            queueItem.MissionCode, newPriority);

        return queueItem;
    }

    public async Task<MissionQueueStatistics> GetStatisticsAsync(CancellationToken cancellationToken = default)
    {
        var items = await _dbContext.MissionQueues
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        var completedItems = items.Where(i =>
            i.Status == MissionQueueStatus.Completed ||
            i.Status == MissionQueueStatus.Failed).ToList();

        var waitTimes = items
            .Where(i => i.AssignedUtc.HasValue)
            .Select(i => (i.AssignedUtc!.Value - i.CreatedUtc).TotalSeconds)
            .ToList();

        return new MissionQueueStatistics
        {
            TotalQueued = items.Count(i => i.Status == MissionQueueStatus.Queued),
            TotalProcessing = items.Count(i => i.Status == MissionQueueStatus.Processing),
            TotalAssigned = items.Count(i => i.Status == MissionQueueStatus.Assigned),
            TotalCompleted = items.Count(i => i.Status == MissionQueueStatus.Completed),
            TotalFailed = items.Count(i => i.Status == MissionQueueStatus.Failed),
            TotalCancelled = items.Count(i => i.Status == MissionQueueStatus.Cancelled),
            AverageWaitTimeSeconds = waitTimes.Any() ? waitTimes.Average() : 0,
            SuccessRate = completedItems.Any()
                ? (double)completedItems.Count(i => i.Status == MissionQueueStatus.Completed) / completedItems.Count * 100
                : 0
        };
    }

    public async Task<MissionQueue?> GetNextQueuedItemAsync(CancellationToken cancellationToken = default)
    {
        return await _dbContext.MissionQueues
            .Where(q => q.Status == MissionQueueStatus.Queued)
            .OrderBy(q => q.Priority)
            .ThenBy(q => q.CreatedUtc)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task UpdateQueuePositionsAsync(CancellationToken cancellationToken = default)
    {
        var queuedItems = await _dbContext.MissionQueues
            .Where(q => q.Status == MissionQueueStatus.Queued)
            .OrderBy(q => q.Priority)
            .ThenBy(q => q.CreatedUtc)
            .ToListAsync(cancellationToken);

        for (int i = 0; i < queuedItems.Count; i++)
        {
            queuedItems[i].QueuePosition = i + 1;
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    /// <summary>
    /// Get count of active mission instances for a saved template.
    /// Active means: Queued, Processing, or Assigned status.
    /// </summary>
    public async Task<int> GetActiveInstanceCountAsync(int savedMissionId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.MissionQueues
            .CountAsync(q =>
                q.SavedMissionId == savedMissionId &&
                (q.Status == MissionQueueStatus.Queued ||
                 q.Status == MissionQueueStatus.Processing ||
                 q.Status == MissionQueueStatus.Assigned),
                cancellationToken);
    }
}
