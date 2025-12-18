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
    private readonly ISignalRConnectionTracker _connectionTracker;
    private readonly RobotRealtimePollingOptions _options;

    // Cache last known positions for change detection
    private readonly Dictionary<string, RobotPositionDto> _lastPositions = new();
    private readonly object _cacheLock = new();

    // Track polling state for logging
    private bool _wasPolling = false;

    public RobotRealtimePollingService(
        IServiceProvider serviceProvider,
        ILogger<RobotRealtimePollingService> logger,
        IRobotRealtimeNotificationService notificationService,
        ISignalRConnectionTracker connectionTracker,
        IOptions<RobotRealtimePollingOptions> options)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
        _notificationService = notificationService;
        _connectionTracker = connectionTracker;
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
                // Only poll if there are active SignalR connections
                if (_connectionTracker.HasActiveConnections)
                {
                    if (!_wasPolling)
                    {
                        _logger.LogInformation(
                            "Starting robot realtime polling - {Count} client(s) connected",
                            _connectionTracker.ConnectionCount);
                        _wasPolling = true;
                    }

                    await PollAndBroadcastAsync(stoppingToken);
                }
                else
                {
                    if (_wasPolling)
                    {
                        _logger.LogInformation("Pausing robot realtime polling - no clients connected");
                        _wasPolling = false;

                        // Clear cached positions when pausing so we get fresh data on resume
                        lock (_cacheLock)
                        {
                            _lastPositions.Clear();
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

    private async Task PollAndBroadcastAsync(CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var realtimeClient = scope.ServiceProvider.GetRequiredService<IRobotRealtimeClient>();

        // Fetch all robots (no map filter - we filter when broadcasting)
        var realtimeData = await realtimeClient.GetRealtimeInfoAsync(
            floorNumber: null,
            mapCode: null,
            isFirst: false,
            cancellationToken: cancellationToken);

        if (realtimeData?.RobotRealtimeList == null || realtimeData.RobotRealtimeList.Count == 0)
        {
            _logger.LogDebug("No robots returned from realtime API");
            return;
        }

        // Map to lightweight DTOs
        var positions = realtimeData.RobotRealtimeList
            .Select(MapToPositionDto)
            .ToList();

        // Optional: Only broadcast if positions changed (reduces network traffic)
        var changedPositions = _options.BroadcastOnlyChanges
            ? FilterChangedPositions(positions)
            : positions;

        if (changedPositions.Count > 0)
        {
            await _notificationService.BroadcastRobotPositionsAsync(changedPositions, cancellationToken);
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

    private List<RobotPositionDto> FilterChangedPositions(List<RobotPositionDto> positions)
    {
        var changed = new List<RobotPositionDto>();

        lock (_cacheLock)
        {
            foreach (var pos in positions)
            {
                if (!_lastPositions.TryGetValue(pos.RobotId, out var lastPos) ||
                    HasSignificantChange(lastPos, pos))
                {
                    changed.Add(pos);
                    _lastPositions[pos.RobotId] = pos;
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
