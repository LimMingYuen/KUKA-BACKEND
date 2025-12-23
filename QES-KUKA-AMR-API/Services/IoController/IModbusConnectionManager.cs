using NModbus;

namespace QES_KUKA_AMR_API.Services.IoController;

/// <summary>
/// Manages persistent Modbus TCP connections to devices.
/// Provides connection pooling and automatic reconnection.
/// </summary>
public interface IModbusConnectionManager : IDisposable
{
    /// <summary>
    /// Gets or creates a Modbus master connection for the specified device.
    /// </summary>
    /// <param name="deviceId">Unique device identifier</param>
    /// <param name="ipAddress">Device IP address</param>
    /// <param name="port">Device TCP port</param>
    /// <param name="unitId">Modbus unit ID</param>
    /// <param name="timeoutMs">Connection/read/write timeout in milliseconds</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Connected Modbus master, or null if connection failed</returns>
    Task<IModbusMaster?> GetConnectionAsync(
        int deviceId,
        string ipAddress,
        int port,
        byte unitId,
        int timeoutMs,
        CancellationToken ct = default);

    /// <summary>
    /// Marks a connection as failed, triggering reconnection on next access.
    /// </summary>
    /// <param name="deviceId">Device identifier</param>
    void InvalidateConnection(int deviceId);

    /// <summary>
    /// Closes and removes the connection for a specific device.
    /// </summary>
    /// <param name="deviceId">Device identifier</param>
    void RemoveConnection(int deviceId);

    /// <summary>
    /// Gets connection statistics for monitoring.
    /// </summary>
    ModbusConnectionStats GetStats();
}

/// <summary>
/// Connection statistics for monitoring.
/// </summary>
public class ModbusConnectionStats
{
    public int ActiveConnections { get; set; }
    public int TotalConnections { get; set; }
    public int TotalReconnects { get; set; }
    public int TotalFailures { get; set; }
}
