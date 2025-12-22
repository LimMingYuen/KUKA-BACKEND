using System.Collections.Concurrent;

namespace QES_KUKA_AMR_API.Services.IoController;

/// <summary>
/// Thread-safe implementation of connection and subscription tracking for IO Controller hub.
/// </summary>
public class IoConnectionTracker : IIoConnectionTracker
{
    private readonly ConcurrentDictionary<string, byte> _connections = new();
    private readonly ConcurrentDictionary<string, HashSet<int?>> _subscriptions = new();
    private readonly object _subscriptionLock = new();

    public bool HasActiveConnections => !_connections.IsEmpty;

    public bool HasActiveSubscriptions
    {
        get
        {
            lock (_subscriptionLock)
            {
                return _subscriptions.Values.Any(s => s.Count > 0);
            }
        }
    }

    public IReadOnlyCollection<int?> GetSubscribedDeviceIds()
    {
        lock (_subscriptionLock)
        {
            return _subscriptions.Values
                .SelectMany(s => s)
                .Distinct()
                .ToList()
                .AsReadOnly();
        }
    }

    public void AddConnection(string connectionId)
    {
        _connections.TryAdd(connectionId, 0);
    }

    public void RemoveConnection(string connectionId)
    {
        _connections.TryRemove(connectionId, out _);
    }

    public void AddSubscription(string connectionId, int? deviceId)
    {
        lock (_subscriptionLock)
        {
            if (!_subscriptions.TryGetValue(connectionId, out var subs))
            {
                subs = new HashSet<int?>();
                _subscriptions[connectionId] = subs;
            }
            subs.Add(deviceId);
        }
    }

    public void RemoveSubscription(string connectionId, int? deviceId)
    {
        lock (_subscriptionLock)
        {
            if (_subscriptions.TryGetValue(connectionId, out var subs))
            {
                subs.Remove(deviceId);
            }
        }
    }

    public void RemoveAllSubscriptions(string connectionId)
    {
        lock (_subscriptionLock)
        {
            _subscriptions.TryRemove(connectionId, out _);
        }
    }
}
