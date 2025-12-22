namespace QES_KUKA_AMR_API.Services.IoController;

/// <summary>
/// Service for communicating with Modbus TCP devices (e.g., ADAM-6052).
/// Provides methods for reading/writing digital I/O channels.
/// </summary>
public interface IModbusTcpService
{
    /// <summary>
    /// Read all Digital Inputs (DI 0-7) from a device.
    /// ADAM-6052: Coil addresses 1-8 (0x0000-0x0007).
    /// </summary>
    /// <param name="ipAddress">Device IP address</param>
    /// <param name="port">Modbus TCP port (default 502)</param>
    /// <param name="unitId">Modbus unit/slave ID</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Read result with channel states</returns>
    Task<IoReadResult> ReadDigitalInputsAsync(string ipAddress, int port, byte unitId, CancellationToken ct = default);

    /// <summary>
    /// Read all Digital Outputs (DO 0-7) from a device.
    /// ADAM-6052: Coil addresses 17-24 (0x0010-0x0017).
    /// </summary>
    Task<IoReadResult> ReadDigitalOutputsAsync(string ipAddress, int port, byte unitId, CancellationToken ct = default);

    /// <summary>
    /// Write a single Digital Output channel.
    /// </summary>
    /// <param name="ipAddress">Device IP address</param>
    /// <param name="port">Modbus TCP port</param>
    /// <param name="unitId">Modbus unit/slave ID</param>
    /// <param name="channel">Channel number (0-7)</param>
    /// <param name="value">Value to write (true = ON, false = OFF)</param>
    /// <param name="ct">Cancellation token</param>
    Task<IoWriteResult> WriteDigitalOutputAsync(string ipAddress, int port, byte unitId, int channel, bool value, CancellationToken ct = default);

    /// <summary>
    /// Read FSV (Fail-Safe Value) settings for all DO channels.
    /// </summary>
    Task<IoFsvResult> ReadFsvSettingsAsync(string ipAddress, int port, byte unitId, CancellationToken ct = default);

    /// <summary>
    /// Write FSV setting for a specific DO channel.
    /// </summary>
    /// <param name="channel">Channel number (0-7)</param>
    /// <param name="enabled">Whether FSV is enabled for this channel</param>
    /// <param name="value">FSV value (true = HIGH, false = LOW)</param>
    Task<IoWriteResult> WriteFsvSettingAsync(string ipAddress, int port, byte unitId, int channel, bool enabled, bool value, CancellationToken ct = default);

    /// <summary>
    /// Read WDT (Watchdog Timer) settings from a device.
    /// </summary>
    Task<IoWdtResult> ReadWdtSettingsAsync(string ipAddress, int port, byte unitId, CancellationToken ct = default);

    /// <summary>
    /// Write WDT settings to a device.
    /// </summary>
    Task<IoWriteResult> WriteWdtSettingsAsync(string ipAddress, int port, byte unitId, WdtSettings settings, CancellationToken ct = default);

    /// <summary>
    /// Test connection to a Modbus device by attempting to read DI channels.
    /// </summary>
    /// <param name="timeoutMs">Connection timeout in milliseconds</param>
    Task<IoConnectionResult> TestConnectionAsync(string ipAddress, int port, byte unitId, int timeoutMs, CancellationToken ct = default);
}
