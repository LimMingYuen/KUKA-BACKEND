using Microsoft.AspNetCore.SignalR;
using QES_KUKA_AMR_API.Hubs;
using QES_KUKA_AMR_API.Models.RobotRealtime;

namespace QES_KUKA_AMR_API.Services.RobotRealtime;

/// <summary>
/// Interface for robot realtime notifications via SignalR
/// </summary>
public interface IRobotRealtimeNotificationService
{
    Task BroadcastRobotPositionsAsync(
        List<RobotPositionDto> robots,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Service to broadcast robot position updates via SignalR.
/// Registered as singleton to work with IHubContext.
/// </summary>
public class RobotRealtimeNotificationService : IRobotRealtimeNotificationService
{
    private readonly IHubContext<RobotRealtimeHub> _hubContext;
    private readonly ILogger<RobotRealtimeNotificationService> _logger;

    public RobotRealtimeNotificationService(
        IHubContext<RobotRealtimeHub> hubContext,
        ILogger<RobotRealtimeNotificationService> logger)
    {
        _hubContext = hubContext;
        _logger = logger;
    }

    /// <summary>
    /// Broadcast robot positions to all subscribed clients.
    /// Groups robots by map/floor and sends to appropriate SignalR groups.
    /// </summary>
    public async Task BroadcastRobotPositionsAsync(
        List<RobotPositionDto> robots,
        CancellationToken cancellationToken = default)
    {
        if (robots.Count == 0) return;

        try
        {
            var serverTimestamp = DateTime.UtcNow;

            // Group robots by map and floor
            var robotsByMapFloor = robots
                .GroupBy(r => RobotRealtimeHub.GetGroupName(r.MapCode, r.FloorNumber))
                .ToList();

            var broadcastTasks = new List<Task>();

            // Broadcast to specific map/floor groups
            foreach (var group in robotsByMapFloor)
            {
                var update = new RobotPositionUpdateDto
                {
                    Robots = group.ToList(),
                    ServerTimestamp = serverTimestamp,
                    MapCode = group.First().MapCode,
                    FloorNumber = group.First().FloorNumber
                };

                broadcastTasks.Add(
                    _hubContext.Clients.Group(group.Key)
                        .SendAsync("RobotPositionsUpdated", update, cancellationToken));
            }

            // Also broadcast all robots to "all" group
            var allUpdate = new RobotPositionUpdateDto
            {
                Robots = robots,
                ServerTimestamp = serverTimestamp
            };
            broadcastTasks.Add(
                _hubContext.Clients.Group("all")
                    .SendAsync("RobotPositionsUpdated", allUpdate, cancellationToken));

            await Task.WhenAll(broadcastTasks);

            _logger.LogDebug("Broadcast {Count} robot positions to {Groups} groups",
                robots.Count, robotsByMapFloor.Count + 1);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to broadcast robot positions");
        }
    }
}
