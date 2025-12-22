using System.Collections.Concurrent;

namespace QES_KUKA_AMR_API.Services.RobotRealtime;

/// <summary>
/// Thread-safe implementation of map subscription tracking.
/// Tracks which SignalR connections are subscribed to which map/floor combinations.
/// </summary>
public class MapSubscriptionTracker : IMapSubscriptionTracker
{
    // Key: connectionId, Value: set of MapSubscriptionKeys the connection is subscribed to
    private readonly ConcurrentDictionary<string, HashSet<MapSubscriptionKey>> _connectionSubscriptions = new();

    // Key: MapSubscriptionKey, Value: count of connections subscribed to this key
    private readonly ConcurrentDictionary<MapSubscriptionKey, int> _subscriptionCounts = new();

    private readonly object _lock = new();
    private readonly ILogger<MapSubscriptionTracker> _logger;

    public MapSubscriptionTracker(ILogger<MapSubscriptionTracker> logger)
    {
        _logger = logger;
    }

    public void AddSubscription(string connectionId, string? mapCode, string? floorNumber)
    {
        var key = new MapSubscriptionKey(mapCode, floorNumber);

        lock (_lock)
        {
            // Get or create the connection's subscription set
            if (!_connectionSubscriptions.TryGetValue(connectionId, out var subscriptions))
            {
                subscriptions = new HashSet<MapSubscriptionKey>();
                _connectionSubscriptions[connectionId] = subscriptions;
            }

            // If already subscribed, skip
            if (!subscriptions.Add(key))
            {
                return;
            }

            // Increment subscription count for this map/floor
            _subscriptionCounts.AddOrUpdate(key, 1, (_, count) => count + 1);

            _logger.LogInformation(
                "Added subscription: ConnectionId={ConnectionId}, MapCode={MapCode}, FloorNumber={FloorNumber}. " +
                "Active subscriptions: {Count}",
                connectionId, mapCode ?? "(all)", floorNumber ?? "(all)",
                _subscriptionCounts.Count);
        }
    }

    public void RemoveSubscription(string connectionId, string? mapCode, string? floorNumber)
    {
        var key = new MapSubscriptionKey(mapCode, floorNumber);

        lock (_lock)
        {
            if (!_connectionSubscriptions.TryGetValue(connectionId, out var subscriptions))
            {
                return;
            }

            if (!subscriptions.Remove(key))
            {
                return;
            }

            // Decrement subscription count
            if (_subscriptionCounts.TryGetValue(key, out var count))
            {
                if (count <= 1)
                {
                    _subscriptionCounts.TryRemove(key, out _);
                }
                else
                {
                    _subscriptionCounts[key] = count - 1;
                }
            }

            // Clean up empty connection entry
            if (subscriptions.Count == 0)
            {
                _connectionSubscriptions.TryRemove(connectionId, out _);
            }

            _logger.LogInformation(
                "Removed subscription: ConnectionId={ConnectionId}, MapCode={MapCode}, FloorNumber={FloorNumber}. " +
                "Active subscriptions: {Count}",
                connectionId, mapCode ?? "(all)", floorNumber ?? "(all)",
                _subscriptionCounts.Count);
        }
    }

    public void RemoveAllSubscriptions(string connectionId)
    {
        lock (_lock)
        {
            if (!_connectionSubscriptions.TryRemove(connectionId, out var subscriptions))
            {
                return;
            }

            foreach (var key in subscriptions)
            {
                if (_subscriptionCounts.TryGetValue(key, out var count))
                {
                    if (count <= 1)
                    {
                        _subscriptionCounts.TryRemove(key, out _);
                    }
                    else
                    {
                        _subscriptionCounts[key] = count - 1;
                    }
                }
            }

            _logger.LogInformation(
                "Removed all subscriptions for ConnectionId={ConnectionId}. Active subscriptions: {Count}",
                connectionId, _subscriptionCounts.Count);
        }
    }

    public IReadOnlyCollection<MapSubscriptionKey> GetActiveSubscriptions()
    {
        lock (_lock)
        {
            return _subscriptionCounts.Keys.ToList().AsReadOnly();
        }
    }

    public bool HasActiveSubscriptions
    {
        get
        {
            lock (_lock)
            {
                return !_subscriptionCounts.IsEmpty;
            }
        }
    }
}
