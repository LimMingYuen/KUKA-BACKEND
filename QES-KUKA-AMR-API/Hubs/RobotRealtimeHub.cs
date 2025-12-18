using Microsoft.AspNetCore.SignalR;
using QES_KUKA_AMR_API.Services.RobotRealtime;

namespace QES_KUKA_AMR_API.Hubs;

/// <summary>
/// SignalR hub for real-time robot position and status updates.
/// Clients can subscribe to specific maps/floors to receive filtered updates.
/// Tracks connections for demand-based polling - polling only occurs when clients are connected.
/// </summary>
public class RobotRealtimeHub : Hub
{
    private readonly ILogger<RobotRealtimeHub> _logger;
    private readonly ISignalRConnectionTracker _connectionTracker;

    public RobotRealtimeHub(
        ILogger<RobotRealtimeHub> logger,
        ISignalRConnectionTracker connectionTracker)
    {
        _logger = logger;
        _connectionTracker = connectionTracker;
    }

    public override async Task OnConnectedAsync()
    {
        _connectionTracker.AddConnection(Context.ConnectionId);
        _logger.LogInformation("Client connected to RobotRealtimeHub: {ConnectionId}", Context.ConnectionId);
        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        _connectionTracker.RemoveConnection(Context.ConnectionId);
        _logger.LogInformation("Client disconnected from RobotRealtimeHub: {ConnectionId}", Context.ConnectionId);
        await base.OnDisconnectedAsync(exception);
    }

    /// <summary>
    /// Subscribe to robot updates for a specific map and floor.
    /// Group name format: "map:{mapCode}:floor:{floorNumber}" or "all" for all robots.
    /// </summary>
    public async Task SubscribeToMap(string? mapCode, string? floorNumber)
    {
        var groupName = GetGroupName(mapCode, floorNumber);
        await Groups.AddToGroupAsync(Context.ConnectionId, groupName);
        _logger.LogInformation("Client {ConnectionId} subscribed to group: {GroupName}",
            Context.ConnectionId, groupName);
    }

    /// <summary>
    /// Unsubscribe from robot updates for a specific map and floor.
    /// </summary>
    public async Task UnsubscribeFromMap(string? mapCode, string? floorNumber)
    {
        var groupName = GetGroupName(mapCode, floorNumber);
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, groupName);
        _logger.LogInformation("Client {ConnectionId} unsubscribed from group: {GroupName}",
            Context.ConnectionId, groupName);
    }

    /// <summary>
    /// Subscribe to all robot updates (no map/floor filter).
    /// </summary>
    public async Task SubscribeToAll()
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, "all");
        _logger.LogInformation("Client {ConnectionId} subscribed to all robots", Context.ConnectionId);
    }

    /// <summary>
    /// Generate consistent group name for map/floor combination.
    /// </summary>
    public static string GetGroupName(string? mapCode, string? floorNumber)
    {
        if (string.IsNullOrEmpty(mapCode) && string.IsNullOrEmpty(floorNumber))
            return "all";

        if (string.IsNullOrEmpty(floorNumber))
            return $"map:{mapCode}";

        if (string.IsNullOrEmpty(mapCode))
            return $"floor:{floorNumber}";

        return $"map:{mapCode}:floor:{floorNumber}";
    }
}
