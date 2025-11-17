using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QES_KUKA_AMR_API.Data;
using QES_KUKA_AMR_API.Data.Entities;
using QES_KUKA_AMR_API.Models.Queue;

namespace QES_KUKA_AMR_API.Controllers;

[ApiController]
[Route("api/queue")]
public class QueueController : ControllerBase
{
    private readonly ApplicationDbContext _dbContext;
    private readonly ILogger<QueueController> _logger;
    private readonly TimeProvider _timeProvider;

    public QueueController(
        ApplicationDbContext dbContext,
        ILogger<QueueController> logger,
        TimeProvider timeProvider)
    {
        _dbContext = dbContext;
        _logger = logger;
        _timeProvider = timeProvider;
    }

    /// <summary>
    /// Get status of all queue items for a specific mission code
    /// </summary>
    [HttpGet("status/{missionCode}")]
    public async Task<ActionResult<MissionQueueStatusResponse>> GetMissionStatusAsync(
        string missionCode,
        CancellationToken cancellationToken)
    {
        var queueItems = await _dbContext.MissionQueueItems
            .Where(q => q.MissionCode == missionCode)
            .OrderBy(q => q.EnqueuedUtc)
            .ToListAsync(cancellationToken);

        if (!queueItems.Any())
        {
            return NotFound(new { message = $"No queue items found for mission code: {missionCode}" });
        }

        var overallStatus = DetermineOverallStatus(queueItems);
        var warnings = new List<string>();

        // Check if queue configurations exist for all MapCodes
        var mapCodes = queueItems.Select(q => q.PrimaryMapCode).Distinct().ToList();
        var configuredMapCodes = await _dbContext.MapCodeQueueConfigurations
            .Where(c => mapCodes.Contains(c.MapCode) && c.EnableQueue)
            .Select(c => c.MapCode)
            .ToListAsync(cancellationToken);

        var missingConfigs = mapCodes.Except(configuredMapCodes).ToList();
        if (missingConfigs.Any())
        {
            warnings.Add($"Queue not configured for MapCode(s): {string.Join(", ", missingConfigs)}. Jobs will not be processed.");
        }

        // Determine if ready for job polling (job has been submitted to AMR)
        var readyForJobPolling = queueItems.Any(q =>
            q.Status == MissionQueueStatus.SubmittedToAmr ||
            q.Status == MissionQueueStatus.Executing ||
            q.Status == MissionQueueStatus.Completed);

        // Estimate processing delay (queue scheduler runs every N seconds)
        int? estimatedDelay = null;
        if (queueItems.All(q => q.Status == MissionQueueStatus.Pending || q.Status == MissionQueueStatus.ReadyToAssign))
        {
            // Still in queue, not yet processed - estimate 5-10 seconds (one or two scheduler cycles)
            estimatedDelay = 7; // Average of 5-10 seconds
        }

        var response = new MissionQueueStatusResponse
        {
            MissionCode = missionCode,
            TotalSegments = queueItems.Count,
            QueueItems = queueItems.Select(MapToDto).ToList(),
            OverallStatus = overallStatus,
            Warnings = warnings,
            ReadyForJobPolling = readyForJobPolling,
            EstimatedProcessingDelaySeconds = estimatedDelay
        };

        return Ok(response);
    }

    /// <summary>
    /// List all queue items for a specific MapCode
    /// </summary>
    [HttpGet("mapcode/{mapCode}")]
    public async Task<ActionResult<MapCodeQueueResponse>> GetMapCodeQueueAsync(
        string mapCode,
        [FromQuery] string? status = null,
        [FromQuery] int limit = 100,
        CancellationToken cancellationToken = default)
    {
        var query = _dbContext.MissionQueueItems
            .Where(q => q.PrimaryMapCode == mapCode);

        // Filter by status if provided
        if (!string.IsNullOrEmpty(status) && Enum.TryParse<MissionQueueStatus>(status, true, out var statusEnum))
        {
            query = query.Where(q => q.Status == statusEnum);
        }

        var queueItems = await query
            .OrderByDescending(q => q.Priority)
            .ThenBy(q => q.EnqueuedUtc)
            .Take(limit)
            .ToListAsync(cancellationToken);

        var response = new MapCodeQueueResponse
        {
            MapCode = mapCode,
            QueueItems = queueItems.Select(MapToDto).ToList(),
            PendingCount = queueItems.Count(q => q.Status == MissionQueueStatus.Pending || q.Status == MissionQueueStatus.ReadyToAssign),
            ProcessingCount = queueItems.Count(q => q.Status == MissionQueueStatus.Assigned || q.Status == MissionQueueStatus.SubmittedToAmr || q.Status == MissionQueueStatus.Executing),
            CompletedCount = queueItems.Count(q => q.Status == MissionQueueStatus.Completed)
        };

        return Ok(response);
    }

    /// <summary>
    /// Get queue statistics across all MapCodes
    /// </summary>
    [HttpGet("statistics")]
    public async Task<ActionResult<QueueStatisticsResponse>> GetQueueStatisticsAsync(
        CancellationToken cancellationToken)
    {
        var allItems = await _dbContext.MissionQueueItems.ToListAsync(cancellationToken);

        var mapCodeGroups = allItems.GroupBy(q => q.PrimaryMapCode);

        var mapCodeStats = mapCodeGroups.Select(g => new MapCodeStatistics
        {
            MapCode = g.Key,
            PendingCount = g.Count(q => q.Status == MissionQueueStatus.Pending),
            ReadyToAssignCount = g.Count(q => q.Status == MissionQueueStatus.ReadyToAssign),
            AssignedCount = g.Count(q => q.Status == MissionQueueStatus.Assigned),
            ExecutingCount = g.Count(q => q.Status == MissionQueueStatus.Executing),
            CompletedCount = g.Count(q => q.Status == MissionQueueStatus.Completed),
            FailedCount = g.Count(q => q.Status == MissionQueueStatus.Failed),
            CancelledCount = g.Count(q => q.Status == MissionQueueStatus.Cancelled),
            TotalCount = g.Count()
        }).OrderByDescending(s => s.TotalCount).ToList();

        var response = new QueueStatisticsResponse
        {
            MapCodeStatistics = mapCodeStats,
            TotalPending = allItems.Count(q => q.Status == MissionQueueStatus.Pending || q.Status == MissionQueueStatus.ReadyToAssign),
            TotalProcessing = allItems.Count(q => q.Status == MissionQueueStatus.Assigned || q.Status == MissionQueueStatus.SubmittedToAmr || q.Status == MissionQueueStatus.Executing),
            TotalCompleted = allItems.Count(q => q.Status == MissionQueueStatus.Completed),
            TotalFailed = allItems.Count(q => q.Status == MissionQueueStatus.Failed),
            GeneratedAt = _timeProvider.GetUtcNow().UtcDateTime
        };

        return Ok(response);
    }

    /// <summary>
    /// Get current job assigned to a specific robot
    /// </summary>
    [HttpGet("robot/{robotId}/current")]
    public async Task<ActionResult<RobotCurrentJobResponse>> GetRobotCurrentJobAsync(
        string robotId,
        CancellationToken cancellationToken)
    {
        var currentJob = await _dbContext.MissionQueueItems
            .Where(q => q.AssignedRobotId == robotId)
            .Where(q => q.Status == MissionQueueStatus.Assigned ||
                       q.Status == MissionQueueStatus.SubmittedToAmr ||
                       q.Status == MissionQueueStatus.Executing)
            .OrderByDescending(q => q.RobotAssignedUtc)
            .FirstOrDefaultAsync(cancellationToken);

        var response = new RobotCurrentJobResponse
        {
            RobotId = robotId,
            HasActiveJob = currentJob != null,
            CurrentJob = currentJob != null ? MapToDto(currentJob) : null
        };

        return Ok(response);
    }

    /// <summary>
    /// Cancel a queued mission
    /// </summary>
    [HttpPost("{queueItemId:int}/cancel")]
    public async Task<ActionResult<CancelQueueItemResponse>> CancelQueueItemAsync(
        int queueItemId,
        CancellationToken cancellationToken)
    {
        var queueItem = await _dbContext.MissionQueueItems
            .FindAsync(new object[] { queueItemId }, cancellationToken);

        if (queueItem == null)
        {
            return NotFound(new CancelQueueItemResponse
            {
                Success = false,
                Message = $"Queue item {queueItemId} not found",
                QueueItemId = queueItemId
            });
        }

        // Check if already completed or cancelled
        if (queueItem.Status == MissionQueueStatus.Completed)
        {
            return BadRequest(new CancelQueueItemResponse
            {
                Success = false,
                Message = "Cannot cancel a completed mission",
                QueueItemId = queueItemId
            });
        }

        if (queueItem.Status == MissionQueueStatus.Cancelled)
        {
            return Ok(new CancelQueueItemResponse
            {
                Success = true,
                Message = "Mission already cancelled",
                QueueItemId = queueItemId,
                CancelledUtc = queueItem.CancelledUtc
            });
        }

        // Check if currently executing - may need to call AMR cancel API
        if (queueItem.Status == MissionQueueStatus.Executing || queueItem.Status == MissionQueueStatus.SubmittedToAmr)
        {
            _logger.LogWarning(
                "Cancelling executing job {QueueItemCode} - AMR system may need to be notified",
                queueItem.QueueItemCode
            );
            // TODO: Call AMR mission cancel API here if needed
        }

        // Mark as cancelled
        queueItem.Status = MissionQueueStatus.Cancelled;
        queueItem.CancelledUtc = _timeProvider.GetUtcNow().UtcDateTime;
        queueItem.UpdatedUtc = _timeProvider.GetUtcNow().UtcDateTime;

        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Queue item {QueueItemId} ({QueueItemCode}) cancelled successfully",
            queueItemId,
            queueItem.QueueItemCode
        );

        return Ok(new CancelQueueItemResponse
        {
            Success = true,
            Message = "Mission cancelled successfully",
            QueueItemId = queueItemId,
            CancelledUtc = queueItem.CancelledUtc
        });
    }

    /// <summary>
    /// Get queue item details by ID
    /// </summary>
    [HttpGet("{queueItemId:int}")]
    public async Task<ActionResult<QueueItemStatusDto>> GetQueueItemAsync(
        int queueItemId,
        CancellationToken cancellationToken)
    {
        var queueItem = await _dbContext.MissionQueueItems
            .FindAsync(new object[] { queueItemId }, cancellationToken);

        if (queueItem == null)
        {
            return NotFound(new { message = $"Queue item {queueItemId} not found" });
        }

        return Ok(MapToDto(queueItem));
    }

    // Helper methods

    private static QueueItemStatusDto MapToDto(MissionQueueItem item)
    {
        return new QueueItemStatusDto
        {
            QueueItemId = item.Id,
            QueueItemCode = item.QueueItemCode,
            MissionCode = item.MissionCode,
            Status = item.Status.ToString(),
            Priority = item.Priority,
            PrimaryMapCode = item.PrimaryMapCode,
            AssignedRobotId = item.AssignedRobotId,
            EnqueuedUtc = item.EnqueuedUtc,
            StartedUtc = item.StartedUtc,
            CompletedUtc = item.CompletedUtc,
            CancelledUtc = item.CancelledUtc,
            ErrorMessage = item.ErrorMessage,
            RetryCount = item.RetryCount,
            IsOpportunisticJob = item.IsOpportunisticJob,
            HasNextSegment = item.NextQueueItemId.HasValue
        };
    }

    private static string DetermineOverallStatus(List<MissionQueueItem> queueItems)
    {
        if (queueItems.All(q => q.Status == MissionQueueStatus.Completed))
            return "Completed";

        if (queueItems.Any(q => q.Status == MissionQueueStatus.Failed))
            return "Failed";

        if (queueItems.Any(q => q.Status == MissionQueueStatus.Cancelled))
            return "Cancelled";

        if (queueItems.Any(q => q.Status == MissionQueueStatus.Executing))
            return "Executing";

        if (queueItems.Any(q => q.Status == MissionQueueStatus.SubmittedToAmr))
            return "SubmittedToAmr";

        if (queueItems.Any(q => q.Status == MissionQueueStatus.Assigned))
            return "Assigned";

        return "Pending";
    }
}
