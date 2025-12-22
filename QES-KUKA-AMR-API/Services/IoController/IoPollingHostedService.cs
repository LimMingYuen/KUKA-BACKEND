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
        try
        {
            // Read Digital Inputs
            var diResult = await modbusService.ReadDigitalInputsAsync(
                device.IpAddress, device.Port, device.UnitId, ct);

            // Read Digital Outputs
            var doResult = await modbusService.ReadDigitalOutputsAsync(
                device.IpAddress, device.Port, device.UnitId, ct);

            bool connectionSuccess = diResult.Success && doResult.Success;
            string? errorMessage = !connectionSuccess
                ? (diResult.ErrorMessage ?? doResult.ErrorMessage)
                : null;

            // Update device connection status
            await deviceService.UpdatePollingStatusAsync(device.Id, connectionSuccess, errorMessage, ct);

            if (connectionSuccess)
            {
                // Update DI channel states and get changed channels
                var diChanges = await channelService.UpdateChannelStatesAsync(
                    device.Id, IoChannelType.DigitalInput, diResult.ChannelStates, ct);

                // Update DO channel states and get changed channels
                var doChanges = await channelService.UpdateChannelStatesAsync(
                    device.Id, IoChannelType.DigitalOutput, doResult.ChannelStates, ct);

                // Broadcast changes if any
                foreach (var channel in diChanges.Concat(doChanges))
                {
                    await _notificationService.BroadcastChannelChangeAsync(device.Id, channel, ct);
                }
            }
            else
            {
                // Broadcast connection failure
                await _notificationService.BroadcastConnectionStatusAsync(
                    device.Id, false, errorMessage, ct);

                _logger.LogWarning("Failed to poll device {DeviceName} ({IpAddress}:{Port}): {Error}",
                    device.DeviceName, device.IpAddress, device.Port, errorMessage);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error polling device {DeviceName} ({IpAddress}:{Port})",
                device.DeviceName, device.IpAddress, device.Port);

            // Update device with error status
            await deviceService.UpdatePollingStatusAsync(device.Id, false, ex.Message, ct);

            // Broadcast connection failure
            await _notificationService.BroadcastConnectionStatusAsync(
                device.Id, false, ex.Message, ct);
        }
    }
}
