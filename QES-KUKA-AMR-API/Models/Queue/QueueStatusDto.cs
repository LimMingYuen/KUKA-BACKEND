using System.Text.Json.Serialization;
using QES_KUKA_AMR_API.Data.Entities;

namespace QES_KUKA_AMR_API.Models.Queue;

/// <summary>
/// Detailed queue item status
/// </summary>
public class QueueItemStatusDto
{
    [JsonPropertyName("queueItemId")]
    public int QueueItemId { get; set; }

    [JsonPropertyName("queueItemCode")]
    public string QueueItemCode { get; set; } = string.Empty;

    [JsonPropertyName("missionCode")]
    public string MissionCode { get; set; } = string.Empty;

    [JsonPropertyName("status")]
    public string Status { get; set; } = string.Empty;

    [JsonPropertyName("priority")]
    public int Priority { get; set; }

    [JsonPropertyName("primaryMapCode")]
    public string PrimaryMapCode { get; set; } = string.Empty;

    [JsonPropertyName("assignedRobotId")]
    public string? AssignedRobotId { get; set; }

    [JsonPropertyName("enqueuedUtc")]
    public DateTime EnqueuedUtc { get; set; }

    [JsonPropertyName("startedUtc")]
    public DateTime? StartedUtc { get; set; }

    [JsonPropertyName("completedUtc")]
    public DateTime? CompletedUtc { get; set; }

    [JsonPropertyName("cancelledUtc")]
    public DateTime? CancelledUtc { get; set; }

    [JsonPropertyName("errorMessage")]
    public string? ErrorMessage { get; set; }

    [JsonPropertyName("retryCount")]
    public int RetryCount { get; set; }

    [JsonPropertyName("isOpportunisticJob")]
    public bool IsOpportunisticJob { get; set; }

    [JsonPropertyName("hasNextSegment")]
    public bool HasNextSegment { get; set; }
}

/// <summary>
/// Mission queue status response
/// </summary>
public class MissionQueueStatusResponse
{
    [JsonPropertyName("missionCode")]
    public string MissionCode { get; set; } = string.Empty;

    [JsonPropertyName("queueItems")]
    public List<QueueItemStatusDto> QueueItems { get; set; } = new();

    [JsonPropertyName("totalSegments")]
    public int TotalSegments { get; set; }

    [JsonPropertyName("overallStatus")]
    public string OverallStatus { get; set; } = string.Empty;
}

/// <summary>
/// MapCode queue list response
/// </summary>
public class MapCodeQueueResponse
{
    [JsonPropertyName("mapCode")]
    public string MapCode { get; set; } = string.Empty;

    [JsonPropertyName("queueItems")]
    public List<QueueItemStatusDto> QueueItems { get; set; } = new();

    [JsonPropertyName("pendingCount")]
    public int PendingCount { get; set; }

    [JsonPropertyName("processingCount")]
    public int ProcessingCount { get; set; }

    [JsonPropertyName("completedCount")]
    public int CompletedCount { get; set; }
}

/// <summary>
/// Queue statistics per MapCode
/// </summary>
public class MapCodeStatistics
{
    [JsonPropertyName("mapCode")]
    public string MapCode { get; set; } = string.Empty;

    [JsonPropertyName("pendingCount")]
    public int PendingCount { get; set; }

    [JsonPropertyName("readyToAssignCount")]
    public int ReadyToAssignCount { get; set; }

    [JsonPropertyName("assignedCount")]
    public int AssignedCount { get; set; }

    [JsonPropertyName("executingCount")]
    public int ExecutingCount { get; set; }

    [JsonPropertyName("completedCount")]
    public int CompletedCount { get; set; }

    [JsonPropertyName("failedCount")]
    public int FailedCount { get; set; }

    [JsonPropertyName("cancelledCount")]
    public int CancelledCount { get; set; }

    [JsonPropertyName("totalCount")]
    public int TotalCount { get; set; }
}

/// <summary>
/// Overall queue statistics
/// </summary>
public class QueueStatisticsResponse
{
    [JsonPropertyName("mapCodeStatistics")]
    public List<MapCodeStatistics> MapCodeStatistics { get; set; } = new();

    [JsonPropertyName("totalPending")]
    public int TotalPending { get; set; }

    [JsonPropertyName("totalProcessing")]
    public int TotalProcessing { get; set; }

    [JsonPropertyName("totalCompleted")]
    public int TotalCompleted { get; set; }

    [JsonPropertyName("totalFailed")]
    public int TotalFailed { get; set; }

    [JsonPropertyName("generatedAt")]
    public DateTime GeneratedAt { get; set; }
}

/// <summary>
/// Robot current job response
/// </summary>
public class RobotCurrentJobResponse
{
    [JsonPropertyName("robotId")]
    public string RobotId { get; set; } = string.Empty;

    [JsonPropertyName("hasActiveJob")]
    public bool HasActiveJob { get; set; }

    [JsonPropertyName("currentJob")]
    public QueueItemStatusDto? CurrentJob { get; set; }
}

/// <summary>
/// Cancel queue item response
/// </summary>
public class CancelQueueItemResponse
{
    [JsonPropertyName("success")]
    public bool Success { get; set; }

    [JsonPropertyName("message")]
    public string Message { get; set; } = string.Empty;

    [JsonPropertyName("queueItemId")]
    public int QueueItemId { get; set; }

    [JsonPropertyName("cancelledUtc")]
    public DateTime? CancelledUtc { get; set; }
}
