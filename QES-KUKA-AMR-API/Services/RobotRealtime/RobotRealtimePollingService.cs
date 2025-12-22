using Microsoft.Extensions.Options;
using QES_KUKA_AMR_API.Models.MobileRobot;
using QES_KUKA_AMR_API.Models.RobotRealtime;
using QES_KUKA_AMR_API.Options;

namespace QES_KUKA_AMR_API.Services.RobotRealtime;

/// <summary>
/// Background service that polls the external AMR system for robot positions
/// and broadcasts updates via SignalR at configurable intervals.
/// Only polls when there are active SignalR connections (demand-based polling).
/// </summary>
public class RobotRealtimePollingService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<RobotRealtimePollingService> _logger;
    private readonly IRobotRealtimeNotificationService _notificationService;
    private readonly IMapSubscriptionTracker _mapSubscriptionTracker;
    private readonly RobotRealtimePollingOptions _options;

    // Cache last known positions for change detection - keyed by subscription
    private readonly Dictionary<MapSubscriptionKey, Dictionary<string, RobotPositionDto>> _lastPositionsBySubscription = new();
    private readonly object _cacheLock = new();

    // Track polling state for logging
    private bool _wasPolling = false;

    public RobotRealtimePollingService(
        IServiceProvider serviceProvider,
        ILogger<RobotRealtimePollingService> logger,
        IRobotRealtimeNotificationService notificationService,
        IMapSubscriptionTracker mapSubscriptionTracker,
        IOptions<RobotRealtimePollingOptions> options)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
        _notificationService = notificationService;
        _mapSubscriptionTracker = mapSubscriptionTracker;
        _options = options.Value;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_options.Enabled)
        {
            _logger.LogInformation("Robot Realtime Polling Service is disabled.");
            return;
        }

        _logger.LogInformation(
            "Robot Realtime Polling Service starting (interval: {Interval}ms, demand-based: only polls when clients connected)...",
            _options.PollingIntervalMs);

        // Wait for application to fully start
        await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                // Only poll if there are active map/floor subscriptions
                if (_mapSubscriptionTracker.HasActiveSubscriptions)
                {
                    var activeSubscriptions = _mapSubscriptionTracker.GetActiveSubscriptions();

                    if (!_wasPolling)
                    {
                        _logger.LogInformation(
                            "Starting robot realtime polling - {Count} active subscription(s)",
                            activeSubscriptions.Count);
                        _wasPolling = true;
                    }

                    await PollAndBroadcastAsync(activeSubscriptions, stoppingToken);
                }
                else
                {
                    if (_wasPolling)
                    {
                        _logger.LogInformation("Pausing robot realtime polling - no active subscriptions");
                        _wasPolling = false;

                        // Clear cached positions when pausing so we get fresh data on resume
                        lock (_cacheLock)
                        {
                            _lastPositionsBySubscription.Clear();
                        }
                    }
                }
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in robot realtime polling cycle");
            }

            await Task.Delay(_options.PollingIntervalMs, stoppingToken);
        }

        _logger.LogInformation("Robot Realtime Polling Service stopped.");
    }

    private async Task PollAndBroadcastAsync(
        IReadOnlyCollection<MapSubscriptionKey> subscriptions,
        CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var realtimeClient = scope.ServiceProvider.GetRequiredService<IRobotRealtimeClient>();

        var allPositions = new List<RobotPositionDto>();

        // Poll for each active subscription
        foreach (var subscription in subscriptions)
        {
            try
            {
                // Skip "all" subscriptions - external API requires mapCode/floorNumber
                // Robots will still be broadcast to "all" group via RobotRealtimeNotificationService
                if (subscription.IsAllSubscription)
                {
                    _logger.LogDebug("Skipping 'all' subscription - external API requires mapCode/floorNumber");
                    continue;
                }

                var realtimeData = await realtimeClient.GetRealtimeInfoAsync(
                    floorNumber: subscription.FloorNumber,
                    mapCode: subscription.MapCode,
                    isFirst: false,
                    cancellationToken: cancellationToken);

                if (realtimeData?.RobotRealtimeList == null || realtimeData.RobotRealtimeList.Count == 0)
                {
                    _logger.LogDebug("No robots returned for MapCode={MapCode}, FloorNumber={FloorNumber}",
                        subscription.MapCode, subscription.FloorNumber);
                    continue;
                }

                // Map to lightweight DTOs
                var positions = realtimeData.RobotRealtimeList
                    .Select(MapToPositionDto)
                    .ToList();

                // Optional: Only broadcast if positions changed (reduces network traffic)
                var positionsToSend = _options.BroadcastOnlyChanges
                    ? FilterChangedPositions(subscription, positions)
                    : positions;

                allPositions.AddRange(positionsToSend);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex,
                    "Error polling realtime info for MapCode={MapCode}, FloorNumber={FloorNumber}",
                    subscription.MapCode, subscription.FloorNumber);
            }
        }

        // Broadcast all collected positions
        if (allPositions.Count > 0)
        {
            await _notificationService.BroadcastRobotPositionsAsync(allPositions, cancellationToken);
        }
    }

    private RobotPositionDto MapToPositionDto(RobotRealtimeDto source)
    {
        return new RobotPositionDto
        {
            RobotId = source.RobotId,
            XCoordinate = source.XCoordinate,
            YCoordinate = source.YCoordinate,
            RobotOrientation = source.RobotOrientation,
            RobotStatus = source.RobotStatus,
            RobotStatusText = GetStatusText(source.RobotStatus),
            BatteryLevel = source.BatteryLevel,
            BatteryIsCharging = source.BatteryIsCharging,
            MapCode = source.MapCode,
            FloorNumber = source.FloorNumber,
            CurrentJobId = source.JobId,
            RobotTypeCode = source.RobotTypeCode,
            ConnectionState = source.ConnectionState,
            WarningLevel = source.WarningLevel,
            Velocity = source.Velocity,
            Timestamp = DateTime.UtcNow
        };
    }

    private List<RobotPositionDto> FilterChangedPositions(
        MapSubscriptionKey subscription,
        List<RobotPositionDto> positions)
    {
        var changed = new List<RobotPositionDto>();

        lock (_cacheLock)
        {
            if (!_lastPositionsBySubscription.TryGetValue(subscription, out var lastPositions))
            {
                lastPositions = new Dictionary<string, RobotPositionDto>();
                _lastPositionsBySubscription[subscription] = lastPositions;
            }

            foreach (var pos in positions)
            {
                if (!lastPositions.TryGetValue(pos.RobotId, out var lastPos) ||
                    HasSignificantChange(lastPos, pos))
                {
                    changed.Add(pos);
                    lastPositions[pos.RobotId] = pos;
                }
            }
        }

        return changed;
    }

    private static bool HasSignificantChange(RobotPositionDto last, RobotPositionDto current)
    {
        // Consider position changed if moved more than threshold
        const double positionThreshold = 0.1; // meters
        const double orientationThreshold = 1.0; // degrees
        const double batteryThreshold = 0.5; // percentage

        return Math.Abs(current.XCoordinate - last.XCoordinate) > positionThreshold ||
               Math.Abs(current.YCoordinate - last.YCoordinate) > positionThreshold ||
               Math.Abs(current.RobotOrientation - last.RobotOrientation) > orientationThreshold ||
               current.RobotStatus != last.RobotStatus ||
               current.ConnectionState != last.ConnectionState ||
               Math.Abs(current.BatteryLevel - last.BatteryLevel) > batteryThreshold ||
               current.BatteryIsCharging != last.BatteryIsCharging;
    }

    private static string GetStatusText(int status)
    {
        return status switch
        {
            0 => "Unknown",
            1 => "Departure",
            2 => "Offline",
            3 => "Idle",
            4 => "Executing",
            5 => "Charging",
            6 => "Updating",
            7 => "Abnormal",
            _ => $"Unknown ({status})"
        };
    }
}
