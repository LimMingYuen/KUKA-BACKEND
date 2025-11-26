using QES_KUKA_AMR_API.Models.MobileRobot;

namespace QES_KUKA_AMR_API.Services.RobotRealtime;

public interface IRobotRealtimeClient
{
    /// <summary>
    /// Gets realtime information for robots and containers filtered by map and floor.
    /// </summary>
    /// <param name="floorNumber">Floor number to filter by</param>
    /// <param name="mapCode">Map code to filter by</param>
    /// <param name="isFirst">Whether this is the first request</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Realtime info data or null if request fails</returns>
    Task<RealtimeInfoData?> GetRealtimeInfoAsync(
        string? floorNumber = null,
        string? mapCode = null,
        bool isFirst = false,
        CancellationToken cancellationToken = default);
}
