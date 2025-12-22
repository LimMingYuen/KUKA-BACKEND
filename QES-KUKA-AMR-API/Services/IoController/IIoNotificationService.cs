using QES_KUKA_AMR_API.Data.Entities;

namespace QES_KUKA_AMR_API.Services.IoController;

/// <summary>
/// Service for broadcasting IO controller updates via SignalR.
/// </summary>
public interface IIoNotificationService
{
    /// <summary>
    /// Broadcast full device status update to subscribers.
    /// </summary>
    Task BroadcastDeviceStatusAsync(IoDeviceFullStatus status, CancellationToken ct = default);

    /// <summary>
    /// Broadcast channel state change to subscribers.
    /// </summary>
    Task BroadcastChannelChangeAsync(int deviceId, IoChannel channel, CancellationToken ct = default);

    /// <summary>
    /// Broadcast device connection status change.
    /// </summary>
    Task BroadcastConnectionStatusAsync(int deviceId, bool isConnected, string? errorMessage, CancellationToken ct = default);
}
