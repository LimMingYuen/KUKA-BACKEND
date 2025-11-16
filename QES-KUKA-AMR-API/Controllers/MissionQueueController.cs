using Microsoft.AspNetCore.Mvc;
using QES_KUKA_AMR_API.Data.Entities;
using QES_KUKA_AMR_API.Services.Queue;

namespace QES_KUKA_AMR_API.Controllers;

[ApiController]
[Route("api/mission-queue")]
public class MissionQueueController : ControllerBase
{
    private readonly IMapCodeQueueManager _queueManager;
    private readonly ILogger<MissionQueueController> _logger;

    public MissionQueueController(
        IMapCodeQueueManager queueManager,
        ILogger<MissionQueueController> logger)
    {
        _queueManager = queueManager;
        _logger = logger;
    }

    /// <summary>
    /// Get all pending jobs for a specific MapCode
    /// </summary>
    [HttpGet("pending/{mapCode}")]
    public async Task<ActionResult<List<MissionQueueItem>>> GetPendingJobsAsync(
        string mapCode,
        [FromQuery] int limit = 100,
        CancellationToken cancellationToken = default)
    {
        var jobs = await _queueManager.GetPendingJobsAsync(mapCode, limit, cancellationToken);
        return Ok(jobs);
    }

    /// <summary>
    /// Get all pending jobs across all maps
    /// </summary>
    [HttpGet("pending")]
    public async Task<ActionResult<object>> GetAllPendingJobsAsync(CancellationToken cancellationToken = default)
    {
        // This would require extending IMapCodeQueueManager with a GetAllPendingJobsAsync method
        // For now, return a message
        return Ok(new { message = "Use GET /api/mission-queue/pending/{mapCode} to get jobs for a specific map" });
    }

    /// <summary>
    /// Get queue item by ID
    /// </summary>
    [HttpGet("{queueItemId:int}")]
    public async Task<ActionResult<MissionQueueItem>> GetQueueItemAsync(
        int queueItemId,
        CancellationToken cancellationToken = default)
    {
        var queueItem = await _queueManager.GetQueueItemByIdAsync(queueItemId, cancellationToken);

        if (queueItem == null)
        {
            return NotFound(new { message = $"Queue item {queueItemId} not found" });
        }

        return Ok(queueItem);
    }

    /// <summary>
    /// Get queue item by mission code
    /// </summary>
    [HttpGet("by-mission/{missionCode}")]
    public async Task<ActionResult<MissionQueueItem>> GetQueueItemByMissionCodeAsync(
        string missionCode,
        CancellationToken cancellationToken = default)
    {
        var queueItem = await _queueManager.GetQueueItemByMissionCodeAsync(missionCode, cancellationToken);

        if (queueItem == null)
        {
            return NotFound(new { message = $"Queue item with mission code {missionCode} not found" });
        }

        return Ok(queueItem);
    }

    /// <summary>
    /// Cancel a queued job
    /// </summary>
    [HttpPost("{queueItemId:int}/cancel")]
    public async Task<ActionResult> CancelJobAsync(
        int queueItemId,
        [FromBody] CancelJobRequest request,
        CancellationToken cancellationToken = default)
    {
        var queueItem = await _queueManager.GetQueueItemByIdAsync(queueItemId, cancellationToken);

        if (queueItem == null)
        {
            return NotFound(new { message = $"Queue item {queueItemId} not found" });
        }

        if (queueItem.Status == MissionQueueStatus.Completed ||
            queueItem.Status == MissionQueueStatus.Cancelled)
        {
            return BadRequest(new { message = $"Cannot cancel job with status {queueItem.Status}" });
        }

        await _queueManager.CancelJobAsync(queueItemId, request.Reason ?? "Cancelled by user", cancellationToken);

        return Ok(new { message = "Job cancelled successfully" });
    }

    /// <summary>
    /// Update job status manually (for testing/debugging)
    /// </summary>
    [HttpPost("{queueItemId:int}/status")]
    public async Task<ActionResult> UpdateJobStatusAsync(
        int queueItemId,
        [FromBody] UpdateStatusRequest request,
        CancellationToken cancellationToken = default)
    {
        var queueItem = await _queueManager.GetQueueItemByIdAsync(queueItemId, cancellationToken);

        if (queueItem == null)
        {
            return NotFound(new { message = $"Queue item {queueItemId} not found" });
        }

        await _queueManager.UpdateJobStatusAsync(queueItemId, request.Status, cancellationToken);

        return Ok(new { message = $"Job status updated to {request.Status}" });
    }
}

public class CancelJobRequest
{
    public string? Reason { get; set; }
}

public class UpdateStatusRequest
{
    public MissionQueueStatus Status { get; set; }
}
