using Microsoft.EntityFrameworkCore;
using QES_KUKA_AMR_API.Data;
using QES_KUKA_AMR_API.Data.Entities;

namespace QES_KUKA_AMR_API.Services.IoController;

/// <summary>
/// Service for logging IO state changes (audit trail).
/// </summary>
public class IoStateLogService : IIoStateLogService
{
    private readonly ApplicationDbContext _dbContext;
    private readonly ILogger<IoStateLogService> _logger;
    private readonly TimeProvider _timeProvider;

    public IoStateLogService(
        ApplicationDbContext dbContext,
        ILogger<IoStateLogService> logger,
        TimeProvider timeProvider)
    {
        _dbContext = dbContext;
        _logger = logger;
        _timeProvider = timeProvider;
    }

    public async Task LogStateChangeAsync(
        int deviceId,
        int channelNumber,
        IoChannelType channelType,
        bool previousState,
        bool newState,
        IoStateChangeSource source,
        string? changedBy,
        string? reason,
        CancellationToken ct = default)
    {
        var log = new IoStateLog
        {
            DeviceId = deviceId,
            ChannelNumber = channelNumber,
            ChannelType = channelType,
            PreviousState = previousState,
            NewState = newState,
            ChangeSource = source,
            ChangedBy = changedBy,
            ChangedUtc = _timeProvider.GetUtcNow().UtcDateTime,
            Reason = reason
        };

        _dbContext.IoStateLogs.Add(log);
        await _dbContext.SaveChangesAsync(ct);

        _logger.LogDebug("Logged {ChannelType} {Channel} state change on device {DeviceId}: {Old} -> {New} (Source: {Source}, By: {By})",
            channelType, channelNumber, deviceId,
            previousState ? "ON" : "OFF",
            newState ? "ON" : "OFF",
            source,
            changedBy ?? "System");
    }

    public async Task<List<IoStateLog>> GetRecentLogsForDeviceAsync(int deviceId, int count = 50, CancellationToken ct = default)
    {
        return await _dbContext.IoStateLogs
            .AsNoTracking()
            .Where(l => l.DeviceId == deviceId)
            .OrderByDescending(l => l.ChangedUtc)
            .Take(count)
            .ToListAsync(ct);
    }

    public async Task<PagedResult<IoStateLog>> GetLogsAsync(IoStateLogQuery query, CancellationToken ct = default)
    {
        var queryable = _dbContext.IoStateLogs.AsNoTracking();

        // Apply filters
        if (query.DeviceId.HasValue)
        {
            queryable = queryable.Where(l => l.DeviceId == query.DeviceId.Value);
        }

        if (query.ChannelNumber.HasValue)
        {
            queryable = queryable.Where(l => l.ChannelNumber == query.ChannelNumber.Value);
        }

        if (query.ChannelType.HasValue)
        {
            queryable = queryable.Where(l => l.ChannelType == query.ChannelType.Value);
        }

        if (query.ChangeSource.HasValue)
        {
            queryable = queryable.Where(l => l.ChangeSource == query.ChangeSource.Value);
        }

        if (!string.IsNullOrEmpty(query.ChangedBy))
        {
            queryable = queryable.Where(l => l.ChangedBy == query.ChangedBy);
        }

        if (query.FromUtc.HasValue)
        {
            queryable = queryable.Where(l => l.ChangedUtc >= query.FromUtc.Value);
        }

        if (query.ToUtc.HasValue)
        {
            queryable = queryable.Where(l => l.ChangedUtc <= query.ToUtc.Value);
        }

        // Get total count
        var totalCount = await queryable.CountAsync(ct);

        // Apply pagination
        var items = await queryable
            .OrderByDescending(l => l.ChangedUtc)
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .ToListAsync(ct);

        return new PagedResult<IoStateLog>
        {
            Items = items,
            TotalCount = totalCount,
            Page = query.Page,
            PageSize = query.PageSize
        };
    }
}
