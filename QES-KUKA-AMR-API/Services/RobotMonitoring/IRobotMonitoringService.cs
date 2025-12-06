using Microsoft.AspNetCore.Http;
using QES_KUKA_AMR_API.Models.RobotMonitoring;

namespace QES_KUKA_AMR_API.Services.RobotMonitoring;

/// <summary>
/// Service interface for robot monitoring map operations
/// </summary>
public interface IRobotMonitoringService
{
    /// <summary>
    /// Get all robot monitoring map configurations
    /// </summary>
    Task<IEnumerable<RobotMonitoringMapSummaryDto>> GetAllMapsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Get a specific map configuration by ID
    /// </summary>
    Task<RobotMonitoringMapDto?> GetMapByIdAsync(int id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get the default map configuration
    /// </summary>
    Task<RobotMonitoringMapDto?> GetDefaultMapAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Create a new map configuration
    /// </summary>
    Task<RobotMonitoringMapDto> CreateMapAsync(CreateRobotMonitoringMapRequest request, string username, CancellationToken cancellationToken = default);

    /// <summary>
    /// Update an existing map configuration
    /// </summary>
    Task<RobotMonitoringMapDto?> UpdateMapAsync(int id, UpdateRobotMonitoringMapRequest request, string username, CancellationToken cancellationToken = default);

    /// <summary>
    /// Delete a map configuration and its background image
    /// </summary>
    Task<bool> DeleteMapAsync(int id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Upload a background image for a map configuration
    /// </summary>
    Task<ImageUploadResponse> UploadBackgroundImageAsync(int mapId, IFormFile file, CancellationToken cancellationToken = default);

    /// <summary>
    /// Delete the background image for a map configuration
    /// </summary>
    Task<bool> DeleteBackgroundImageAsync(int mapId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get aggregated map data (nodes and zones) for rendering
    /// </summary>
    Task<MapDataDto> GetMapDataAsync(string? mapCode, string? floorNumber, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get available map codes from QrCodes table
    /// </summary>
    Task<IEnumerable<string>> GetAvailableMapCodesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Get available floor numbers for a specific map code
    /// </summary>
    Task<IEnumerable<string>> GetAvailableFloorNumbersAsync(string? mapCode, CancellationToken cancellationToken = default);
}
