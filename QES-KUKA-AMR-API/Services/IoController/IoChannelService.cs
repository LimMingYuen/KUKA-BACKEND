using Microsoft.EntityFrameworkCore;
using QES_KUKA_AMR_API.Data;
using QES_KUKA_AMR_API.Data.Entities;

namespace QES_KUKA_AMR_API.Services.IoController;

/// <summary>
/// Service for managing IO channels and controlling digital outputs.
/// </summary>
public class IoChannelService : IIoChannelService
{
    private readonly ApplicationDbContext _dbContext;
    private readonly IModbusTcpService _modbusService;
    private readonly IIoStateLogService _logService;
    private readonly ILogger<IoChannelService> _logger;
    private readonly TimeProvider _timeProvider;

    public IoChannelService(
        ApplicationDbContext dbContext,
        IModbusTcpService modbusService,
        IIoStateLogService logService,
        ILogger<IoChannelService> logger,
        TimeProvider timeProvider)
    {
        _dbContext = dbContext;
        _modbusService = modbusService;
        _logService = logService;
        _logger = logger;
        _timeProvider = timeProvider;
    }

    public async Task<List<IoChannel>> GetChannelsByDeviceAsync(int deviceId, CancellationToken ct = default)
    {
        return await _dbContext.IoChannels
            .AsNoTracking()
            .Where(c => c.DeviceId == deviceId)
            .OrderBy(c => c.ChannelType)
            .ThenBy(c => c.ChannelNumber)
            .ToListAsync(ct);
    }

    public async Task<IoChannel?> GetChannelAsync(int deviceId, int channelNumber, IoChannelType channelType, CancellationToken ct = default)
    {
        return await _dbContext.IoChannels
            .AsNoTracking()
            .FirstOrDefaultAsync(c =>
                c.DeviceId == deviceId &&
                c.ChannelNumber == channelNumber &&
                c.ChannelType == channelType, ct);
    }

    public async Task<IoChannel?> UpdateChannelLabelAsync(int deviceId, int channelNumber, IoChannelType channelType, string? label, CancellationToken ct = default)
    {
        var channel = await _dbContext.IoChannels
            .FirstOrDefaultAsync(c =>
                c.DeviceId == deviceId &&
                c.ChannelNumber == channelNumber &&
                c.ChannelType == channelType, ct);

        if (channel == null)
        {
            return null;
        }

        channel.Label = label;
        await _dbContext.SaveChangesAsync(ct);

        _logger.LogDebug("Updated label for {Type} {Channel} on device {DeviceId}: {Label}",
            channelType, channelNumber, deviceId, label);

        return channel;
    }

    public async Task<IoWriteResult> SetDigitalOutputAsync(int deviceId, int channelNumber, bool value, string username, string? reason, CancellationToken ct = default)
    {
        // Validate channel number
        if (channelNumber < 0 || channelNumber > 7)
        {
            return IoWriteResult.Failure($"Invalid channel number: {channelNumber}. Must be 0-7.");
        }

        // Get device
        var device = await _dbContext.IoControllerDevices
            .AsNoTracking()
            .FirstOrDefaultAsync(d => d.Id == deviceId, ct);

        if (device == null)
        {
            return IoWriteResult.Failure($"Device with ID {deviceId} not found.");
        }

        // Get channel
        var channel = await _dbContext.IoChannels
            .FirstOrDefaultAsync(c =>
                c.DeviceId == deviceId &&
                c.ChannelNumber == channelNumber &&
                c.ChannelType == IoChannelType.DigitalOutput, ct);

        if (channel == null)
        {
            return IoWriteResult.Failure($"DO channel {channelNumber} not found for device {deviceId}.");
        }

        var previousState = channel.CurrentState;

        // Write to Modbus device
        var result = await _modbusService.WriteDigitalOutputAsync(
            device.IpAddress,
            device.Port,
            device.UnitId,
            channelNumber,
            value,
            ct);

        if (!result.Success)
        {
            return result;
        }

        // Update channel state in database
        var now = _timeProvider.GetUtcNow().UtcDateTime;
        channel.CurrentState = value;
        channel.LastStateChangeUtc = now;
        await _dbContext.SaveChangesAsync(ct);

        // Log the state change
        await _logService.LogStateChangeAsync(
            deviceId,
            channelNumber,
            IoChannelType.DigitalOutput,
            previousState,
            value,
            IoStateChangeSource.User,
            username,
            reason,
            ct);

        _logger.LogInformation("User {Username} set DO{Channel} to {Value} on device {DeviceName}",
            username, channelNumber, value ? "ON" : "OFF", device.DeviceName);

        return IoWriteResult.Ok();
    }

    public async Task<IoWriteResult> SetFsvAsync(int deviceId, int channelNumber, bool enabled, bool value, string username, CancellationToken ct = default)
    {
        // Validate channel number
        if (channelNumber < 0 || channelNumber > 7)
        {
            return IoWriteResult.Failure($"Invalid channel number: {channelNumber}. Must be 0-7.");
        }

        // Get device
        var device = await _dbContext.IoControllerDevices
            .AsNoTracking()
            .FirstOrDefaultAsync(d => d.Id == deviceId, ct);

        if (device == null)
        {
            return IoWriteResult.Failure($"Device with ID {deviceId} not found.");
        }

        // Get channel
        var channel = await _dbContext.IoChannels
            .FirstOrDefaultAsync(c =>
                c.DeviceId == deviceId &&
                c.ChannelNumber == channelNumber &&
                c.ChannelType == IoChannelType.DigitalOutput, ct);

        if (channel == null)
        {
            return IoWriteResult.Failure($"DO channel {channelNumber} not found for device {deviceId}.");
        }

        // Write to Modbus device (if FSV registers are implemented)
        var result = await _modbusService.WriteFsvSettingAsync(
            device.IpAddress,
            device.Port,
            device.UnitId,
            channelNumber,
            enabled,
            value,
            ct);

        if (!result.Success)
        {
            return result;
        }

        // Update channel FSV settings in database
        channel.FsvEnabled = enabled;
        channel.FailSafeValue = value;
        await _dbContext.SaveChangesAsync(ct);

        _logger.LogInformation("User {Username} set FSV for DO{Channel} to Enabled={Enabled}, Value={Value} on device {DeviceName}",
            username, channelNumber, enabled, value ? "HIGH" : "LOW", device.DeviceName);

        return IoWriteResult.Ok();
    }

    public async Task<List<IoChannel>> UpdateChannelStatesAsync(int deviceId, IoChannelType channelType, bool[] states, CancellationToken ct = default)
    {
        var changedChannels = new List<IoChannel>();
        var now = _timeProvider.GetUtcNow().UtcDateTime;

        // Get all channels for this device and type
        var channels = await _dbContext.IoChannels
            .Where(c => c.DeviceId == deviceId && c.ChannelType == channelType)
            .OrderBy(c => c.ChannelNumber)
            .ToListAsync(ct);

        for (int i = 0; i < Math.Min(states.Length, channels.Count); i++)
        {
            var channel = channels.FirstOrDefault(c => c.ChannelNumber == i);
            if (channel == null) continue;

            var previousState = channel.CurrentState;
            var newState = states[i];

            if (previousState != newState)
            {
                channel.CurrentState = newState;
                channel.LastStateChangeUtc = now;
                changedChannels.Add(channel);

                // Log the state change (system-initiated)
                await _logService.LogStateChangeAsync(
                    deviceId,
                    i,
                    channelType,
                    previousState,
                    newState,
                    IoStateChangeSource.System,
                    null,
                    null,
                    ct);
            }
        }

        if (changedChannels.Count > 0)
        {
            await _dbContext.SaveChangesAsync(ct);
            _logger.LogDebug("Updated {Count} {Type} channel states for device {DeviceId}",
                changedChannels.Count, channelType, deviceId);
        }

        return changedChannels;
    }
}
