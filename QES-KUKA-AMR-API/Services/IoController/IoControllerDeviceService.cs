using Microsoft.EntityFrameworkCore;
using QES_KUKA_AMR_API.Data;
using QES_KUKA_AMR_API.Data.Entities;

namespace QES_KUKA_AMR_API.Services.IoController;

/// <summary>
/// Service for managing IO controller devices.
/// </summary>
public class IoControllerDeviceService : IIoControllerDeviceService
{
    private readonly ApplicationDbContext _dbContext;
    private readonly IModbusTcpService _modbusService;
    private readonly ILogger<IoControllerDeviceService> _logger;
    private readonly TimeProvider _timeProvider;

    public IoControllerDeviceService(
        ApplicationDbContext dbContext,
        IModbusTcpService modbusService,
        ILogger<IoControllerDeviceService> logger,
        TimeProvider timeProvider)
    {
        _dbContext = dbContext;
        _modbusService = modbusService;
        _logger = logger;
        _timeProvider = timeProvider;
    }

    public async Task<List<IoControllerDevice>> GetAllAsync(CancellationToken ct = default)
    {
        return await _dbContext.IoControllerDevices
            .AsNoTracking()
            .OrderBy(d => d.DeviceName)
            .ToListAsync(ct);
    }

    public async Task<IoControllerDevice?> GetByIdAsync(int id, CancellationToken ct = default)
    {
        return await _dbContext.IoControllerDevices
            .AsNoTracking()
            .FirstOrDefaultAsync(d => d.Id == id, ct);
    }

    public async Task<IoControllerDevice> CreateAsync(IoControllerDevice device, string username, CancellationToken ct = default)
    {
        // Check for duplicate IP:Port
        var exists = await _dbContext.IoControllerDevices
            .AnyAsync(d => d.IpAddress == device.IpAddress && d.Port == device.Port, ct);

        if (exists)
        {
            throw new InvalidOperationException($"A device with IP {device.IpAddress}:{device.Port} already exists.");
        }

        var now = _timeProvider.GetUtcNow().UtcDateTime;
        device.CreatedUtc = now;
        device.UpdatedUtc = now;
        device.CreatedBy = username;
        device.UpdatedBy = username;

        _dbContext.IoControllerDevices.Add(device);
        await _dbContext.SaveChangesAsync(ct);

        // Initialize all 16 channels for this device
        await InitializeChannelsAsync(device.Id, ct);

        _logger.LogInformation("Created IO controller device {DeviceName} ({IpAddress}:{Port}) by {Username}",
            device.DeviceName, device.IpAddress, device.Port, username);

        return device;
    }

    public async Task<IoControllerDevice?> UpdateAsync(int id, IoControllerDevice device, string username, CancellationToken ct = default)
    {
        var existing = await _dbContext.IoControllerDevices.FindAsync(new object[] { id }, ct);
        if (existing == null)
        {
            return null;
        }

        // Update properties
        existing.DeviceName = device.DeviceName;
        existing.Description = device.Description;
        existing.IsActive = device.IsActive;
        existing.PollingIntervalMs = device.PollingIntervalMs;
        existing.ConnectionTimeoutMs = device.ConnectionTimeoutMs;
        existing.UpdatedUtc = _timeProvider.GetUtcNow().UtcDateTime;
        existing.UpdatedBy = username;

        // Update connection properties if provided
        if (!string.IsNullOrEmpty(device.IpAddress))
        {
            existing.IpAddress = device.IpAddress;
        }
        if (device.Port > 0)
        {
            existing.Port = device.Port;
        }
        if (device.UnitId > 0)
        {
            existing.UnitId = device.UnitId;
        }

        await _dbContext.SaveChangesAsync(ct);

        _logger.LogInformation("Updated IO controller device {DeviceName} (ID: {Id}) by {Username}",
            existing.DeviceName, id, username);

        return existing;
    }

    public async Task<bool> DeleteAsync(int id, CancellationToken ct = default)
    {
        var device = await _dbContext.IoControllerDevices.FindAsync(new object[] { id }, ct);
        if (device == null)
        {
            return false;
        }

        // Cascade delete will remove channels and logs
        _dbContext.IoControllerDevices.Remove(device);
        await _dbContext.SaveChangesAsync(ct);

        _logger.LogInformation("Deleted IO controller device {DeviceName} (ID: {Id})", device.DeviceName, id);

        return true;
    }

    public async Task<IoConnectionResult> TestConnectionAsync(int id, CancellationToken ct = default)
    {
        var device = await GetByIdAsync(id, ct);
        if (device == null)
        {
            return IoConnectionResult.Failed($"Device with ID {id} not found.");
        }

        return await _modbusService.TestConnectionAsync(
            device.IpAddress,
            device.Port,
            device.UnitId,
            device.ConnectionTimeoutMs,
            ct);
    }

    public async Task<IoDeviceFullStatus?> GetDeviceStatusAsync(int id, CancellationToken ct = default)
    {
        var device = await _dbContext.IoControllerDevices
            .AsNoTracking()
            .FirstOrDefaultAsync(d => d.Id == id, ct);

        if (device == null)
        {
            return null;
        }

        var channels = await _dbContext.IoChannels
            .AsNoTracking()
            .Where(c => c.DeviceId == id)
            .OrderBy(c => c.ChannelType)
            .ThenBy(c => c.ChannelNumber)
            .ToListAsync(ct);

        return new IoDeviceFullStatus
        {
            Device = device,
            Channels = channels,
            IsConnected = device.LastConnectionSuccess ?? false,
            LastPollUtc = device.LastPollUtc
        };
    }

    public async Task UpdatePollingStatusAsync(int id, bool success, string? errorMessage, CancellationToken ct = default)
    {
        var device = await _dbContext.IoControllerDevices.FindAsync(new object[] { id }, ct);
        if (device == null)
        {
            return;
        }

        device.LastPollUtc = _timeProvider.GetUtcNow().UtcDateTime;
        device.LastConnectionSuccess = success;
        device.LastErrorMessage = success ? null : errorMessage;
        device.UpdatedUtc = _timeProvider.GetUtcNow().UtcDateTime;

        await _dbContext.SaveChangesAsync(ct);
    }

    /// <summary>
    /// Initialize all 16 channels (8 DI + 8 DO) for a new device.
    /// </summary>
    private async Task InitializeChannelsAsync(int deviceId, CancellationToken ct)
    {
        var channels = new List<IoChannel>();

        // Create 8 Digital Input channels (DI 0-7)
        for (int i = 0; i < 8; i++)
        {
            channels.Add(new IoChannel
            {
                DeviceId = deviceId,
                ChannelNumber = i,
                ChannelType = IoChannelType.DigitalInput,
                Label = $"DI {i}",
                CurrentState = false,
                FsvEnabled = false,
                FailSafeValue = null
            });
        }

        // Create 8 Digital Output channels (DO 0-7)
        for (int i = 0; i < 8; i++)
        {
            channels.Add(new IoChannel
            {
                DeviceId = deviceId,
                ChannelNumber = i,
                ChannelType = IoChannelType.DigitalOutput,
                Label = $"DO {i}",
                CurrentState = false,
                FsvEnabled = false,
                FailSafeValue = false
            });
        }

        _dbContext.IoChannels.AddRange(channels);
        await _dbContext.SaveChangesAsync(ct);

        _logger.LogDebug("Initialized 16 channels for device ID {DeviceId}", deviceId);
    }
}
