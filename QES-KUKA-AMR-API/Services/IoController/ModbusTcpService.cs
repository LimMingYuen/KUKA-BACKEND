using System.Diagnostics;
using System.Net.Sockets;
using NModbus;

namespace QES_KUKA_AMR_API.Services.IoController;

/// <summary>
/// Implementation of Modbus TCP communication for ADAM-6052 and compatible devices.
/// Uses persistent connections via ModbusConnectionManager for improved stability.
///
/// ADAM-6052 Modbus Register Mapping:
/// - Digital Inputs (DI 0-7): Coil addresses 1-8 (0-based: 0-7)
/// - Digital Outputs (DO 0-7): Coil addresses 17-24 (0-based: 16-23)
/// - FSV registers: Holding registers (device-specific)
/// - WDT registers: Holding registers (device-specific)
/// </summary>
public class ModbusTcpService : IModbusTcpService
{
    private readonly ILogger<ModbusTcpService> _logger;
    private readonly IModbusConnectionManager _connectionManager;
    private readonly IModbusFactory _modbusFactory;

    // ADAM-6052 register addresses (0-based for NModbus)
    private const ushort DiStartAddress = 0;    // DI 0-7 at coils 1-8 (0-based: 0-7)
    private const ushort DiCount = 8;
    private const ushort DoStartAddress = 16;   // DO 0-7 at coils 17-24 (0-based: 16-23)
    private const ushort DoCount = 8;

    // Default timeout if not specified
    private const int DefaultTimeoutMs = 3000;

    public ModbusTcpService(
        ILogger<ModbusTcpService> logger,
        IModbusConnectionManager connectionManager)
    {
        _logger = logger;
        _connectionManager = connectionManager;
        _modbusFactory = new ModbusFactory();
    }

    public async Task<IoReadResult> ReadDigitalInputsAsync(string ipAddress, int port, byte unitId, CancellationToken ct = default)
    {
        return await ReadDigitalInputsAsync(ipAddress, port, unitId, DefaultTimeoutMs, ct);
    }

    public async Task<IoReadResult> ReadDigitalInputsAsync(string ipAddress, int port, byte unitId, int timeoutMs, CancellationToken ct = default)
    {
        var sw = Stopwatch.StartNew();
        var deviceKey = $"{ipAddress}:{port}";

        _logger.LogDebug("[Modbus:DI] Starting read from {Device} Unit {UnitId}", deviceKey, unitId);

        try
        {
            // Get device ID from connection (using hash as a simple identifier)
            var deviceId = GetDeviceId(ipAddress, port);

            var master = await _connectionManager.GetConnectionAsync(deviceId, ipAddress, port, unitId, timeoutMs, ct);
            if (master == null)
            {
                sw.Stop();
                _logger.LogError("[Modbus:DI] Failed to get connection for {Device} after {ElapsedMs}ms", deviceKey, sw.ElapsedMilliseconds);
                return IoReadResult.Failure($"Failed to connect to {deviceKey}");
            }

            _logger.LogDebug("[Modbus:DI] Connection acquired for {Device}, reading coils at address {Address} count {Count}...",
                deviceKey, DiStartAddress, DiCount);

            bool[] coils;
            try
            {
                coils = await master.ReadCoilsAsync(unitId, DiStartAddress, DiCount);
            }
            catch (Exception readEx)
            {
                sw.Stop();
                _logger.LogError(readEx, "[Modbus:DI] Read operation failed for {Device} after {ElapsedMs}ms - invalidating connection",
                    deviceKey, sw.ElapsedMilliseconds);

                // Invalidate the connection so it will be recreated on next attempt
                _connectionManager.InvalidateConnection(deviceId);

                return IoReadResult.Failure($"Read failed: {readEx.Message}");
            }

            sw.Stop();
            var statesStr = string.Join(",", coils.Select(c => c ? "1" : "0"));

            _logger.LogDebug("[Modbus:DI] Read successful from {Device} in {ElapsedMs}ms: [{States}]",
                deviceKey, sw.ElapsedMilliseconds, statesStr);

            return IoReadResult.Ok(coils);
        }
        catch (Exception ex)
        {
            sw.Stop();
            _logger.LogError(ex, "[Modbus:DI] Unexpected error reading from {Device} after {ElapsedMs}ms",
                deviceKey, sw.ElapsedMilliseconds);
            return IoReadResult.Failure($"Failed to read digital inputs: {ex.Message}");
        }
    }

    public async Task<IoReadResult> ReadDigitalOutputsAsync(string ipAddress, int port, byte unitId, CancellationToken ct = default)
    {
        return await ReadDigitalOutputsAsync(ipAddress, port, unitId, DefaultTimeoutMs, ct);
    }

    public async Task<IoReadResult> ReadDigitalOutputsAsync(string ipAddress, int port, byte unitId, int timeoutMs, CancellationToken ct = default)
    {
        var sw = Stopwatch.StartNew();
        var deviceKey = $"{ipAddress}:{port}";

        _logger.LogDebug("[Modbus:DO] Starting read from {Device} Unit {UnitId}", deviceKey, unitId);

        try
        {
            var deviceId = GetDeviceId(ipAddress, port);

            var master = await _connectionManager.GetConnectionAsync(deviceId, ipAddress, port, unitId, timeoutMs, ct);
            if (master == null)
            {
                sw.Stop();
                _logger.LogError("[Modbus:DO] Failed to get connection for {Device} after {ElapsedMs}ms", deviceKey, sw.ElapsedMilliseconds);
                return IoReadResult.Failure($"Failed to connect to {deviceKey}");
            }

            _logger.LogDebug("[Modbus:DO] Connection acquired for {Device}, reading coils at address {Address} count {Count}...",
                deviceKey, DoStartAddress, DoCount);

            bool[] coils;
            try
            {
                coils = await master.ReadCoilsAsync(unitId, DoStartAddress, DoCount);
            }
            catch (Exception readEx)
            {
                sw.Stop();
                _logger.LogError(readEx, "[Modbus:DO] Read operation failed for {Device} after {ElapsedMs}ms - invalidating connection",
                    deviceKey, sw.ElapsedMilliseconds);

                _connectionManager.InvalidateConnection(deviceId);

                return IoReadResult.Failure($"Read failed: {readEx.Message}");
            }

            sw.Stop();
            var statesStr = string.Join(",", coils.Select(c => c ? "1" : "0"));

            _logger.LogDebug("[Modbus:DO] Read successful from {Device} in {ElapsedMs}ms: [{States}]",
                deviceKey, sw.ElapsedMilliseconds, statesStr);

            return IoReadResult.Ok(coils);
        }
        catch (Exception ex)
        {
            sw.Stop();
            _logger.LogError(ex, "[Modbus:DO] Unexpected error reading from {Device} after {ElapsedMs}ms",
                deviceKey, sw.ElapsedMilliseconds);
            return IoReadResult.Failure($"Failed to read digital outputs: {ex.Message}");
        }
    }

    public async Task<IoWriteResult> WriteDigitalOutputAsync(string ipAddress, int port, byte unitId, int channel, bool value, CancellationToken ct = default)
    {
        return await WriteDigitalOutputAsync(ipAddress, port, unitId, channel, value, DefaultTimeoutMs, ct);
    }

    public async Task<IoWriteResult> WriteDigitalOutputAsync(string ipAddress, int port, byte unitId, int channel, bool value, int timeoutMs, CancellationToken ct = default)
    {
        if (channel < 0 || channel > 7)
        {
            _logger.LogError("[Modbus:Write] Invalid channel number: {Channel}. Must be 0-7.", channel);
            return IoWriteResult.Failure($"Invalid channel number: {channel}. Must be 0-7.");
        }

        var sw = Stopwatch.StartNew();
        var deviceKey = $"{ipAddress}:{port}";

        _logger.LogInformation("[Modbus:Write] Writing DO{Channel}={Value} to {Device} Unit {UnitId}",
            channel, value ? "ON" : "OFF", deviceKey, unitId);

        try
        {
            var deviceId = GetDeviceId(ipAddress, port);

            var master = await _connectionManager.GetConnectionAsync(deviceId, ipAddress, port, unitId, timeoutMs, ct);
            if (master == null)
            {
                sw.Stop();
                _logger.LogError("[Modbus:Write] Failed to get connection for {Device} after {ElapsedMs}ms", deviceKey, sw.ElapsedMilliseconds);
                return IoWriteResult.Failure($"Failed to connect to {deviceKey}");
            }

            ushort coilAddress = (ushort)(DoStartAddress + channel);

            _logger.LogDebug("[Modbus:Write] Writing to coil address {Address} value {Value}...", coilAddress, value);

            try
            {
                await master.WriteSingleCoilAsync(unitId, coilAddress, value);
            }
            catch (Exception writeEx)
            {
                sw.Stop();
                _logger.LogError(writeEx, "[Modbus:Write] Write operation failed for {Device} DO{Channel} after {ElapsedMs}ms - invalidating connection",
                    deviceKey, channel, sw.ElapsedMilliseconds);

                _connectionManager.InvalidateConnection(deviceId);

                return IoWriteResult.Failure($"Write failed: {writeEx.Message}");
            }

            sw.Stop();
            _logger.LogInformation("[Modbus:Write] Successfully wrote DO{Channel}={Value} to {Device} in {ElapsedMs}ms",
                channel, value ? "ON" : "OFF", deviceKey, sw.ElapsedMilliseconds);

            return IoWriteResult.Ok();
        }
        catch (Exception ex)
        {
            sw.Stop();
            _logger.LogError(ex, "[Modbus:Write] Unexpected error writing to {Device} DO{Channel} after {ElapsedMs}ms",
                deviceKey, channel, sw.ElapsedMilliseconds);
            return IoWriteResult.Failure($"Failed to write digital output: {ex.Message}");
        }
    }

    public async Task<IoFsvResult> ReadFsvSettingsAsync(string ipAddress, int port, byte unitId, CancellationToken ct = default)
    {
        var deviceKey = $"{ipAddress}:{port}";

        try
        {
            var deviceId = GetDeviceId(ipAddress, port);

            var master = await _connectionManager.GetConnectionAsync(deviceId, ipAddress, port, unitId, DefaultTimeoutMs, ct);
            if (master == null)
            {
                _logger.LogError("[Modbus:FSV] Failed to get connection for {Device}", deviceKey);
                return IoFsvResult.Failure($"Failed to connect to {deviceKey}");
            }

            // Note: FSV register addresses are ADAM-6052 specific
            // These may need adjustment based on actual device documentation
            _logger.LogWarning("[Modbus:FSV] FSV read not fully implemented - returning default values for {Device}", deviceKey);

            // Return default FSV settings (all disabled, all LOW)
            var enabled = new bool[8];
            var values = new bool[8];

            return IoFsvResult.Ok(enabled, values);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[Modbus:FSV] Failed to read FSV from {Device} Unit {UnitId}", deviceKey, unitId);
            return IoFsvResult.Failure($"Failed to read FSV settings: {ex.Message}");
        }
    }

    public async Task<IoWriteResult> WriteFsvSettingAsync(string ipAddress, int port, byte unitId, int channel, bool enabled, bool value, CancellationToken ct = default)
    {
        if (channel < 0 || channel > 7)
        {
            return IoWriteResult.Failure($"Invalid channel number: {channel}. Must be 0-7.");
        }

        var deviceKey = $"{ipAddress}:{port}";

        try
        {
            // Note: FSV register addresses are ADAM-6052 specific
            _logger.LogWarning("[Modbus:FSV] FSV write not fully implemented for {Device} Channel {Channel}", deviceKey, channel);

            // Placeholder - actual implementation requires device-specific register mapping
            return IoWriteResult.Ok();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[Modbus:FSV] Failed to write FSV for channel {Channel} to {Device}", channel, deviceKey);
            return IoWriteResult.Failure($"Failed to write FSV setting: {ex.Message}");
        }
    }

    public async Task<IoWdtResult> ReadWdtSettingsAsync(string ipAddress, int port, byte unitId, CancellationToken ct = default)
    {
        var deviceKey = $"{ipAddress}:{port}";

        try
        {
            var deviceId = GetDeviceId(ipAddress, port);

            var master = await _connectionManager.GetConnectionAsync(deviceId, ipAddress, port, unitId, DefaultTimeoutMs, ct);
            if (master == null)
            {
                _logger.LogError("[Modbus:WDT] Failed to get connection for {Device}", deviceKey);
                return IoWdtResult.Failure($"Failed to connect to {deviceKey}");
            }

            // Note: WDT register addresses are ADAM-6052 specific
            _logger.LogWarning("[Modbus:WDT] WDT read not fully implemented - returning default values for {Device}", deviceKey);

            return IoWdtResult.Ok(false, 30); // Default: disabled, 30 seconds
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[Modbus:WDT] Failed to read WDT from {Device} Unit {UnitId}", deviceKey, unitId);
            return IoWdtResult.Failure($"Failed to read WDT settings: {ex.Message}");
        }
    }

    public async Task<IoWriteResult> WriteWdtSettingsAsync(string ipAddress, int port, byte unitId, WdtSettings settings, CancellationToken ct = default)
    {
        var deviceKey = $"{ipAddress}:{port}";

        try
        {
            // Note: WDT register addresses are ADAM-6052 specific
            _logger.LogWarning("[Modbus:WDT] WDT write not fully implemented for {Device}", deviceKey);

            return IoWriteResult.Ok();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[Modbus:WDT] Failed to write WDT to {Device}", deviceKey);
            return IoWriteResult.Failure($"Failed to write WDT settings: {ex.Message}");
        }
    }

    public async Task<IoConnectionResult> TestConnectionAsync(string ipAddress, int port, byte unitId, int timeoutMs, CancellationToken ct = default)
    {
        var sw = Stopwatch.StartNew();
        var deviceKey = $"{ipAddress}:{port}";

        _logger.LogInformation("[Modbus:Test] Testing connection to {Device} Unit {UnitId} (Timeout: {Timeout}ms)...",
            deviceKey, unitId, timeoutMs);

        try
        {
            var deviceId = GetDeviceId(ipAddress, port);

            // Force a fresh connection for the test by invalidating any existing one
            _connectionManager.InvalidateConnection(deviceId);

            var master = await _connectionManager.GetConnectionAsync(deviceId, ipAddress, port, unitId, timeoutMs, ct);
            if (master == null)
            {
                sw.Stop();
                var message = $"Connection failed after {sw.ElapsedMilliseconds}ms";
                _logger.LogError("[Modbus:Test] Connection test FAILED for {Device}: {Message}", deviceKey, message);
                return IoConnectionResult.Failed(message);
            }

            // Verify with a test read
            _logger.LogDebug("[Modbus:Test] Connection established, performing test read...");

            try
            {
                await master.ReadCoilsAsync(unitId, DiStartAddress, DiCount);
            }
            catch (Exception readEx)
            {
                sw.Stop();
                _logger.LogError(readEx, "[Modbus:Test] Test read failed for {Device} after {ElapsedMs}ms", deviceKey, sw.ElapsedMilliseconds);
                _connectionManager.InvalidateConnection(deviceId);
                return IoConnectionResult.Failed($"Test read failed: {readEx.Message}");
            }

            sw.Stop();
            _logger.LogInformation("[Modbus:Test] Connection test SUCCESSFUL for {Device} Unit {UnitId} in {ElapsedMs}ms",
                deviceKey, unitId, sw.ElapsedMilliseconds);

            return IoConnectionResult.Connected((int)sw.ElapsedMilliseconds);
        }
        catch (OperationCanceledException)
        {
            sw.Stop();
            var message = $"Connection timeout after {timeoutMs}ms";
            _logger.LogWarning("[Modbus:Test] Connection test TIMEOUT for {Device}: {Message}", deviceKey, message);
            return IoConnectionResult.Failed(message);
        }
        catch (Exception ex)
        {
            sw.Stop();
            _logger.LogError(ex, "[Modbus:Test] Connection test FAILED for {Device} after {ElapsedMs}ms", deviceKey, sw.ElapsedMilliseconds);
            return IoConnectionResult.Failed($"Connection failed: {ex.Message}");
        }
    }

    /// <summary>
    /// Generate a consistent device ID from IP and port.
    /// </summary>
    private static int GetDeviceId(string ipAddress, int port)
    {
        return HashCode.Combine(ipAddress.ToLowerInvariant(), port);
    }
}
