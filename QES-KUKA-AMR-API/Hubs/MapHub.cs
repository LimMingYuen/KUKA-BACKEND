using Microsoft.AspNetCore.SignalR;
using QES_KUKA_AMR_API.Models.MapData;

namespace QES_KUKA_AMR_API.Hubs;

/// <summary>
/// SignalR hub for real-time map updates
/// </summary>
public class MapHub : Hub
{
    private readonly ILogger<MapHub> _logger;

    public MapHub(ILogger<MapHub> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Client joins a specific floor group to receive updates for that floor
    /// </summary>
    /// <param name="floorNumber">Floor number to join</param>
    /// <param name="mapCode">Optional map code for more specific filtering</param>
    public async Task JoinFloor(string floorNumber, string? mapCode = null)
    {
        var groupName = GetGroupName(floorNumber, mapCode);
        await Groups.AddToGroupAsync(Context.ConnectionId, groupName);
        _logger.LogInformation(
            "Client {ConnectionId} joined floor group: {GroupName}",
            Context.ConnectionId, groupName);
    }

    /// <summary>
    /// Client leaves a specific floor group
    /// </summary>
    /// <param name="floorNumber">Floor number to leave</param>
    /// <param name="mapCode">Optional map code</param>
    public async Task LeaveFloor(string floorNumber, string? mapCode = null)
    {
        var groupName = GetGroupName(floorNumber, mapCode);
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, groupName);
        _logger.LogInformation(
            "Client {ConnectionId} left floor group: {GroupName}",
            Context.ConnectionId, groupName);
    }

    public override async Task OnConnectedAsync()
    {
        _logger.LogInformation("Client connected to MapHub: {ConnectionId}", Context.ConnectionId);
        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        _logger.LogInformation("Client disconnected from MapHub: {ConnectionId}", Context.ConnectionId);
        await base.OnDisconnectedAsync(exception);
    }

    /// <summary>
    /// Generate group name from floor and map code
    /// </summary>
    private static string GetGroupName(string floorNumber, string? mapCode)
    {
        return string.IsNullOrEmpty(mapCode)
            ? $"floor_{floorNumber}"
            : $"floor_{floorNumber}_map_{mapCode}";
    }
}

/// <summary>
/// Interface for map hub notifications (for dependency injection)
/// </summary>
public interface IMapNotificationService
{
    /// <summary>
    /// Notify all connected clients that robot positions have been updated
    /// </summary>
    /// <param name="data">Updated realtime data</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task NotifyRobotPositionsUpdatedAsync(MapRealtimeDataDto data, CancellationToken cancellationToken = default);

    /// <summary>
    /// Notify clients in a specific floor group that positions have been updated
    /// </summary>
    /// <param name="floorNumber">Floor number</param>
    /// <param name="mapCode">Optional map code</param>
    /// <param name="data">Updated realtime data for this floor/map</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task NotifyFloorUpdatedAsync(string floorNumber, string? mapCode, MapRealtimeDataDto data, CancellationToken cancellationToken = default);
}

/// <summary>
/// Service to send notifications via MapHub SignalR hub
/// </summary>
public class MapNotificationService : IMapNotificationService
{
    private readonly IHubContext<MapHub> _hubContext;
    private readonly ILogger<MapNotificationService> _logger;

    public MapNotificationService(
        IHubContext<MapHub> hubContext,
        ILogger<MapNotificationService> logger)
    {
        _hubContext = hubContext;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task NotifyRobotPositionsUpdatedAsync(MapRealtimeDataDto data, CancellationToken cancellationToken = default)
    {
        try
        {
            await _hubContext.Clients.All.SendAsync("RobotPositionsUpdated", data, cancellationToken);
            _logger.LogDebug(
                "Sent RobotPositionsUpdated to all clients: {RobotCount} robots, {ContainerCount} containers",
                data.Robots.Count, data.Containers.Count);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to send RobotPositionsUpdated notification");
        }
    }

    /// <inheritdoc />
    public async Task NotifyFloorUpdatedAsync(string floorNumber, string? mapCode, MapRealtimeDataDto data, CancellationToken cancellationToken = default)
    {
        try
        {
            var groupName = GetGroupName(floorNumber, mapCode);
            await _hubContext.Clients.Group(groupName).SendAsync("RobotPositionsUpdated", data, cancellationToken);
            _logger.LogDebug(
                "Sent RobotPositionsUpdated to group {GroupName}: {RobotCount} robots",
                groupName, data.Robots.Count);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to send RobotPositionsUpdated to floor group");
        }
    }

    private static string GetGroupName(string floorNumber, string? mapCode)
    {
        return string.IsNullOrEmpty(mapCode)
            ? $"floor_{floorNumber}"
            : $"floor_{floorNumber}_map_{mapCode}";
    }
}
