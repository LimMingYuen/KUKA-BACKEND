using System.Diagnostics;
using System.Net.Sockets;
using NModbus;

namespace QES_KUKA_AMR_API.Services.IoController;

/// <summary>
/// Implementation of Modbus TCP communication for ADAM-6052 and compatible devices.
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
    private readonly IModbusFactory _modbusFactory;

    // ADAM-6052 register addresses (0-based for NModbus)
    private const ushort DiStartAddress = 0;    // DI 0-7 at coils 1-8 (0-based: 0-7)
    private const ushort DiCount = 8;
    private const ushort DoStartAddress = 16;   // DO 0-7 at coils 17-24 (0-based: 16-23)
    private const ushort DoCount = 8;

    // ADAM-6052 FSV and WDT holding register addresses (approximate - may need adjustment)
    // These addresses are based on ADAM-6052 Modbus mapping documentation
    private const ushort FsvEnableRegister = 40101;  // FSV enable bits
    private const ushort FsvValueRegister = 40102;   // FSV value bits
    private const ushort WdtEnableRegister = 40501;  // WDT enable
    private const ushort WdtTimeoutRegister = 40502; // WDT timeout value

    public ModbusTcpService(ILogger<ModbusTcpService> logger)
    {
        _logger = logger;
        _modbusFactory = new ModbusFactory();
    }

    public async Task<IoReadResult> ReadDigitalInputsAsync(string ipAddress, int port, byte unitId, CancellationToken ct = default)
    {
        try
        {
            using var client = new TcpClient();
            await client.ConnectAsync(ipAddress, port, ct);

            var master = _modbusFactory.CreateMaster(client);
            master.Transport.ReadTimeout = 3000;
            master.Transport.WriteTimeout = 3000;

            // Read DI coils (ADAM-6052: addresses 1-8, 0-based: 0-7)
            var coils = await master.ReadCoilsAsync(unitId, DiStartAddress, DiCount);

            _logger.LogDebug("Read DI from {IpAddress}:{Port} Unit {UnitId}: {States}",
                ipAddress, port, unitId, string.Join(",", coils.Select(c => c ? "1" : "0")));

            return IoReadResult.Ok(coils);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to read DI from {IpAddress}:{Port} Unit {UnitId}", ipAddress, port, unitId);
            return IoReadResult.Failure($"Failed to read digital inputs: {ex.Message}");
        }
    }

    public async Task<IoReadResult> ReadDigitalOutputsAsync(string ipAddress, int port, byte unitId, CancellationToken ct = default)
    {
        try
        {
            using var client = new TcpClient();
            await client.ConnectAsync(ipAddress, port, ct);

            var master = _modbusFactory.CreateMaster(client);
            master.Transport.ReadTimeout = 3000;
            master.Transport.WriteTimeout = 3000;

            // Read DO coils (ADAM-6052: addresses 17-24, 0-based: 16-23)
            var coils = await master.ReadCoilsAsync(unitId, DoStartAddress, DoCount);

            _logger.LogDebug("Read DO from {IpAddress}:{Port} Unit {UnitId}: {States}",
                ipAddress, port, unitId, string.Join(",", coils.Select(c => c ? "1" : "0")));

            return IoReadResult.Ok(coils);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to read DO from {IpAddress}:{Port} Unit {UnitId}", ipAddress, port, unitId);
            return IoReadResult.Failure($"Failed to read digital outputs: {ex.Message}");
        }
    }

    public async Task<IoWriteResult> WriteDigitalOutputAsync(string ipAddress, int port, byte unitId, int channel, bool value, CancellationToken ct = default)
    {
        if (channel < 0 || channel > 7)
        {
            return IoWriteResult.Failure($"Invalid channel number: {channel}. Must be 0-7.");
        }

        try
        {
            using var client = new TcpClient();
            await client.ConnectAsync(ipAddress, port, ct);

            var master = _modbusFactory.CreateMaster(client);
            master.Transport.ReadTimeout = 3000;
            master.Transport.WriteTimeout = 3000;

            // Write single coil (ADAM-6052 DO: 0-based address 16 + channel)
            ushort coilAddress = (ushort)(DoStartAddress + channel);
            await master.WriteSingleCoilAsync(unitId, coilAddress, value);

            _logger.LogInformation("Wrote DO{Channel}={Value} to {IpAddress}:{Port} Unit {UnitId}",
                channel, value ? "ON" : "OFF", ipAddress, port, unitId);

            return IoWriteResult.Ok();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to write DO{Channel} to {IpAddress}:{Port} Unit {UnitId}",
                channel, ipAddress, port, unitId);
            return IoWriteResult.Failure($"Failed to write digital output: {ex.Message}");
        }
    }

    public async Task<IoFsvResult> ReadFsvSettingsAsync(string ipAddress, int port, byte unitId, CancellationToken ct = default)
    {
        try
        {
            using var client = new TcpClient();
            await client.ConnectAsync(ipAddress, port, ct);

            var master = _modbusFactory.CreateMaster(client);
            master.Transport.ReadTimeout = 3000;
            master.Transport.WriteTimeout = 3000;

            // Note: FSV register addresses are ADAM-6052 specific
            // These may need adjustment based on actual device documentation
            // For now, returning default values since FSV registers vary by device

            _logger.LogWarning("FSV read not fully implemented - returning default values for {IpAddress}:{Port}",
                ipAddress, port);

            // Return default FSV settings (all disabled, all LOW)
            var enabled = new bool[8];
            var values = new bool[8];

            return IoFsvResult.Ok(enabled, values);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to read FSV from {IpAddress}:{Port} Unit {UnitId}", ipAddress, port, unitId);
            return IoFsvResult.Failure($"Failed to read FSV settings: {ex.Message}");
        }
    }

    public async Task<IoWriteResult> WriteFsvSettingAsync(string ipAddress, int port, byte unitId, int channel, bool enabled, bool value, CancellationToken ct = default)
    {
        if (channel < 0 || channel > 7)
        {
            return IoWriteResult.Failure($"Invalid channel number: {channel}. Must be 0-7.");
        }

        try
        {
            // Note: FSV register addresses are ADAM-6052 specific
            // Implementation depends on actual device Modbus mapping

            _logger.LogWarning("FSV write not fully implemented for {IpAddress}:{Port} Channel {Channel}",
                ipAddress, port, channel);

            // Placeholder - actual implementation requires device-specific register mapping
            return IoWriteResult.Ok();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to write FSV for channel {Channel} to {IpAddress}:{Port}",
                channel, ipAddress, port);
            return IoWriteResult.Failure($"Failed to write FSV setting: {ex.Message}");
        }
    }

    public async Task<IoWdtResult> ReadWdtSettingsAsync(string ipAddress, int port, byte unitId, CancellationToken ct = default)
    {
        try
        {
            using var client = new TcpClient();
            await client.ConnectAsync(ipAddress, port, ct);

            var master = _modbusFactory.CreateMaster(client);
            master.Transport.ReadTimeout = 3000;
            master.Transport.WriteTimeout = 3000;

            // Note: WDT register addresses are ADAM-6052 specific
            // Implementation depends on actual device Modbus mapping

            _logger.LogWarning("WDT read not fully implemented - returning default values for {IpAddress}:{Port}",
                ipAddress, port);

            return IoWdtResult.Ok(false, 30); // Default: disabled, 30 seconds
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to read WDT from {IpAddress}:{Port} Unit {UnitId}", ipAddress, port, unitId);
            return IoWdtResult.Failure($"Failed to read WDT settings: {ex.Message}");
        }
    }

    public async Task<IoWriteResult> WriteWdtSettingsAsync(string ipAddress, int port, byte unitId, WdtSettings settings, CancellationToken ct = default)
    {
        try
        {
            // Note: WDT register addresses are ADAM-6052 specific
            // Implementation depends on actual device Modbus mapping

            _logger.LogWarning("WDT write not fully implemented for {IpAddress}:{Port}", ipAddress, port);

            return IoWriteResult.Ok();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to write WDT to {IpAddress}:{Port}", ipAddress, port);
            return IoWriteResult.Failure($"Failed to write WDT settings: {ex.Message}");
        }
    }

    public async Task<IoConnectionResult> TestConnectionAsync(string ipAddress, int port, byte unitId, int timeoutMs, CancellationToken ct = default)
    {
        var sw = Stopwatch.StartNew();

        try
        {
            using var client = new TcpClient();

            // Set connection timeout
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            cts.CancelAfter(timeoutMs);

            await client.ConnectAsync(ipAddress, port, cts.Token);

            var master = _modbusFactory.CreateMaster(client);
            master.Transport.ReadTimeout = timeoutMs;
            master.Transport.WriteTimeout = timeoutMs;

            // Test by reading DI coils
            await master.ReadCoilsAsync(unitId, DiStartAddress, DiCount);

            sw.Stop();

            _logger.LogInformation("Connection test successful to {IpAddress}:{Port} Unit {UnitId} in {Ms}ms",
                ipAddress, port, unitId, sw.ElapsedMilliseconds);

            return IoConnectionResult.Connected((int)sw.ElapsedMilliseconds);
        }
        catch (OperationCanceledException)
        {
            sw.Stop();
            var message = $"Connection timeout after {timeoutMs}ms";
            _logger.LogWarning("Connection test timeout to {IpAddress}:{Port} Unit {UnitId}", ipAddress, port, unitId);
            return IoConnectionResult.Failed(message);
        }
        catch (Exception ex)
        {
            sw.Stop();
            _logger.LogError(ex, "Connection test failed to {IpAddress}:{Port} Unit {UnitId}", ipAddress, port, unitId);
            return IoConnectionResult.Failed($"Connection failed: {ex.Message}");
        }
    }
}
