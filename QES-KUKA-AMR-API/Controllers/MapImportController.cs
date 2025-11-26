using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QES_KUKA_AMR_API.Data;
using QES_KUKA_AMR_API.Models.MapImport;
using QES_KUKA_AMR_API.Services.MapImport;

namespace QES_KUKA_AMR_API.Controllers;

/// <summary>
/// Controller for importing map data and QR code coordinates from JSON files
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class MapImportController : ControllerBase
{
    private readonly IMapImportService _mapImportService;
    private readonly ApplicationDbContext _dbContext;
    private readonly ILogger<MapImportController> _logger;

    public MapImportController(
        IMapImportService mapImportService,
        ApplicationDbContext dbContext,
        ILogger<MapImportController> logger)
    {
        _mapImportService = mapImportService;
        _dbContext = dbContext;
        _logger = logger;
    }

    /// <summary>
    /// Import QR codes and coordinates from a map JSON file
    /// </summary>
    /// <param name="request">Import request with file path and options</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Import result with statistics</returns>
    [HttpPost("import")]
    public async Task<ActionResult<MapImportResponse>> ImportFromFile(
        [FromBody] MapImportRequest request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.FilePath))
        {
            return BadRequest(new MapImportResponse
            {
                Success = false,
                Message = "File path is required",
                Errors = new List<string> { "FilePath cannot be empty" }
            });
        }

        _logger.LogInformation("Starting map import from file: {FilePath}", request.FilePath);

        var result = await _mapImportService.ImportFromJsonFileAsync(
            request.FilePath,
            request.OverwriteExisting,
            cancellationToken);

        if (result.Success)
        {
            return Ok(result);
        }
        else
        {
            return BadRequest(result);
        }
    }

    /// <summary>
    /// Upload and import map JSON file (with automatic overwrite of existing data)
    /// </summary>
    /// <param name="file">The map JSON file to upload</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Import result with statistics</returns>
    [HttpPost("upload")]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(50 * 1024 * 1024)] // 50MB max
    public async Task<ActionResult<MapImportResponse>> UploadMapFile(
        IFormFile file,
        CancellationToken cancellationToken)
    {
        if (file == null || file.Length == 0)
        {
            return BadRequest(new MapImportResponse
            {
                Success = false,
                Message = "No file uploaded",
                Errors = new List<string> { "Please select a JSON file to upload" }
            });
        }

        if (!file.FileName.EndsWith(".json", StringComparison.OrdinalIgnoreCase))
        {
            return BadRequest(new MapImportResponse
            {
                Success = false,
                Message = "Invalid file type",
                Errors = new List<string> { "Only JSON files are accepted" }
            });
        }

        _logger.LogInformation("Starting map upload import: {FileName}, Size={Size} bytes",
            file.FileName, file.Length);

        using var stream = file.OpenReadStream();
        var result = await _mapImportService.ImportFromUploadAsync(stream, file.FileName, cancellationToken);

        if (result.Success)
        {
            return Ok(result);
        }
        else
        {
            return BadRequest(result);
        }
    }

    /// <summary>
    /// Parse and preview map JSON file without importing to database
    /// </summary>
    /// <param name="filePath">Full path to the map JSON file</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Parsed map data structure</returns>
    [HttpGet("preview")]
    public async Task<ActionResult<MapDataDto>> PreviewMapFile(
        [FromQuery] string filePath,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(filePath))
        {
            return BadRequest("File path is required");
        }

        _logger.LogInformation("Previewing map file: {FilePath}", filePath);

        var mapData = await _mapImportService.ParseMapJsonFileAsync(filePath, cancellationToken);

        if (mapData == null)
        {
            return NotFound(new
            {
                error = "File not found or invalid JSON format",
                filePath
            });
        }

        return Ok(mapData);
    }

    /// <summary>
    /// Get summary statistics from a map JSON file without importing
    /// </summary>
    /// <param name="filePath">Full path to the map JSON file</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Map statistics summary</returns>
    [HttpGet("statistics")]
    public async Task<ActionResult<object>> GetMapStatistics(
        [FromQuery] string filePath,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(filePath))
        {
            return BadRequest("File path is required");
        }

        _logger.LogInformation("Getting statistics for map file: {FilePath}", filePath);

        var mapData = await _mapImportService.ParseMapJsonFileAsync(filePath, cancellationToken);

        if (mapData == null)
        {
            return NotFound(new
            {
                error = "File not found or invalid JSON format",
                filePath
            });
        }

        var stats = new
        {
            mapCode = mapData.MapCode,
            totalFloors = mapData.FloorList.Count,
            floors = mapData.FloorList.Select(floor => new
            {
                floorLevel = floor.FloorLevel,
                floorName = floor.FloorName,
                dimensions = new
                {
                    length = floor.FloorLength,
                    width = floor.FloorWidth
                },
                totalNodes = floor.NodeList.Count,
                totalEdges = floor.EdgeList?.Count ?? 0,
                nodeTypes = floor.NodeList
                    .GroupBy(n => n.NodeType)
                    .Select(g => new { nodeType = g.Key, count = g.Count() })
                    .ToList(),
                rackParkingNodes = floor.NodeList.Count(n => n.NodeLabel.Contains("RackPark")),
                nodesWithFunctions = floor.NodeList.Count(n => n.FunctionList != null && n.FunctionList.Any()),
                coordinateRange = new
                {
                    xMin = floor.NodeList.Min(n => n.XCoordinate),
                    xMax = floor.NodeList.Max(n => n.XCoordinate),
                    yMin = floor.NodeList.Min(n => n.YCoordinate),
                    yMax = floor.NodeList.Max(n => n.YCoordinate)
                }
            }).ToList()
        };

        return Ok(stats);
    }

    /// <summary>
    /// Get sync status showing which QR codes have coordinates vs. which don't
    /// </summary>
    /// <param name="mapCode">Optional filter by map code</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Sync status summary</returns>
    [HttpGet("sync-status")]
    public async Task<ActionResult<object>> GetSyncStatus(
        [FromQuery] string? mapCode = null,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Getting sync status for map: {MapCode}", mapCode ?? "All");

        // Query all QR codes, optionally filtered by map code
        var query = _dbContext.QrCodes.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(mapCode))
        {
            query = query.Where(q => q.MapCode == mapCode);
        }

        var allQrCodes = await query.ToListAsync(cancellationToken);

        // Categorize QR codes
        var withCoordinates = allQrCodes
            .Where(q => q.XCoordinate.HasValue && q.YCoordinate.HasValue)
            .ToList();

        var withoutCoordinates = allQrCodes
            .Where(q => !q.XCoordinate.HasValue || !q.YCoordinate.HasValue)
            .ToList();

        // Group by map code
        var byMapCode = allQrCodes
            .GroupBy(q => q.MapCode)
            .Select(g => new
            {
                mapCode = g.Key,
                total = g.Count(),
                withCoordinates = g.Count(q => q.XCoordinate.HasValue && q.YCoordinate.HasValue),
                withoutCoordinates = g.Count(q => !q.XCoordinate.HasValue || !q.YCoordinate.HasValue),
                coveragePercent = g.Count() > 0
                    ? Math.Round((double)g.Count(q => q.XCoordinate.HasValue && q.YCoordinate.HasValue) / g.Count() * 100, 2)
                    : 0
            })
            .OrderBy(x => x.mapCode)
            .ToList();

        // Find nodes with incomplete data (some fields but not all)
        var partialData = allQrCodes
            .Where(q =>
                (q.XCoordinate.HasValue && !q.YCoordinate.HasValue) ||
                (!q.XCoordinate.HasValue && q.YCoordinate.HasValue))
            .Select(q => new
            {
                nodeLabel = q.NodeLabel,
                mapCode = q.MapCode,
                hasX = q.XCoordinate.HasValue,
                hasY = q.YCoordinate.HasValue
            })
            .ToList();

        var status = new
        {
            summary = new
            {
                totalQrCodes = allQrCodes.Count,
                withCoordinates = withCoordinates.Count,
                withoutCoordinates = withoutCoordinates.Count,
                coordinateCoverage = allQrCodes.Count > 0
                    ? Math.Round((double)withCoordinates.Count / allQrCodes.Count * 100, 2)
                    : 0,
                partialDataCount = partialData.Count
            },
            byMapCode = byMapCode,
            qrCodesWithCoordinates = withCoordinates
                .Select(q => new
                {
                    nodeLabel = q.NodeLabel,
                    mapCode = q.MapCode,
                    coordinates = new
                    {
                        x = q.XCoordinate,
                        y = q.YCoordinate
                    },
                    nodeUuid = q.NodeUuid,
                    hasExternalId = q.ExternalQrCodeId.HasValue,
                    hasFunctions = !string.IsNullOrEmpty(q.FunctionListJson)
                })
                .OrderBy(q => q.mapCode)
                .ThenBy(q => q.nodeLabel)
                .ToList(),
            qrCodesMissingCoordinates = withoutCoordinates
                .Select(q => new
                {
                    nodeLabel = q.NodeLabel,
                    mapCode = q.MapCode,
                    floorNumber = q.FloorNumber,
                    hasExternalId = q.ExternalQrCodeId.HasValue,
                    reliability = q.Reliability,
                    lastUpdate = q.LastUpdateTime.ToString("yyyy-MM-dd HH:mm:ss")
                })
                .OrderBy(q => q.mapCode)
                .ThenBy(q => q.nodeLabel)
                .ToList(),
            warnings = new List<object>()
        };

        // Add warnings
        var warnings = (List<object>)status.warnings;

        if (partialData.Any())
        {
            warnings.Add(new
            {
                type = "PARTIAL_DATA",
                message = $"{partialData.Count} QR code(s) have incomplete coordinate data (X or Y missing)",
                affectedNodes = partialData
            });
        }

        if (withoutCoordinates.Any())
        {
            warnings.Add(new
            {
                type = "MISSING_COORDINATES",
                message = $"{withoutCoordinates.Count} QR code(s) are missing coordinate data",
                recommendation = "Import coordinates from map JSON file using /api/mapimport/import"
            });
        }

        if (allQrCodes.Count == 0)
        {
            warnings.Add(new
            {
                type = "NO_DATA",
                message = "No QR codes found in database",
                recommendation = "Sync from external API using /api/qrcodes/sync"
            });
        }

        return Ok(status);
    }
}
