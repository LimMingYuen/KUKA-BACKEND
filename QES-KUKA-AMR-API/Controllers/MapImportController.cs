using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
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
    private readonly ILogger<MapImportController> _logger;

    public MapImportController(
        IMapImportService mapImportService,
        ILogger<MapImportController> logger)
    {
        _mapImportService = mapImportService;
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
}
