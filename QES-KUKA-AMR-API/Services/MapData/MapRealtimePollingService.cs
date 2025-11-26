using QES_KUKA_AMR_API.Hubs;
using QES_KUKA_AMR_API.Services.RobotRealtime;

namespace QES_KUKA_AMR_API.Services.MapData;

/// <summary>
/// Background service that polls external AMR API for realtime data
/// and broadcasts updates via SignalR
/// </summary>
public class MapRealtimePollingService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IMapDataCacheService _cacheService;
    private readonly IMapNotificationService _notificationService;
    private readonly ILogger<MapRealtimePollingService> _logger;
    private readonly TimeSpan _pollingInterval = TimeSpan.FromSeconds(30);

    public MapRealtimePollingService(
        IServiceProvider serviceProvider,
        IMapDataCacheService cacheService,
        IMapNotificationService notificationService,
        ILogger<MapRealtimePollingService> logger)
    {
        _serviceProvider = serviceProvider;
        _cacheService = cacheService;
        _notificationService = notificationService;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation(
            "Map Realtime Polling Service started. Polling interval: {Interval} seconds",
            _pollingInterval.TotalSeconds);

        // Initial delay to let the application start up
        await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await PollAndBroadcastAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                // Normal shutdown, don't log as error
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during realtime data polling cycle");
            }

            try
            {
                await Task.Delay(_pollingInterval, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
        }

        _logger.LogInformation("Map Realtime Polling Service stopped");
    }

    private async Task PollAndBroadcastAsync(CancellationToken cancellationToken)
    {
        _logger.LogDebug("Polling external API for realtime data...");

        // Create a scope to get scoped services
        using var scope = _serviceProvider.CreateScope();
        var robotRealtimeClient = scope.ServiceProvider.GetRequiredService<IRobotRealtimeClient>();

        // Fetch realtime data from external API
        var realtimeData = await robotRealtimeClient.GetRealtimeInfoAsync(
            floorNumber: null,  // Get all floors
            mapCode: null,      // Get all maps
            isFirst: false,
            cancellationToken: cancellationToken);

        if (realtimeData == null)
        {
            _logger.LogWarning("Failed to fetch realtime data from external API");
            return;
        }

        _logger.LogDebug(
            "Received realtime data: {RobotCount} robots, {ContainerCount} containers, {ErrorCount} errors",
            realtimeData.RobotRealtimeList.Count,
            realtimeData.ContainerRealtimeList.Count,
            realtimeData.ErrorRobotList.Count);

        // Update cache
        _cacheService.UpdateRealtimeData(realtimeData);

        // Get cached data and broadcast to all clients
        var cachedData = _cacheService.GetCachedData();
        if (cachedData != null)
        {
            await _notificationService.NotifyRobotPositionsUpdatedAsync(cachedData, cancellationToken);
        }
    }
}
