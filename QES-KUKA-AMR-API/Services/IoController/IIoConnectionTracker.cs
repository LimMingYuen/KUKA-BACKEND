namespace QES_KUKA_AMR_API.Services.IoController;

/// <summary>
/// Tracks SignalR connections and device subscriptions for IO Controller hub.
/// Used for demand-based polling - only poll devices that have active subscribers.
/// </summary>
public interface IIoConnectionTracker
{
    /// <summary>
    /// Whether there are any active connections.
    /// </summary>
    bool HasActiveConnections { get; }

    /// <summary>
    /// Whether there are any active device subscriptions.
    /// </summary>
    bool HasActiveSubscriptions { get; }

    /// <summary>
    /// Get list of device IDs that have active subscribers.
    /// Returns null in the list if someone subscribed to "all devices".
    /// </summary>
    IReadOnlyCollection<int?> GetSubscribedDeviceIds();

    /// <summary>
    /// Add a new connection.
    /// </summary>
    void AddConnection(string connectionId);

    /// <summary>
    /// Remove a connection.
    /// </summary>
    void RemoveConnection(string connectionId);

    /// <summary>
    /// Add a device subscription for a connection.
    /// </summary>
    /// <param name="connectionId">SignalR connection ID</param>
    /// <param name="deviceId">Device ID to subscribe to, or null for "all devices"</param>
    void AddSubscription(string connectionId, int? deviceId);

    /// <summary>
    /// Remove a device subscription for a connection.
    /// </summary>
    void RemoveSubscription(string connectionId, int? deviceId);

    /// <summary>
    /// Remove all subscriptions for a connection (on disconnect).
    /// </summary>
    void RemoveAllSubscriptions(string connectionId);
}
