using Microsoft.AspNetCore.SignalR;
using QES_KUKA_AMR_API.Data.Entities;

namespace QES_KUKA_AMR_API.Hubs;

/// <summary>
/// SignalR hub for real-time queue updates
/// </summary>
public class QueueHub : Hub
{
    private readonly ILogger<QueueHub> _logger;

    public QueueHub(ILogger<QueueHub> logger)
    {
        _logger = logger;
    }

    public override async Task OnConnectedAsync()
    {
        _logger.LogInformation("Client connected to QueueHub: {ConnectionId}", Context.ConnectionId);
        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        _logger.LogInformation("Client disconnected from QueueHub: {ConnectionId}", Context.ConnectionId);
        await base.OnDisconnectedAsync(exception);
    }
}

/// <summary>
/// Interface for queue hub notifications (for dependency injection)
/// </summary>
public interface IQueueNotificationService
{
    Task NotifyQueueUpdatedAsync(CancellationToken cancellationToken = default);
    Task NotifyMissionStatusChangedAsync(int missionId, MissionQueueStatus status, CancellationToken cancellationToken = default);
    Task NotifyStatisticsUpdatedAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// Service to send notifications via SignalR hub
/// </summary>
public class QueueNotificationService : IQueueNotificationService
{
    private readonly IHubContext<QueueHub> _hubContext;
    private readonly ILogger<QueueNotificationService> _logger;

    public QueueNotificationService(
        IHubContext<QueueHub> hubContext,
        ILogger<QueueNotificationService> logger)
    {
        _hubContext = hubContext;
        _logger = logger;
    }

    /// <summary>
    /// Notify all clients that the queue has been updated
    /// </summary>
    public async Task NotifyQueueUpdatedAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            await _hubContext.Clients.All.SendAsync("QueueUpdated", cancellationToken);
            _logger.LogDebug("Sent QueueUpdated notification to all clients");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to send QueueUpdated notification");
        }
    }

    /// <summary>
    /// Notify all clients that a specific mission's status has changed
    /// </summary>
    public async Task NotifyMissionStatusChangedAsync(int missionId, MissionQueueStatus status, CancellationToken cancellationToken = default)
    {
        try
        {
            await _hubContext.Clients.All.SendAsync("MissionStatusChanged", new
            {
                MissionId = missionId,
                Status = (int)status,
                StatusName = status.ToString()
            }, cancellationToken);
            _logger.LogDebug("Sent MissionStatusChanged notification: MissionId={MissionId}, Status={Status}", missionId, status);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to send MissionStatusChanged notification");
        }
    }

    /// <summary>
    /// Notify all clients that statistics have been updated
    /// </summary>
    public async Task NotifyStatisticsUpdatedAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            await _hubContext.Clients.All.SendAsync("StatisticsUpdated", cancellationToken);
            _logger.LogDebug("Sent StatisticsUpdated notification to all clients");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to send StatisticsUpdated notification");
        }
    }
}
