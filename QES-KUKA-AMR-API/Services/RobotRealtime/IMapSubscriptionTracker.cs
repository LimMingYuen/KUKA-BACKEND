namespace QES_KUKA_AMR_API.Services.RobotRealtime;

/// <summary>
/// Represents a unique map/floor subscription key.
/// </summary>
public record MapSubscriptionKey(string? MapCode, string? FloorNumber)
{
    /// <summary>
    /// True if this is an "all robots" subscription (no map/floor filter).
    /// </summary>
    public bool IsAllSubscription => string.IsNullOrEmpty(MapCode) && string.IsNullOrEmpty(FloorNumber);
}

/// <summary>
/// Tracks which map/floor combinations have active SignalR subscriptions.
/// Used by the polling service to determine what to poll from the external API.
/// </summary>
public interface IMapSubscriptionTracker
{
    /// <summary>
    /// Add a subscription for a connection to a specific map/floor.
    /// </summary>
    void AddSubscription(string connectionId, string? mapCode, string? floorNumber);

    /// <summary>
    /// Remove a subscription for a connection from a specific map/floor.
    /// </summary>
    void RemoveSubscription(string connectionId, string? mapCode, string? floorNumber);

    /// <summary>
    /// Remove all subscriptions for a connection (when disconnected).
    /// </summary>
    void RemoveAllSubscriptions(string connectionId);

    /// <summary>
    /// Get all unique map/floor combinations that have active subscriptions.
    /// </summary>
    IReadOnlyCollection<MapSubscriptionKey> GetActiveSubscriptions();

    /// <summary>
    /// Check if there are any active subscriptions.
    /// </summary>
    bool HasActiveSubscriptions { get; }
}
