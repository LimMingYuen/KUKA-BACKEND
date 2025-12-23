using System.Collections.Concurrent;
using System.Diagnostics;
using System.Net.Sockets;
using NModbus;

namespace QES_KUKA_AMR_API.Services.IoController;

/// <summary>
/// Manages persistent Modbus TCP connections with automatic reconnection.
/// Thread-safe singleton that maintains one connection per device.
/// </summary>
public class ModbusConnectionManager : IModbusConnectionManager
{
    private readonly ILogger<ModbusConnectionManager> _logger;
    private readonly IModbusFactory _modbusFactory;
    private readonly ConcurrentDictionary<int, ModbusConnection> _connections;
    private readonly SemaphoreSlim _connectionLock;
    private bool _disposed;

    // Statistics
    private int _totalConnections;
    private int _totalReconnects;
    private int _totalFailures;

    public ModbusConnectionManager(ILogger<ModbusConnectionManager> logger)
    {
        _logger = logger;
        _modbusFactory = new ModbusFactory();
        _connections = new ConcurrentDictionary<int, ModbusConnection>();
        _connectionLock = new SemaphoreSlim(1, 1);
    }

    public async Task<IModbusMaster?> GetConnectionAsync(
        int deviceId,
        string ipAddress,
        int port,
        byte unitId,
        int timeoutMs,
        CancellationToken ct = default)
    {
        if (_disposed)
        {
            _logger.LogWarning("[Modbus] Connection manager is disposed, cannot get connection for device {DeviceId}", deviceId);
            return null;
        }

        // Check if we have a valid existing connection
        if (_connections.TryGetValue(deviceId, out var existing))
        {
            if (existing.IsConnected && existing.IpAddress == ipAddress && existing.Port == port)
            {
                _logger.LogDebug("[Modbus] Reusing existing connection for device {DeviceId} ({IpAddress}:{Port})",
                    deviceId, ipAddress, port);
                return existing.Master;
            }

            // Connection is stale or settings changed - close it
            _logger.LogInformation("[Modbus] Closing stale connection for device {DeviceId} (Connected: {Connected}, IP match: {IpMatch})",
                deviceId, existing.IsConnected, existing.IpAddress == ipAddress);
            await CloseConnectionAsync(deviceId);
        }

        // Create new connection
        return await CreateConnectionAsync(deviceId, ipAddress, port, unitId, timeoutMs, ct);
    }

    private async Task<IModbusMaster?> CreateConnectionAsync(
        int deviceId,
        string ipAddress,
        int port,
        byte unitId,
        int timeoutMs,
        CancellationToken ct)
    {
        await _connectionLock.WaitAsync(ct);
        try
        {
            // Double-check after acquiring lock
            if (_connections.TryGetValue(deviceId, out var existing) && existing.IsConnected)
            {
                _logger.LogDebug("[Modbus] Connection already created by another thread for device {DeviceId}", deviceId);
                return existing.Master;
            }

            var sw = Stopwatch.StartNew();
            _logger.LogInformation("[Modbus] Creating new connection to device {DeviceId} at {IpAddress}:{Port} (UnitId: {UnitId}, Timeout: {Timeout}ms)",
                deviceId, ipAddress, port, unitId, timeoutMs);

            try
            {
                var tcpClient = new TcpClient();

                // Set socket options for keep-alive
                tcpClient.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.KeepAlive, true);
                tcpClient.Client.SetSocketOption(SocketOptionLevel.Tcp, SocketOptionName.TcpKeepAliveTime, 30);
                tcpClient.Client.SetSocketOption(SocketOptionLevel.Tcp, SocketOptionName.TcpKeepAliveInterval, 10);
                tcpClient.Client.SetSocketOption(SocketOptionLevel.Tcp, SocketOptionName.TcpKeepAliveRetryCount, 3);

                // Connect with timeout
                using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
                cts.CancelAfter(timeoutMs);

                _logger.LogDebug("[Modbus] Initiating TCP connection to {IpAddress}:{Port}...", ipAddress, port);
                await tcpClient.ConnectAsync(ipAddress, port, cts.Token);

                sw.Stop();
                _logger.LogInformation("[Modbus] TCP connection established to {IpAddress}:{Port} in {ElapsedMs}ms",
                    ipAddress, port, sw.ElapsedMilliseconds);

                // Create Modbus master
                var master = _modbusFactory.CreateMaster(tcpClient);
                master.Transport.ReadTimeout = timeoutMs;
                master.Transport.WriteTimeout = timeoutMs;
                master.Transport.Retries = 1;

                _logger.LogDebug("[Modbus] Modbus master created for device {DeviceId}, verifying connection with test read...", deviceId);

                // Verify connection with a test read
                try
                {
                    await master.ReadCoilsAsync(unitId, 0, 1);
                    _logger.LogInformation("[Modbus] Connection verified for device {DeviceId} - test read successful", deviceId);
                }
                catch (Exception testEx)
                {
                    _logger.LogWarning(testEx, "[Modbus] Test read failed for device {DeviceId}, but connection is established. Device may have different register mapping.", deviceId);
                    // Continue anyway - the device might have different register addresses
                }

                var connection = new ModbusConnection(tcpClient, master, ipAddress, port);
                _connections[deviceId] = connection;

                Interlocked.Increment(ref _totalConnections);
                if (existing != null)
                {
                    Interlocked.Increment(ref _totalReconnects);
                }

                _logger.LogInformation("[Modbus] Successfully connected to device {DeviceId} ({IpAddress}:{Port}). Total connections: {Total}, Reconnects: {Reconnects}",
                    deviceId, ipAddress, port, _totalConnections, _totalReconnects);

                return master;
            }
            catch (OperationCanceledException)
            {
                sw.Stop();
                Interlocked.Increment(ref _totalFailures);
                _logger.LogError("[Modbus] Connection TIMEOUT for device {DeviceId} ({IpAddress}:{Port}) after {ElapsedMs}ms. Timeout was {Timeout}ms",
                    deviceId, ipAddress, port, sw.ElapsedMilliseconds, timeoutMs);
                return null;
            }
            catch (SocketException sockEx)
            {
                sw.Stop();
                Interlocked.Increment(ref _totalFailures);
                _logger.LogError("[Modbus] Socket error connecting to device {DeviceId} ({IpAddress}:{Port}): {ErrorCode} - {Message}. Elapsed: {ElapsedMs}ms",
                    deviceId, ipAddress, port, sockEx.SocketErrorCode, sockEx.Message, sw.ElapsedMilliseconds);
                return null;
            }
            catch (Exception ex)
            {
                sw.Stop();
                Interlocked.Increment(ref _totalFailures);
                _logger.LogError(ex, "[Modbus] Failed to connect to device {DeviceId} ({IpAddress}:{Port}) after {ElapsedMs}ms",
                    deviceId, ipAddress, port, sw.ElapsedMilliseconds);
                return null;
            }
        }
        finally
        {
            _connectionLock.Release();
        }
    }

    public void InvalidateConnection(int deviceId)
    {
        if (_connections.TryGetValue(deviceId, out var connection))
        {
            _logger.LogWarning("[Modbus] Invalidating connection for device {DeviceId} ({IpAddress}:{Port}) - will reconnect on next access",
                deviceId, connection.IpAddress, connection.Port);
            connection.MarkDisconnected();
        }
    }

    public void RemoveConnection(int deviceId)
    {
        if (_connections.TryRemove(deviceId, out var connection))
        {
            _logger.LogInformation("[Modbus] Removing connection for device {DeviceId} ({IpAddress}:{Port})",
                deviceId, connection.IpAddress, connection.Port);
            connection.Dispose();
        }
    }

    private async Task CloseConnectionAsync(int deviceId)
    {
        if (_connections.TryRemove(deviceId, out var connection))
        {
            _logger.LogDebug("[Modbus] Closing connection for device {DeviceId}", deviceId);
            connection.Dispose();
            await Task.CompletedTask; // Allow for async cleanup if needed
        }
    }

    public ModbusConnectionStats GetStats()
    {
        return new ModbusConnectionStats
        {
            ActiveConnections = _connections.Count(c => c.Value.IsConnected),
            TotalConnections = _totalConnections,
            TotalReconnects = _totalReconnects,
            TotalFailures = _totalFailures
        };
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        _logger.LogInformation("[Modbus] Disposing connection manager, closing {Count} connections...", _connections.Count);

        foreach (var kvp in _connections)
        {
            try
            {
                kvp.Value.Dispose();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "[Modbus] Error disposing connection for device {DeviceId}", kvp.Key);
            }
        }

        _connections.Clear();
        _connectionLock.Dispose();

        _logger.LogInformation("[Modbus] Connection manager disposed. Stats: Total={Total}, Reconnects={Reconnects}, Failures={Failures}",
            _totalConnections, _totalReconnects, _totalFailures);
    }

    /// <summary>
    /// Represents a persistent Modbus connection.
    /// </summary>
    private class ModbusConnection : IDisposable
    {
        private readonly TcpClient _tcpClient;
        private bool _isConnected;

        public IModbusMaster Master { get; }
        public string IpAddress { get; }
        public int Port { get; }

        public bool IsConnected
        {
            get
            {
                if (!_isConnected) return false;

                try
                {
                    // Check if socket is still connected
                    return _tcpClient.Connected &&
                           !(_tcpClient.Client.Poll(1, SelectMode.SelectRead) && _tcpClient.Client.Available == 0);
                }
                catch
                {
                    _isConnected = false;
                    return false;
                }
            }
        }

        public ModbusConnection(TcpClient tcpClient, IModbusMaster master, string ipAddress, int port)
        {
            _tcpClient = tcpClient;
            Master = master;
            IpAddress = ipAddress;
            Port = port;
            _isConnected = true;
        }

        public void MarkDisconnected()
        {
            _isConnected = false;
        }

        public void Dispose()
        {
            _isConnected = false;
            try
            {
                Master?.Dispose();
            }
            catch { }
            try
            {
                _tcpClient?.Close();
                _tcpClient?.Dispose();
            }
            catch { }
        }
    }
}
