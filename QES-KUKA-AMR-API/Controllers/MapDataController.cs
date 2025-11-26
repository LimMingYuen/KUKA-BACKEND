using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QES_KUKA_AMR_API.Data;
using QES_KUKA_AMR_API.Models.MapData;
using QES_KUKA_AMR_API.Services.MapData;

namespace QES_KUKA_AMR_API.Controllers;

/// <summary>
/// Controller for map visualization data
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class MapDataController : ControllerBase
{
    private readonly ApplicationDbContext _dbContext;
    private readonly IMapDataCacheService _cacheService;
    private readonly ILogger<MapDataController> _logger;

    public MapDataController(
        ApplicationDbContext dbContext,
        IMapDataCacheService cacheService,
        ILogger<MapDataController> logger)
    {
        _dbContext = dbContext;
        _cacheService = cacheService;
        _logger = logger;
    }

    /// <summary>
    /// Get available floors/maps for tab navigation
    /// </summary>
    [HttpGet("floors")]
    public async Task<ActionResult<IEnumerable<FloorInfoDto>>> GetFloorsAsync(CancellationToken cancellationToken)
    {
        var floors = await _dbContext.QrCodes
            .AsNoTracking()
            .Where(q => q.XCoordinate.HasValue && q.YCoordinate.HasValue && !string.IsNullOrEmpty(q.NodeUuid))
            .GroupBy(q => new { q.FloorNumber, q.MapCode })
            .Select(g => new FloorInfoDto
            {
                FloorNumber = g.Key.FloorNumber,
                MapCode = g.Key.MapCode,
                DisplayName = $"Floor {g.Key.FloorNumber} - {g.Key.MapCode}",
                NodeCount = g.Count(),
                ZoneCount = 0 // Will be populated separately
            })
            .OrderBy(f => f.FloorNumber)
            .ThenBy(f => f.MapCode)
            .ToListAsync(cancellationToken);

        // Get zone counts
        var zoneCounts = await _dbContext.MapZones
            .AsNoTracking()
            .Where(z => z.Status == 1) // Only active zones
            .GroupBy(z => new { z.FloorNumber, z.MapCode })
            .Select(g => new { g.Key.FloorNumber, g.Key.MapCode, Count = g.Count() })
            .ToListAsync(cancellationToken);

        foreach (var floor in floors)
        {
            var zoneCount = zoneCounts.FirstOrDefault(z =>
                z.FloorNumber == floor.FloorNumber && z.MapCode == floor.MapCode);
            floor.ZoneCount = zoneCount?.Count ?? 0;
        }

        _logger.LogInformation("Retrieved {Count} floors", floors.Count);
        return Ok(floors);
    }

    /// <summary>
    /// Get QR code nodes for a specific floor/map
    /// </summary>
    [HttpGet("nodes")]
    public async Task<ActionResult<IEnumerable<MapNodeDto>>> GetNodesAsync(
        [FromQuery] string? floorNumber,
        [FromQuery] string? mapCode,
        CancellationToken cancellationToken)
    {
        var query = _dbContext.QrCodes.AsNoTracking();

        // Filter by floor/map if provided
        if (!string.IsNullOrEmpty(floorNumber))
        {
            query = query.Where(q => q.FloorNumber == floorNumber);
        }

        if (!string.IsNullOrEmpty(mapCode))
        {
            query = query.Where(q => q.MapCode == mapCode);
        }

        // Only get nodes with coordinates AND valid NodeUuid
        query = query.Where(q => q.XCoordinate.HasValue && q.YCoordinate.HasValue && !string.IsNullOrEmpty(q.NodeUuid));

        var nodes = await query
            .Select(q => new MapNodeDto
            {
                Id = q.Id,
                NodeLabel = q.NodeLabel,
                XCoordinate = q.XCoordinate ?? 0,
                YCoordinate = q.YCoordinate ?? 0,
                NodeNumber = q.NodeNumber,
                NodeType = q.NodeType,
                MapCode = q.MapCode,
                FloorNumber = q.FloorNumber,
                NodeUuid = q.NodeUuid,
                TransitOrientations = q.TransitOrientations
            })
            .OrderBy(n => n.NodeNumber)
            .ToListAsync(cancellationToken);

        _logger.LogInformation(
            "Retrieved {Count} nodes for floor={Floor}, map={Map}",
            nodes.Count, floorNumber ?? "all", mapCode ?? "all");

        return Ok(nodes);
    }

    /// <summary>
    /// Get map zones for a specific floor/map
    /// </summary>
    [HttpGet("zones")]
    public async Task<ActionResult<IEnumerable<MapZoneDisplayDto>>> GetZonesAsync(
        [FromQuery] string? floorNumber,
        [FromQuery] string? mapCode,
        CancellationToken cancellationToken)
    {
        var query = _dbContext.MapZones
            .AsNoTracking()
            .Where(z => z.Status == 1); // Only active zones

        if (!string.IsNullOrEmpty(floorNumber))
        {
            query = query.Where(z => z.FloorNumber == floorNumber);
        }

        if (!string.IsNullOrEmpty(mapCode))
        {
            query = query.Where(z => z.MapCode == mapCode);
        }

        var zones = await query
            .Select(z => new
            {
                z.Id,
                z.ZoneName,
                z.ZoneCode,
                z.ZoneColor,
                z.ZoneType,
                z.MapCode,
                z.FloorNumber,
                z.Points,
                z.Nodes,
                z.Status
            })
            .ToListAsync(cancellationToken);

        var result = zones.Select(z => new MapZoneDisplayDto
        {
            Id = z.Id,
            ZoneName = z.ZoneName,
            ZoneCode = z.ZoneCode,
            ZoneColor = string.IsNullOrEmpty(z.ZoneColor) ? "#E8DEF8" : z.ZoneColor, // Default purple
            ZoneType = z.ZoneType,
            MapCode = z.MapCode,
            FloorNumber = z.FloorNumber,
            PolygonPoints = ParsePolygonPoints(z.Points),
            NodeIds = ParseNodeIds(z.Nodes),
            Status = z.Status
        }).ToList();

        _logger.LogInformation(
            "Retrieved {Count} zones for floor={Floor}, map={Map}",
            result.Count, floorNumber ?? "all", mapCode ?? "all");

        return Ok(result);
    }

    /// <summary>
    /// Get map edges (connections between nodes) for a specific floor/map
    /// </summary>
    [HttpGet("edges")]
    public async Task<ActionResult<IEnumerable<MapEdgeDto>>> GetEdgesAsync(
        [FromQuery] string? floorNumber,
        [FromQuery] string? mapCode,
        CancellationToken cancellationToken)
    {
        var query = _dbContext.MapEdges.AsNoTracking();

        if (!string.IsNullOrEmpty(floorNumber))
        {
            query = query.Where(e => e.FloorNumber == floorNumber);
        }

        if (!string.IsNullOrEmpty(mapCode))
        {
            query = query.Where(e => e.MapCode == mapCode);
        }

        var edges = await query
            .Where(e => e.Status == 1) // Only enabled edges
            .Select(e => new MapEdgeDto
            {
                Id = e.Id,
                BeginNodeLabel = e.BeginNodeLabel,
                EndNodeLabel = e.EndNodeLabel,
                EdgeLength = e.EdgeLength,
                EdgeType = e.EdgeType,
                MapCode = e.MapCode,
                FloorNumber = e.FloorNumber
            })
            .ToListAsync(cancellationToken);

        _logger.LogInformation(
            "Retrieved {Count} edges for floor={Floor}, map={Map}",
            edges.Count, floorNumber ?? "all", mapCode ?? "all");

        return Ok(edges);
    }

    /// <summary>
    /// Get cached realtime data (robots, containers)
    /// </summary>
    [HttpGet("realtime")]
    public ActionResult<MapRealtimeDataDto> GetRealtimeData(
        [FromQuery] string? floorNumber,
        [FromQuery] string? mapCode)
    {
        var data = _cacheService.GetCachedData(floorNumber, mapCode);

        if (data == null)
        {
            return Ok(new MapRealtimeDataDto
            {
                Robots = new List<RobotPositionDto>(),
                Containers = new List<ContainerPositionDto>(),
                ErrorRobots = new List<RobotPositionDto>(),
                LastUpdated = DateTime.UtcNow
            });
        }

        _logger.LogDebug(
            "Retrieved realtime data: {RobotCount} robots, {ContainerCount} containers",
            data.Robots.Count, data.Containers.Count);

        return Ok(data);
    }

    /// <summary>
    /// Get cache status (last update time)
    /// </summary>
    [HttpGet("cache-status")]
    public ActionResult<object> GetCacheStatus()
    {
        var lastUpdate = _cacheService.GetLastUpdateTime();

        return Ok(new
        {
            LastUpdated = lastUpdate,
            HasData = lastUpdate.HasValue,
            AgeSeconds = lastUpdate.HasValue
                ? (DateTime.UtcNow - lastUpdate.Value).TotalSeconds
                : (double?)null
        });
    }

    /// <summary>
    /// Parse polygon points from JSON string
    /// </summary>
    private static List<PolygonPoint> ParsePolygonPoints(string? pointsJson)
    {
        if (string.IsNullOrEmpty(pointsJson))
        {
            return new List<PolygonPoint>();
        }

        try
        {
            var points = JsonSerializer.Deserialize<List<PolygonPoint>>(
                pointsJson,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            return points ?? new List<PolygonPoint>();
        }
        catch
        {
            return new List<PolygonPoint>();
        }
    }

    /// <summary>
    /// Parse node IDs from JSON string
    /// </summary>
    private static List<string> ParseNodeIds(string? nodesJson)
    {
        if (string.IsNullOrEmpty(nodesJson))
        {
            return new List<string>();
        }

        try
        {
            var nodes = JsonSerializer.Deserialize<List<string>>(
                nodesJson,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            return nodes ?? new List<string>();
        }
        catch
        {
            // Try parsing as array of objects with id/uuid properties
            try
            {
                var nodeObjects = JsonSerializer.Deserialize<List<JsonElement>>(nodesJson);
                if (nodeObjects != null)
                {
                    return nodeObjects
                        .Select(n =>
                        {
                            if (n.TryGetProperty("id", out var id))
                                return id.ToString();
                            if (n.TryGetProperty("uuid", out var uuid))
                                return uuid.ToString();
                            if (n.TryGetProperty("nodeUuid", out var nodeUuid))
                                return nodeUuid.ToString();
                            return n.ToString();
                        })
                        .ToList();
                }
            }
            catch
            {
                // Ignore parsing errors
            }

            return new List<string>();
        }
    }
}
