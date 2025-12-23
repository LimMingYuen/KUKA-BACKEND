using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using QES_KUKA_AMR_API.Data;
using QES_KUKA_AMR_API.Data.Entities;
using QES_KUKA_AMR_API.Options;

namespace QES_KUKA_AMR_API.Services.IoController;

/// <summary>
/// Background service that polls IO controller devices for state changes
/// and broadcasts updates via SignalR.
/// Supports demand-based polling - only polls when clients are connected.
/// </summary>
public class IoPollingHostedService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<IoPollingHostedService> _logger;
    private readonly IIoConnectionTracker _connectionTracker;
    private readonly IIoNotificationService _notificationService;
    private readonly IoPollingOptions _options;

    // Track polling state for logging
    private bool _wasPolling = false;

    public IoPollingHostedService(
        IServiceProvider serviceProvider,
        ILogger<IoPollingHostedService> logger,
        IIoConnectionTracker connectionTracker,
        IIoNotificationService notificationService,
        IOptions<IoPollingOptions> options)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
        _connectionTracker = connectionTracker;
        _notificationService = notificationService;
        _options = options.Value;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_options.Enabled)
        {
            _logger.LogInformation("IO Polling Service is disabled.");
            return;
        }

        _logger.LogInformation(
            "IO Polling Service starting (interval: {Interval}ms, demand-based: {DemandBased})...",
            _options.DefaultPollingIntervalMs,
            _options.DemandBasedPolling);

        // Wait for application to fully start
        await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                // Check if we should poll (demand-based or always)
                bool shouldPoll = !_options.DemandBasedPolling || _connectionTracker.HasActiveSubscriptions;

                if (shouldPoll)
                {
                    if (!_wasPolling)
                    {
                        _logger.LogInformation("Starting IO device polling - clients connected");
                        _wasPolling = true;
                    }

                    await PollAllDevicesAsync(stoppingToken);
                }
                else
                {
                    if (_wasPolling)
                    {
                        _logger.LogInformation("Pausing IO device polling - no active clients");
                        _wasPolling = false;
                    }
                }
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in IO polling cycle");
            }

            await Task.Delay(_options.DefaultPollingIntervalMs, stoppingToken);
        }

        _logger.LogInformation("IO Polling Service stopped.");
    }

    private async Task PollAllDevicesAsync(CancellationToken ct)
    {
        using var scope = _serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var modbusService = scope.ServiceProvider.GetRequiredService<IModbusTcpService>();
        var channelService = scope.ServiceProvider.GetRequiredService<IIoChannelService>();
        var deviceService = scope.ServiceProvider.GetRequiredService<IIoControllerDeviceService>();

        // Get active devices
        var devices = await dbContext.IoControllerDevices
            .AsNoTracking()
            .Where(d => d.IsActive)
            .ToListAsync(ct);

        if (devices.Count == 0)
        {
            return;
        }

        // Get subscribed device IDs (if demand-based)
        var subscribedIds = _connectionTracker.GetSubscribedDeviceIds();
        bool hasAllSubscription = subscribedIds.Contains(null); // null means "all devices"

        // Filter to only subscribed devices (if demand-based and not subscribed to "all")
        if (_options.DemandBasedPolling && !hasAllSubscription && subscribedIds.Count > 0)
        {
            var subscribedDeviceIds = subscribedIds.Where(id => id.HasValue).Select(id => id!.Value).ToHashSet();
            devices = devices.Where(d => subscribedDeviceIds.Contains(d.Id)).ToList();
        }

        // Poll devices (could be parallelized with SemaphoreSlim for MaxConcurrentPolls)
        foreach (var device in devices)
        {
            await PollDeviceAsync(device, modbusService, channelService, deviceService, ct);
        }
    }

    private async Task PollDeviceAsync(
        IoControllerDevice device,
        IModbusTcpService modbusService,
        IIoChannelService channelService,
        IIoControllerDeviceService deviceService,
        CancellationToken ct)
    {
        var pollStart = DateTime.UtcNow;

        _logger.LogDebug("[Polling] Starting poll for device {DeviceName} (ID: {DeviceId}, {IpAddress}:{Port}, UnitId: {UnitId})",
            device.DeviceName, device.Id, device.IpAddress, device.Port, device.UnitId);

        try
        {
            // Read Digital Inputs
            _logger.LogDebug("[Polling] Reading DI from {DeviceName}...", device.DeviceName);
            var diResult = await modbusService.ReadDigitalInputsAsync(
                device.IpAddress, device.Port, device.UnitId, ct);

            if (!diResult.Success)
            {
                _logger.LogWarning("[Polling] DI read FAILED for {DeviceName}: {Error}",
                    device.DeviceName, diResult.ErrorMessage);
            }
            else
            {
                _logger.LogDebug("[Polling] DI read OK for {DeviceName}: [{States}]",
                    device.DeviceName, string.Join(",", diResult.ChannelStates.Select(s => s ? "1" : "0")));
            }

            // Read Digital Outputs
            _logger.LogDebug("[Polling] Reading DO from {DeviceName}...", device.DeviceName);
            var doResult = await modbusService.ReadDigitalOutputsAsync(
                device.IpAddress, device.Port, device.UnitId, ct);

            if (!doResult.Success)
            {
                _logger.LogWarning("[Polling] DO read FAILED for {DeviceName}: {Error}",
                    device.DeviceName, doResult.ErrorMessage);
            }
            else
            {
                _logger.LogDebug("[Polling] DO read OK for {DeviceName}: [{States}]",
                    device.DeviceName, string.Join(",", doResult.ChannelStates.Select(s => s ? "1" : "0")));
            }

            bool connectionSuccess = diResult.Success && doResult.Success;
            string? errorMessage = !connectionSuccess
                ? (diResult.ErrorMessage ?? doResult.ErrorMessage)
                : null;

            // Update device connection status
            await deviceService.UpdatePollingStatusAsync(device.Id, connectionSuccess, errorMessage, ct);

            var elapsed = (DateTime.UtcNow - pollStart).TotalMilliseconds;

            if (connectionSuccess)
            {
                // Update DI channel states and get changed channels
                var diChanges = await channelService.UpdateChannelStatesAsync(
                    device.Id, IoChannelType.DigitalInput, diResult.ChannelStates, ct);

                // Update DO channel states and get changed channels
                var doChanges = await channelService.UpdateChannelStatesAsync(
                    device.Id, IoChannelType.DigitalOutput, doResult.ChannelStates, ct);

                var totalChanges = diChanges.Count + doChanges.Count;
                if (totalChanges > 0)
                {
                    _logger.LogInformation("[Polling] Device {DeviceName} has {Changes} channel changes (DI: {DiChanges}, DO: {DoChanges})",
                        device.DeviceName, totalChanges, diChanges.Count, doChanges.Count);
                }

                // Broadcast changes if any
                foreach (var channel in diChanges.Concat(doChanges))
                {
                    await _notificationService.BroadcastChannelChangeAsync(device.Id, channel, ct);
                }

                _logger.LogDebug("[Polling] Poll completed for {DeviceName} in {ElapsedMs}ms - SUCCESS",
                    device.DeviceName, elapsed);
            }
            else
            {
                // Broadcast connection failure
                await _notificationService.BroadcastConnectionStatusAsync(
                    device.Id, false, errorMessage, ct);

                _logger.LogWarning("[Polling] Poll completed for {DeviceName} in {ElapsedMs}ms - FAILED: {Error}",
                    device.DeviceName, elapsed, errorMessage);
            }
        }
        catch (Exception ex)
        {
            var elapsed = (DateTime.UtcNow - pollStart).TotalMilliseconds;
            _logger.LogError(ex, "[Polling] Poll EXCEPTION for device {DeviceName} ({IpAddress}:{Port}) after {ElapsedMs}ms: {Message}",
                device.DeviceName, device.IpAddress, device.Port, elapsed, ex.Message);

            // Update device with error status
            await deviceService.UpdatePollingStatusAsync(device.Id, false, ex.Message, ct);

            // Broadcast connection failure
            await _notificationService.BroadcastConnectionStatusAsync(
                device.Id, false, ex.Message, ct);
        }
    }
}
