using QES_KUKA_AMR_API.Data.Entities;

namespace QES_KUKA_AMR_API.Services.IoController;

/// <summary>
/// Service for managing IO channels and controlling digital outputs.
/// </summary>
public interface IIoChannelService
{
    /// <summary>
    /// Get all channels for a device.
    /// </summary>
    Task<List<IoChannel>> GetChannelsByDeviceAsync(int deviceId, CancellationToken ct = default);

    /// <summary>
    /// Get a specific channel.
    /// </summary>
    Task<IoChannel?> GetChannelAsync(int deviceId, int channelNumber, IoChannelType channelType, CancellationToken ct = default);

    /// <summary>
    /// Update the label for a channel.
    /// </summary>
    Task<IoChannel?> UpdateChannelLabelAsync(int deviceId, int channelNumber, IoChannelType channelType, string? label, CancellationToken ct = default);

    /// <summary>
    /// Set a Digital Output channel value.
    /// Writes to the Modbus device and logs the state change.
    /// </summary>
    /// <param name="deviceId">Device ID</param>
    /// <param name="channelNumber">Channel number (0-7)</param>
    /// <param name="value">Value to set (true = ON, false = OFF)</param>
    /// <param name="username">Username performing the action</param>
    /// <param name="reason">Optional reason for the change</param>
    Task<IoWriteResult> SetDigitalOutputAsync(int deviceId, int channelNumber, bool value, string username, string? reason, CancellationToken ct = default);

    /// <summary>
    /// Set FSV (Fail-Safe Value) settings for a DO channel.
    /// </summary>
    Task<IoWriteResult> SetFsvAsync(int deviceId, int channelNumber, bool enabled, bool value, string username, CancellationToken ct = default);

    /// <summary>
    /// Update channel states from Modbus read (called by polling service).
    /// Returns list of channels that changed state.
    /// </summary>
    Task<List<IoChannel>> UpdateChannelStatesAsync(int deviceId, IoChannelType channelType, bool[] states, CancellationToken ct = default);
}
