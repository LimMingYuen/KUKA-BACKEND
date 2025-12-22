using QES_KUKA_AMR_API.Data.Entities;

namespace QES_KUKA_AMR_API.Services.IoController;

/// <summary>
/// Service for managing IO controller devices (CRUD operations).
/// </summary>
public interface IIoControllerDeviceService
{
    /// <summary>
    /// Get all IO controller devices.
    /// </summary>
    Task<List<IoControllerDevice>> GetAllAsync(CancellationToken ct = default);

    /// <summary>
    /// Get a device by ID.
    /// </summary>
    Task<IoControllerDevice?> GetByIdAsync(int id, CancellationToken ct = default);

    /// <summary>
    /// Create a new IO controller device.
    /// Also initializes all 16 channels (8 DI + 8 DO) for the device.
    /// </summary>
    Task<IoControllerDevice> CreateAsync(IoControllerDevice device, string username, CancellationToken ct = default);

    /// <summary>
    /// Update an existing device.
    /// </summary>
    Task<IoControllerDevice?> UpdateAsync(int id, IoControllerDevice device, string username, CancellationToken ct = default);

    /// <summary>
    /// Delete a device and all its channels/logs.
    /// </summary>
    Task<bool> DeleteAsync(int id, CancellationToken ct = default);

    /// <summary>
    /// Test connection to a device.
    /// </summary>
    Task<IoConnectionResult> TestConnectionAsync(int id, CancellationToken ct = default);

    /// <summary>
    /// Get full device status including all channel states.
    /// </summary>
    Task<IoDeviceFullStatus?> GetDeviceStatusAsync(int id, CancellationToken ct = default);

    /// <summary>
    /// Update device polling status (called by polling service).
    /// </summary>
    Task UpdatePollingStatusAsync(int id, bool success, string? errorMessage, CancellationToken ct = default);
}

/// <summary>
/// Full status of an IO controller device including all channels.
/// </summary>
public class IoDeviceFullStatus
{
    public IoControllerDevice Device { get; set; } = null!;
    public List<IoChannel> Channels { get; set; } = new();
    public bool IsConnected { get; set; }
    public DateTime? LastPollUtc { get; set; }
}
