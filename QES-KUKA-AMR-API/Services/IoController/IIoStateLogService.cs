using QES_KUKA_AMR_API.Data.Entities;

namespace QES_KUKA_AMR_API.Services.IoController;

/// <summary>
/// Service for logging IO state changes (audit trail).
/// </summary>
public interface IIoStateLogService
{
    /// <summary>
    /// Log a state change event.
    /// </summary>
    Task LogStateChangeAsync(
        int deviceId,
        int channelNumber,
        IoChannelType channelType,
        bool previousState,
        bool newState,
        IoStateChangeSource source,
        string? changedBy,
        string? reason,
        CancellationToken ct = default);

    /// <summary>
    /// Get recent logs for a specific device.
    /// </summary>
    Task<List<IoStateLog>> GetRecentLogsForDeviceAsync(int deviceId, int count = 50, CancellationToken ct = default);

    /// <summary>
    /// Get logs with filtering and pagination.
    /// </summary>
    Task<PagedResult<IoStateLog>> GetLogsAsync(IoStateLogQuery query, CancellationToken ct = default);
}

/// <summary>
/// Query parameters for retrieving state logs.
/// </summary>
public class IoStateLogQuery
{
    public int? DeviceId { get; set; }
    public int? ChannelNumber { get; set; }
    public IoChannelType? ChannelType { get; set; }
    public IoStateChangeSource? ChangeSource { get; set; }
    public string? ChangedBy { get; set; }
    public DateTime? FromUtc { get; set; }
    public DateTime? ToUtc { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 50;
}

/// <summary>
/// Paged result for queries.
/// </summary>
public class PagedResult<T>
{
    public List<T> Items { get; set; } = new();
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalPages => (int)Math.Ceiling(TotalCount / (double)PageSize);
    public bool HasNextPage => Page < TotalPages;
    public bool HasPreviousPage => Page > 1;
}
