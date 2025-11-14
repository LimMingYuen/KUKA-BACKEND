using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QES_KUKA_AMR_API.Data;
using QES_KUKA_AMR_API.Data.Entities;
using QES_KUKA_AMR_API.Services;

namespace QES_KUKA_AMR_API.Controllers;

[ApiController]
[Route("api/mission-queue")]
public class MissionQueueController : ControllerBase
{
    private readonly IQueueService _queueService;
    private readonly ApplicationDbContext _context;
    private readonly ILogger<MissionQueueController> _logger;

    public MissionQueueController(
        IQueueService queueService,
        ApplicationDbContext context,
        ILogger<MissionQueueController> logger)
    {
        _queueService = queueService;
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// Get all missions in the queue (all statuses)
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<IEnumerable<MissionQueue>>> GetAllAsync(
        [FromQuery] QueueStatus? status = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var query = _context.MissionQueues.AsQueryable();

            if (status.HasValue)
            {
                query = query.Where(m => m.Status == status.Value);
            }

            var missions = await query
                .OrderByDescending(m => m.CreatedDate)
                .ToListAsync(cancellationToken);

            return Ok(missions);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving mission queue");
            return StatusCode(500, new { message = "Error retrieving mission queue" });
        }
    }

    /// <summary>
    /// Get queue statistics
    /// </summary>
    [HttpGet("stats")]
    public async Task<ActionResult<QueueStats>> GetStatsAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var stats = await _queueService.GetQueueStatsAsync(cancellationToken);
            return Ok(stats);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving queue stats");
            return StatusCode(500, new { message = "Error retrieving queue stats" });
        }
    }

    /// <summary>
    /// Get only queued missions (ordered by priority and creation date)
    /// </summary>
    [HttpGet("queued")]
    public async Task<ActionResult<IEnumerable<MissionQueue>>> GetQueuedAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var missions = await _queueService.GetQueuedMissionsAsync(cancellationToken);
            return Ok(missions);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving queued missions");
            return StatusCode(500, new { message = "Error retrieving queued missions" });
        }
    }

    /// <summary>
    /// Enqueue a new mission
    /// </summary>
    [HttpPost("enqueue")]
    public async Task<ActionResult<QueueResult>> EnqueueAsync(
        [FromBody] EnqueueRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Enqueueing mission: {MissionCode}, WorkflowId: {WorkflowId}, Priority: {Priority}",
                request.MissionCode, request.WorkflowId, request.Priority);

            var result = await _queueService.EnqueueMissionAsync(request, cancellationToken);

            if (!result.Success)
            {
                return BadRequest(result);
            }

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error enqueueing mission");
            return StatusCode(500, new { message = "Error enqueueing mission", error = ex.Message });
        }
    }

    /// <summary>
    /// Process next queued mission manually
    /// </summary>
    [HttpPost("process-next")]
    public async Task<ActionResult> ProcessNextAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var processed = await _queueService.ProcessNextAsync(cancellationToken);

            if (!processed)
            {
                return Ok(new { message = "No missions to process or no available slots" });
            }

            return Ok(new { message = "Next mission moved to processing" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing next mission");
            return StatusCode(500, new { message = "Error processing next mission" });
        }
    }

    /// <summary>
    /// Update priority of a queued mission
    /// </summary>
    [HttpPut("{queueId}/priority")]
    public async Task<ActionResult> UpdatePriorityAsync(
        int queueId,
        [FromBody] UpdatePriorityRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var success = await _queueService.UpdatePriorityAsync(queueId, request.Priority, cancellationToken);

            if (!success)
            {
                return NotFound(new { message = $"Queue item {queueId} not found or cannot update priority" });
            }

            return Ok(new { message = "Priority updated successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating priority for queue item {QueueId}", queueId);
            return StatusCode(500, new { message = "Error updating priority" });
        }
    }

    /// <summary>
    /// Update priority using smart assignment (high/medium/low)
    /// </summary>
    [HttpPut("{queueId}/priority/smart")]
    public async Task<ActionResult<UpdateSmartPriorityResponse>> UpdateSmartPriorityAsync(
        int queueId,
        [FromBody] UpdateSmartPriorityRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Get smart priority value
            var assignedPriority = await _queueService.GetSmartPriorityAsync(request.PriorityLevel, cancellationToken);

            // Update the mission priority
            var success = await _queueService.UpdatePriorityAsync(queueId, assignedPriority, cancellationToken);

            if (!success)
            {
                return NotFound(new { message = $"Queue item {queueId} not found or cannot update priority" });
            }

            var response = new UpdateSmartPriorityResponse
            {
                AssignedPriority = assignedPriority,
                Message = $"Priority set to {assignedPriority} ({request.PriorityLevel})"
            };

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating smart priority for queue item {QueueId}", queueId);
            return StatusCode(500, new { message = "Error updating priority" });
        }
    }

    /// <summary>
    /// Remove a queued mission
    /// </summary>
    [HttpDelete("{queueId}")]
    public async Task<ActionResult> RemoveQueuedAsync(int queueId, CancellationToken cancellationToken = default)
    {
        try
        {
            var success = await _queueService.RemoveQueuedMissionAsync(queueId, cancellationToken);

            if (!success)
            {
                return NotFound(new { message = $"Queue item {queueId} not found or cannot be removed" });
            }

            return Ok(new { message = "Mission removed from queue successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing queue item {QueueId}", queueId);
            return StatusCode(500, new { message = "Error removing mission from queue" });
        }
    }

    /// <summary>
    /// Cancel a processing mission
    /// </summary>
    [HttpPost("{queueId}/cancel")]
    public async Task<ActionResult> CancelProcessingAsync(int queueId, CancellationToken cancellationToken = default)
    {
        try
        {
            var success = await _queueService.CancelProcessingMissionAsync(queueId, cancellationToken);

            if (!success)
            {
                return NotFound(new { message = $"Queue item {queueId} not found or cannot be cancelled" });
            }

            return Ok(new { message = "Mission cancelled successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error cancelling queue item {QueueId}", queueId);
            return StatusCode(500, new { message = "Error cancelling mission" });
        }
    }

    /// <summary>
    /// Mark a mission as completed (called by external systems or polling)
    /// </summary>
    [HttpPost("complete")]
    public async Task<ActionResult> CompleteAsync(
        [FromBody] CompleteMissionRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Completing mission: {MissionCode}, Success: {Success}",
                request.MissionCode, request.Success);

            await _queueService.OnMissionCompletedAsync(
                request.MissionCode,
                request.Success,
                request.ErrorMessage,
                cancellationToken);

            return Ok(new { message = "Mission completion processed" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error completing mission {MissionCode}", request.MissionCode);
            return StatusCode(500, new { message = "Error processing mission completion" });
        }
    }

    /// <summary>
    /// Clear completed, failed, and cancelled missions
    /// </summary>
    [HttpDelete("clear-completed")]
    public async Task<ActionResult> ClearCompletedAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var toRemove = await _context.MissionQueues
                .Where(m => m.Status == QueueStatus.Completed ||
                           m.Status == QueueStatus.Failed ||
                           m.Status == QueueStatus.Cancelled)
                .ToListAsync(cancellationToken);

            _context.MissionQueues.RemoveRange(toRemove);
            var deletedCount = await _context.SaveChangesAsync(cancellationToken);

            // Check if the table is now empty
            var remainingCount = await _context.MissionQueues.CountAsync(cancellationToken);

            if (remainingCount == 0)
            {
                try
                {
                    // Reset identity seed to start from 1 on next insert
                    // Step 1: Check current identity value
                    await _context.Database.ExecuteSqlRawAsync(
                        "DBCC CHECKIDENT ('MissionQueues', NORESEED);",
                        cancellationToken);

                    // Step 2: Force reseed to 0 (next insert will get 1)
                    await _context.Database.ExecuteSqlRawAsync(
                        "DBCC CHECKIDENT ('MissionQueues', RESEED, 0);",
                        cancellationToken);

                    _logger.LogInformation("Reset MissionQueue identity seed to 0 (next ID will be 1, table is empty)");
                }
                catch (Exception reseedEx)
                {
                    _logger.LogWarning(reseedEx, "Could not reset identity seed, but this is non-critical");
                }
            }

            _logger.LogInformation("Cleared {Count} completed/failed/cancelled missions from queue", deletedCount);

            return Ok(new { message = $"{deletedCount} missions cleared successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error clearing completed missions");
            return StatusCode(500, new { message = "Error clearing completed missions" });
        }
    }

    /// <summary>
    /// Mark a mission as submitted to AMR system
    /// </summary>
    [HttpPut("{queueId}/mark-submitted")]
    public async Task<ActionResult> MarkAsSubmittedAsync(int queueId, CancellationToken cancellationToken = default)
    {
        try
        {
            var queueItem = await _context.MissionQueues
                .FirstOrDefaultAsync(m => m.Id == queueId, cancellationToken);

            if (queueItem == null)
            {
                return NotFound(new { message = $"Queue item {queueId} not found" });
            }

            queueItem.SubmittedToAmr = true;
            queueItem.SubmittedToAmrDate = DateTime.UtcNow;
            queueItem.AmrSubmissionError = null;

            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("✓ Queue item {QueueId} (Mission: {MissionCode}) marked as submitted to AMR",
                queueId, queueItem.MissionCode);

            return Ok(new { message = "Mission marked as submitted successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error marking queue item {QueueId} as submitted", queueId);
            return StatusCode(500, new { message = "Error marking mission as submitted" });
        }
    }

    /// <summary>
    /// Get a specific queue item by ID
    /// </summary>
    [HttpGet("{queueId}")]
    public async Task<ActionResult<MissionQueue>> GetByIdAsync(int queueId, CancellationToken cancellationToken = default)
    {
        try
        {
            var queueItem = await _context.MissionQueues
                .FirstOrDefaultAsync(m => m.Id == queueId, cancellationToken);

            if (queueItem == null)
            {
                return NotFound(new { message = $"Queue item {queueId} not found" });
            }

            return Ok(queueItem);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving queue item {QueueId}", queueId);
            return StatusCode(500, new { message = "Error retrieving queue item" });
        }
    }

    /// <summary>
    /// Get a queue item by mission code (any status)
    /// </summary>
    [HttpGet("by-mission/{missionCode}")]
    public async Task<ActionResult<MissionQueue>> GetByMissionCodeAsync(
        string missionCode,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(missionCode))
        {
            return BadRequest(new { message = "Mission code must be provided." });
        }

        try
        {
            var queueItem = await _context.MissionQueues
                .AsNoTracking()
                .FirstOrDefaultAsync(m => m.MissionCode == missionCode, cancellationToken);

            if (queueItem == null)
            {
                return NotFound(new { message = $"Mission '{missionCode}' not found in queue." });
            }

            return Ok(queueItem);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving queue item for mission {MissionCode}", missionCode);
            return StatusCode(500, new { message = "Error retrieving queue item" });
        }
    }

    [HttpGet("by-workflow/{workflowId:int}")]
    public async Task<IActionResult> GetByWorkflowAsync(int workflowId, CancellationToken cancellationToken = default)
    {
        try
        {
            var queueItem = await _context.MissionQueues
                .AsNoTracking()
                .Where(m => m.WorkflowId == workflowId &&
                            (m.Status == QueueStatus.Queued || m.Status == QueueStatus.Processing))
                .OrderBy(m => m.CreatedDate)
                .FirstOrDefaultAsync(cancellationToken);

            if (queueItem is null)
            {
                return NotFound(new { message = $"No active queue item found for workflow {workflowId}." });
            }

            var statusName = queueItem.Status switch
            {
                QueueStatus.Queued => "Queued",
                QueueStatus.Processing => "Processing",
                _ => queueItem.Status.ToString()
            };

            var response = new
            {
                queueItem.Id,
                queueItem.WorkflowId,
                queueItem.MissionCode,
                queueItem.RequestId,
                queueItem.CreatedDate,
                queueItem.Priority,
                queueItem.Status,
                StatusName = statusName,
                queueItem.CreatedBy,
                queueItem.TriggerSource,
                TriggerSourceName = queueItem.TriggerSource.ToString(),
                IsScheduler = string.Equals(queueItem.CreatedBy, "Scheduler", StringComparison.OrdinalIgnoreCase)
            };

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving queue item for workflow {WorkflowId}", workflowId);
            return StatusCode(500, new { message = "Error retrieving queue item" });
        }
    }
}

// Request Models
public class UpdatePriorityRequest
{
    /// <summary>
    /// Priority value (0-10). Lower values = higher priority.
    /// </summary>
    [Range(0, 10, ErrorMessage = "Priority must be between 0 and 10")]
    public int Priority { get; set; }
}

public class UpdateSmartPriorityRequest
{
    /// <summary>
    /// Priority level: "high" (0-3), "medium" (5), or "low" (8-10)
    /// </summary>
    [Required(ErrorMessage = "PriorityLevel is required")]
    [RegularExpression("^(high|medium|low)$", ErrorMessage = "PriorityLevel must be 'high', 'medium', or 'low'")]
    public string PriorityLevel { get; set; } = string.Empty;
}

public class UpdateSmartPriorityResponse
{
    public int AssignedPriority { get; set; }
    public string Message { get; set; } = string.Empty;
}

public class CompleteMissionRequest
{
    public string MissionCode { get; set; } = string.Empty;
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
}
