using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using QES_KUKA_AMR_API.Data.Entities;
using QES_KUKA_AMR_API.Models;
using QES_KUKA_AMR_API.Services.Queue;

namespace QES_KUKA_AMR_API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class MissionQueueController : ControllerBase
{
    private readonly IMissionQueueService _queueService;
    private readonly ILogger<MissionQueueController> _logger;

    public MissionQueueController(
        IMissionQueueService queueService,
        ILogger<MissionQueueController> logger)
    {
        _queueService = queueService;
        _logger = logger;
    }

    /// <summary>
    /// Get all queue items
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<ApiResponse<List<MissionQueueDto>>>> GetAll(CancellationToken cancellationToken)
    {
        try
        {
            var items = await _queueService.GetAllAsync(cancellationToken);
            var dtos = items.Select(MapToDto).ToList();

            return Ok(new ApiResponse<List<MissionQueueDto>>
            {
                Success = true,
                Data = dtos
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting queue items");
            return StatusCode(500, new ApiResponse<List<MissionQueueDto>>
            {
                Success = false,
                Msg = "Failed to get queue items"
            });
        }
    }

    /// <summary>
    /// Get queued items only (status = Queued or Processing)
    /// </summary>
    [HttpGet("queued")]
    public async Task<ActionResult<ApiResponse<List<MissionQueueDto>>>> GetQueued(CancellationToken cancellationToken)
    {
        try
        {
            var items = await _queueService.GetQueuedAsync(cancellationToken);
            var dtos = items.Select(MapToDto).ToList();

            return Ok(new ApiResponse<List<MissionQueueDto>>
            {
                Success = true,
                Data = dtos
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting queued items");
            return StatusCode(500, new ApiResponse<List<MissionQueueDto>>
            {
                Success = false,
                Msg = "Failed to get queued items"
            });
        }
    }

    /// <summary>
    /// Get queue item by ID
    /// </summary>
    [HttpGet("{id}")]
    public async Task<ActionResult<ApiResponse<MissionQueueDto>>> GetById(int id, CancellationToken cancellationToken)
    {
        try
        {
            var item = await _queueService.GetByIdAsync(id, cancellationToken);
            if (item == null)
            {
                return NotFound(new ApiResponse<MissionQueueDto>
                {
                    Success = false,
                    Msg = "Queue item not found"
                });
            }

            return Ok(new ApiResponse<MissionQueueDto>
            {
                Success = true,
                Data = MapToDto(item)
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting queue item {Id}", id);
            return StatusCode(500, new ApiResponse<MissionQueueDto>
            {
                Success = false,
                Msg = "Failed to get queue item"
            });
        }
    }

    /// <summary>
    /// Get queue statistics
    /// </summary>
    [HttpGet("statistics")]
    public async Task<ActionResult<ApiResponse<MissionQueueStatistics>>> GetStatistics(CancellationToken cancellationToken)
    {
        try
        {
            var stats = await _queueService.GetStatisticsAsync(cancellationToken);

            return Ok(new ApiResponse<MissionQueueStatistics>
            {
                Success = true,
                Data = stats
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting queue statistics");
            return StatusCode(500, new ApiResponse<MissionQueueStatistics>
            {
                Success = false,
                Msg = "Failed to get queue statistics"
            });
        }
    }

    /// <summary>
    /// Get count of active mission instances for a saved template
    /// </summary>
    [HttpGet("active-count/{savedMissionId}")]
    public async Task<ActionResult<ApiResponse<int>>> GetActiveCount(int savedMissionId, CancellationToken cancellationToken)
    {
        try
        {
            var count = await _queueService.GetActiveInstanceCountAsync(savedMissionId, cancellationToken);
            return Ok(new ApiResponse<int>
            {
                Success = true,
                Data = count
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting active count for template {SavedMissionId}", savedMissionId);
            return StatusCode(500, new ApiResponse<int>
            {
                Success = false,
                Msg = "Failed to get active instance count"
            });
        }
    }

    /// <summary>
    /// Add mission to queue
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<ApiResponse<MissionQueueDto>>> AddToQueue(
        [FromBody] AddToQueueRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var username = User.Identity?.Name ?? "system";

            var queueItem = new MissionQueue
            {
                MissionCode = request.MissionCode,
                RequestId = request.RequestId,
                SavedMissionId = request.SavedMissionId,
                MissionName = request.MissionName,
                MissionRequestJson = request.MissionRequestJson,
                Priority = request.Priority,
                RobotTypeFilter = request.RobotTypeFilter,
                PreferredRobotIds = request.PreferredRobotIds,
                CreatedBy = username
            };

            var result = await _queueService.AddToQueueAsync(queueItem, cancellationToken);

            return Ok(new ApiResponse<MissionQueueDto>
            {
                Success = true,
                Data = MapToDto(result),
                Msg = $"Mission added to queue at position {result.QueuePosition}"
            });
        }
        catch (ConcurrencyViolationException ex)
        {
            _logger.LogWarning("Concurrency violation: {Message}", ex.Message);
            return Conflict(new ApiResponse<MissionQueueDto>
            {
                Success = false,
                Msg = ex.Message
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding mission to queue");
            return StatusCode(500, new ApiResponse<MissionQueueDto>
            {
                Success = false,
                Msg = "Failed to add mission to queue"
            });
        }
    }

    /// <summary>
    /// Cancel a queued mission with specified cancel mode
    /// </summary>
    /// <param name="id">Queue item ID</param>
    /// <param name="request">Cancel request with mode and optional reason</param>
    /// <param name="cancellationToken">Cancellation token</param>
    [HttpPost("{id}/cancel")]
    public async Task<ActionResult<ApiResponse<object>>> Cancel(
        int id,
        [FromBody] CancelQueueRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Cancel request received for queue item {Id} with mode {CancelMode}, reason: {Reason}",
                id, request.CancelMode, request.Reason);

            var success = await _queueService.CancelAsync(id, request.CancelMode, request.Reason, cancellationToken);
            if (!success)
            {
                return BadRequest(new ApiResponse<object>
                {
                    Success = false,
                    Msg = "Cannot cancel this queue item. It may not exist or is not in a cancellable state."
                });
            }

            return Ok(new ApiResponse<object>
            {
                Success = true,
                Msg = $"Queue item cancelled successfully (mode: {request.CancelMode})"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error cancelling queue item {Id}", id);
            return StatusCode(500, new ApiResponse<object>
            {
                Success = false,
                Msg = "Failed to cancel queue item"
            });
        }
    }

    /// <summary>
    /// Retry a failed mission
    /// </summary>
    [HttpPost("{id}/retry")]
    public async Task<ActionResult<ApiResponse<MissionQueueDto>>> Retry(int id, CancellationToken cancellationToken)
    {
        try
        {
            var result = await _queueService.RetryAsync(id, cancellationToken);
            if (result == null)
            {
                return BadRequest(new ApiResponse<MissionQueueDto>
                {
                    Success = false,
                    Msg = "Cannot retry this queue item. It may not exist, not be in failed state, or has reached max retries."
                });
            }

            return Ok(new ApiResponse<MissionQueueDto>
            {
                Success = true,
                Data = MapToDto(result),
                Msg = $"Mission queued for retry (attempt {result.RetryCount})"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrying queue item {Id}", id);
            return StatusCode(500, new ApiResponse<MissionQueueDto>
            {
                Success = false,
                Msg = "Failed to retry queue item"
            });
        }
    }

    /// <summary>
    /// Move queue item up (increase priority)
    /// </summary>
    [HttpPost("{id}/move-up")]
    public async Task<ActionResult<ApiResponse<object>>> MoveUp(int id, CancellationToken cancellationToken)
    {
        try
        {
            var success = await _queueService.MoveUpAsync(id, cancellationToken);
            if (!success)
            {
                return BadRequest(new ApiResponse<object>
                {
                    Success = false,
                    Msg = "Cannot move this item up. It may not exist, not be queued, or already at the top."
                });
            }

            return Ok(new ApiResponse<object>
            {
                Success = true,
                Msg = "Queue item moved up successfully"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error moving queue item {Id} up", id);
            return StatusCode(500, new ApiResponse<object>
            {
                Success = false,
                Msg = "Failed to move queue item up"
            });
        }
    }

    /// <summary>
    /// Move queue item down (decrease priority)
    /// </summary>
    [HttpPost("{id}/move-down")]
    public async Task<ActionResult<ApiResponse<object>>> MoveDown(int id, CancellationToken cancellationToken)
    {
        try
        {
            var success = await _queueService.MoveDownAsync(id, cancellationToken);
            if (!success)
            {
                return BadRequest(new ApiResponse<object>
                {
                    Success = false,
                    Msg = "Cannot move this item down. It may not exist, not be queued, or already at the bottom."
                });
            }

            return Ok(new ApiResponse<object>
            {
                Success = true,
                Msg = "Queue item moved down successfully"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error moving queue item {Id} down", id);
            return StatusCode(500, new ApiResponse<object>
            {
                Success = false,
                Msg = "Failed to move queue item down"
            });
        }
    }

    /// <summary>
    /// Change queue item priority
    /// </summary>
    [HttpPut("{id}/priority")]
    public async Task<ActionResult<ApiResponse<MissionQueueDto>>> ChangePriority(
        int id,
        [FromBody] ChangePriorityRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await _queueService.ChangePriorityAsync(id, request.Priority, cancellationToken);
            if (result == null)
            {
                return NotFound(new ApiResponse<MissionQueueDto>
                {
                    Success = false,
                    Msg = "Queue item not found"
                });
            }

            return Ok(new ApiResponse<MissionQueueDto>
            {
                Success = true,
                Data = MapToDto(result),
                Msg = $"Priority changed to {request.Priority}"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error changing priority for queue item {Id}", id);
            return StatusCode(500, new ApiResponse<MissionQueueDto>
            {
                Success = false,
                Msg = "Failed to change priority"
            });
        }
    }

    private static MissionQueueDto MapToDto(MissionQueue entity)
    {
        return new MissionQueueDto
        {
            Id = entity.Id,
            MissionCode = entity.MissionCode,
            RequestId = entity.RequestId,
            SavedMissionId = entity.SavedMissionId,
            MissionName = entity.MissionName,
            MissionRequestJson = entity.MissionRequestJson,
            Status = entity.Status.ToString(),
            StatusCode = (int)entity.Status,
            Priority = entity.Priority,
            QueuePosition = entity.QueuePosition,
            AssignedRobotId = entity.AssignedRobotId,
            CreatedUtc = entity.CreatedUtc,
            ProcessingStartedUtc = entity.ProcessingStartedUtc,
            AssignedUtc = entity.AssignedUtc,
            CompletedUtc = entity.CompletedUtc,
            CreatedBy = entity.CreatedBy,
            RetryCount = entity.RetryCount,
            MaxRetries = entity.MaxRetries,
            ErrorMessage = entity.ErrorMessage,
            RobotTypeFilter = entity.RobotTypeFilter,
            PreferredRobotIds = entity.PreferredRobotIds,
            WaitTimeSeconds = entity.AssignedUtc.HasValue
                ? (entity.AssignedUtc.Value - entity.CreatedUtc).TotalSeconds
                : (DateTime.UtcNow - entity.CreatedUtc).TotalSeconds
        };
    }
}

// DTOs

public class MissionQueueDto
{
    public int Id { get; set; }
    public string MissionCode { get; set; } = string.Empty;
    public string RequestId { get; set; } = string.Empty;
    public int? SavedMissionId { get; set; }
    public string MissionName { get; set; } = string.Empty;
    public string MissionRequestJson { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public int StatusCode { get; set; }
    public int Priority { get; set; }
    public int QueuePosition { get; set; }
    public string? AssignedRobotId { get; set; }
    public DateTime CreatedUtc { get; set; }
    public DateTime? ProcessingStartedUtc { get; set; }
    public DateTime? AssignedUtc { get; set; }
    public DateTime? CompletedUtc { get; set; }
    public string? CreatedBy { get; set; }
    public int RetryCount { get; set; }
    public int MaxRetries { get; set; }
    public string? ErrorMessage { get; set; }
    public string? RobotTypeFilter { get; set; }
    public string? PreferredRobotIds { get; set; }
    public double WaitTimeSeconds { get; set; }
}

public class AddToQueueRequest
{
    public string MissionCode { get; set; } = string.Empty;
    public string RequestId { get; set; } = string.Empty;
    public int? SavedMissionId { get; set; }
    public string MissionName { get; set; } = string.Empty;
    public string MissionRequestJson { get; set; } = string.Empty;
    public int Priority { get; set; } = 1;
    public string? RobotTypeFilter { get; set; }
    public string? PreferredRobotIds { get; set; }
}

public class ChangePriorityRequest
{
    public int Priority { get; set; }
}

public class CancelQueueRequest
{
    public string CancelMode { get; set; } = "FORCE";
    public string? Reason { get; set; }
}
