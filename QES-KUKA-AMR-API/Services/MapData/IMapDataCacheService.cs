using QES_KUKA_AMR_API.Models.MapData;
using QES_KUKA_AMR_API.Models.MobileRobot;

namespace QES_KUKA_AMR_API.Services.MapData;

/// <summary>
/// Interface for caching realtime map data
/// </summary>
public interface IMapDataCacheService
{
    /// <summary>
    /// Update the cached realtime data from external API response
    /// </summary>
    /// <param name="data">Realtime data from external API</param>
    void UpdateRealtimeData(RealtimeInfoData data);

    /// <summary>
    /// Get cached realtime data, optionally filtered by floor/map
    /// </summary>
    /// <param name="floorNumber">Optional floor number filter</param>
    /// <param name="mapCode">Optional map code filter</param>
    /// <returns>Cached realtime data or null if no data cached</returns>
    MapRealtimeDataDto? GetCachedData(string? floorNumber = null, string? mapCode = null);

    /// <summary>
    /// Get the timestamp when data was last updated
    /// </summary>
    /// <returns>Last update time or null if never updated</returns>
    DateTime? GetLastUpdateTime();
}
