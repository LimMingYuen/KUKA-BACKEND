using Microsoft.AspNetCore.SignalR;
using QES_KUKA_AMR_API.Services.IoController;

namespace QES_KUKA_AMR_API.Hubs;

/// <summary>
/// SignalR hub for real-time IO controller state updates.
/// Clients can subscribe to specific devices to receive filtered updates.
/// Tracks connections for demand-based polling - polling only occurs when clients are connected.
/// </summary>
public class IoControllerHub : Hub
{
    private readonly ILogger<IoControllerHub> _logger;
    private readonly IIoConnectionTracker _connectionTracker;

    public IoControllerHub(
        ILogger<IoControllerHub> logger,
        IIoConnectionTracker connectionTracker)
    {
        _logger = logger;
        _connectionTracker = connectionTracker;
    }

    public override async Task OnConnectedAsync()
    {
        _connectionTracker.AddConnection(Context.ConnectionId);
        _logger.LogInformation("Client connected to IoControllerHub: {ConnectionId}", Context.ConnectionId);
        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        _connectionTracker.RemoveConnection(Context.ConnectionId);
        _connectionTracker.RemoveAllSubscriptions(Context.ConnectionId);
        _logger.LogInformation("Client disconnected from IoControllerHub: {ConnectionId}", Context.ConnectionId);
        await base.OnDisconnectedAsync(exception);
    }

    /// <summary>
    /// Subscribe to updates for a specific device.
    /// Group name format: "device:{deviceId}"
    /// </summary>
    public async Task SubscribeToDevice(int deviceId)
    {
        var groupName = GetDeviceGroupName(deviceId);
        await Groups.AddToGroupAsync(Context.ConnectionId, groupName);
        _connectionTracker.AddSubscription(Context.ConnectionId, deviceId);
        _logger.LogInformation("Client {ConnectionId} subscribed to device: {DeviceId}",
            Context.ConnectionId, deviceId);
    }

    /// <summary>
    /// Unsubscribe from updates for a specific device.
    /// </summary>
    public async Task UnsubscribeFromDevice(int deviceId)
    {
        var groupName = GetDeviceGroupName(deviceId);
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, groupName);
        _connectionTracker.RemoveSubscription(Context.ConnectionId, deviceId);
        _logger.LogInformation("Client {ConnectionId} unsubscribed from device: {DeviceId}",
            Context.ConnectionId, deviceId);
    }

    /// <summary>
    /// Subscribe to all device updates.
    /// </summary>
    public async Task SubscribeToAll()
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, "all-devices");
        _connectionTracker.AddSubscription(Context.ConnectionId, null);
        _logger.LogInformation("Client {ConnectionId} subscribed to all devices", Context.ConnectionId);
    }

    /// <summary>
    /// Unsubscribe from all device updates.
    /// </summary>
    public async Task UnsubscribeFromAll()
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, "all-devices");
        _connectionTracker.RemoveSubscription(Context.ConnectionId, null);
        _logger.LogInformation("Client {ConnectionId} unsubscribed from all devices", Context.ConnectionId);
    }

    /// <summary>
    /// Generate consistent group name for a device.
    /// </summary>
    public static string GetDeviceGroupName(int deviceId)
    {
        return $"device:{deviceId}";
    }
}
