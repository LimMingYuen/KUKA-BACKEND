using System.Collections.Concurrent;

namespace QES_KUKA_AMR_API.Services.RobotRealtime;

/// <summary>
/// Thread-safe implementation of SignalR connection tracking.
/// Uses ConcurrentDictionary for safe multi-threaded access.
/// </summary>
public class SignalRConnectionTracker : ISignalRConnectionTracker
{
    private readonly ConcurrentDictionary<string, byte> _connections = new();
    private readonly ILogger<SignalRConnectionTracker> _logger;

    public SignalRConnectionTracker(ILogger<SignalRConnectionTracker> logger)
    {
        _logger = logger;
    }

    public void AddConnection(string connectionId)
    {
        if (_connections.TryAdd(connectionId, 0))
        {
            _logger.LogInformation(
                "SignalR connection added: {ConnectionId}. Active connections: {Count}",
                connectionId, _connections.Count);
        }
    }

    public void RemoveConnection(string connectionId)
    {
        if (_connections.TryRemove(connectionId, out _))
        {
            _logger.LogInformation(
                "SignalR connection removed: {ConnectionId}. Active connections: {Count}",
                connectionId, _connections.Count);
        }
    }

    public bool HasActiveConnections => !_connections.IsEmpty;

    public int ConnectionCount => _connections.Count;
}
