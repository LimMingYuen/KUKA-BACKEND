using Microsoft.AspNetCore.SignalR;
using QES_KUKA_AMR_API.Data.Entities;
using QES_KUKA_AMR_API.Hubs;

namespace QES_KUKA_AMR_API.Services.IoController;

/// <summary>
/// Service for broadcasting IO controller updates via SignalR.
/// </summary>
public class IoNotificationService : IIoNotificationService
{
    private readonly IHubContext<IoControllerHub> _hubContext;
    private readonly ILogger<IoNotificationService> _logger;

    public IoNotificationService(
        IHubContext<IoControllerHub> hubContext,
        ILogger<IoNotificationService> logger)
    {
        _hubContext = hubContext;
        _logger = logger;
    }

    public async Task BroadcastDeviceStatusAsync(IoDeviceFullStatus status, CancellationToken ct = default)
    {
        var deviceGroupName = IoControllerHub.GetDeviceGroupName(status.Device.Id);

        // Create DTO for transmission
        var dto = new IoDeviceStatusDto
        {
            DeviceId = status.Device.Id,
            DeviceName = status.Device.DeviceName,
            IsConnected = status.IsConnected,
            LastPollUtc = status.LastPollUtc,
            Channels = status.Channels.Select(c => new IoChannelDto
            {
                ChannelNumber = c.ChannelNumber,
                ChannelType = c.ChannelType.ToString(),
                Label = c.Label,
                CurrentState = c.CurrentState,
                FsvEnabled = c.FsvEnabled,
                FailSafeValue = c.FailSafeValue,
                LastStateChangeUtc = c.LastStateChangeUtc
            }).ToList()
        };

        // Send to device-specific group
        await _hubContext.Clients.Group(deviceGroupName)
            .SendAsync("ReceiveDeviceStatus", dto, ct);

        // Also send to "all-devices" group
        await _hubContext.Clients.Group("all-devices")
            .SendAsync("ReceiveDeviceStatus", dto, ct);

        _logger.LogDebug("Broadcast device status for {DeviceName} (ID: {DeviceId})",
            status.Device.DeviceName, status.Device.Id);
    }

    public async Task BroadcastChannelChangeAsync(int deviceId, IoChannel channel, CancellationToken ct = default)
    {
        var deviceGroupName = IoControllerHub.GetDeviceGroupName(deviceId);

        var dto = new IoChannelChangeDto
        {
            DeviceId = deviceId,
            ChannelNumber = channel.ChannelNumber,
            ChannelType = channel.ChannelType.ToString(),
            Label = channel.Label,
            CurrentState = channel.CurrentState,
            LastStateChangeUtc = channel.LastStateChangeUtc
        };

        // Send to device-specific group
        await _hubContext.Clients.Group(deviceGroupName)
            .SendAsync("ReceiveChannelChange", dto, ct);

        // Also send to "all-devices" group
        await _hubContext.Clients.Group("all-devices")
            .SendAsync("ReceiveChannelChange", dto, ct);

        _logger.LogDebug("Broadcast channel change: Device {DeviceId} {Type} {Channel} = {State}",
            deviceId, channel.ChannelType, channel.ChannelNumber, channel.CurrentState ? "ON" : "OFF");
    }

    public async Task BroadcastConnectionStatusAsync(int deviceId, bool isConnected, string? errorMessage, CancellationToken ct = default)
    {
        var deviceGroupName = IoControllerHub.GetDeviceGroupName(deviceId);

        var dto = new IoConnectionStatusDto
        {
            DeviceId = deviceId,
            IsConnected = isConnected,
            ErrorMessage = errorMessage,
            Timestamp = DateTime.UtcNow
        };

        // Send to device-specific group
        await _hubContext.Clients.Group(deviceGroupName)
            .SendAsync("ReceiveConnectionStatus", dto, ct);

        // Also send to "all-devices" group
        await _hubContext.Clients.Group("all-devices")
            .SendAsync("ReceiveConnectionStatus", dto, ct);

        _logger.LogDebug("Broadcast connection status: Device {DeviceId} Connected={IsConnected}",
            deviceId, isConnected);
    }
}

// DTOs for SignalR transmission
public class IoDeviceStatusDto
{
    public int DeviceId { get; set; }
    public string DeviceName { get; set; } = string.Empty;
    public bool IsConnected { get; set; }
    public DateTime? LastPollUtc { get; set; }
    public List<IoChannelDto> Channels { get; set; } = new();
}

public class IoChannelDto
{
    public int ChannelNumber { get; set; }
    public string ChannelType { get; set; } = string.Empty;
    public string? Label { get; set; }
    public bool CurrentState { get; set; }
    public bool FsvEnabled { get; set; }
    public bool? FailSafeValue { get; set; }
    public DateTime? LastStateChangeUtc { get; set; }
}

public class IoChannelChangeDto
{
    public int DeviceId { get; set; }
    public int ChannelNumber { get; set; }
    public string ChannelType { get; set; } = string.Empty;
    public string? Label { get; set; }
    public bool CurrentState { get; set; }
    public DateTime? LastStateChangeUtc { get; set; }
}

public class IoConnectionStatusDto
{
    public int DeviceId { get; set; }
    public bool IsConnected { get; set; }
    public string? ErrorMessage { get; set; }
    public DateTime Timestamp { get; set; }
}
