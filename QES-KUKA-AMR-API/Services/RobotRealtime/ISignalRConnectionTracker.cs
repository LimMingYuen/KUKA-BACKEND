namespace QES_KUKA_AMR_API.Services.RobotRealtime;

/// <summary>
/// Tracks active SignalR connections for demand-based polling.
/// When no clients are connected, background services can pause to save resources.
/// </summary>
public interface ISignalRConnectionTracker
{
    /// <summary>
    /// Register a new connection.
    /// </summary>
    /// <param name="connectionId">The SignalR connection ID.</param>
    void AddConnection(string connectionId);

    /// <summary>
    /// Remove a connection when client disconnects.
    /// </summary>
    /// <param name="connectionId">The SignalR connection ID.</param>
    void RemoveConnection(string connectionId);

    /// <summary>
    /// Check if there are any active connections.
    /// </summary>
    bool HasActiveConnections { get; }

    /// <summary>
    /// Get the current number of active connections.
    /// </summary>
    int ConnectionCount { get; }
}
