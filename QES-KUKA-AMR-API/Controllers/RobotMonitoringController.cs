using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using QES_KUKA_AMR_API.Models.RobotMonitoring;
using QES_KUKA_AMR_API.Services.RobotMonitoring;
using QES_KUKA_AMR_API.Services.RobotRealtime;
using System.Security.Claims;

namespace QES_KUKA_AMR_API.Controllers;

[ApiController]
[Authorize]
[Route("api/[controller]")]
public class RobotMonitoringController : ControllerBase
{
    private readonly IRobotMonitoringService _robotMonitoringService;
    private readonly IRobotRealtimeClient _robotRealtimeClient;
    private readonly ILogger<RobotMonitoringController> _logger;

    public RobotMonitoringController(
        IRobotMonitoringService robotMonitoringService,
        IRobotRealtimeClient robotRealtimeClient,
        ILogger<RobotMonitoringController> logger)
    {
        _robotMonitoringService = robotMonitoringService;
        _robotRealtimeClient = robotRealtimeClient;
        _logger = logger;
    }

    /// <summary>
    /// Get all robot monitoring map configurations
    /// </summary>
    [HttpGet("maps")]
    public async Task<ActionResult<IEnumerable<RobotMonitoringMapSummaryDto>>> GetMaps(CancellationToken cancellationToken)
    {
        var maps = await _robotMonitoringService.GetAllMapsAsync(cancellationToken);
        return Ok(maps);
    }

    /// <summary>
    /// Get a specific map configuration by ID
    /// </summary>
    [HttpGet("maps/{id}")]
    public async Task<ActionResult<RobotMonitoringMapDto>> GetMapById(int id, CancellationToken cancellationToken)
    {
        var map = await _robotMonitoringService.GetMapByIdAsync(id, cancellationToken);
        if (map == null)
        {
            return NotFound(new { Message = $"Map configuration with ID {id} not found" });
        }
        return Ok(map);
    }

    /// <summary>
    /// Get the default map configuration
    /// </summary>
    [HttpGet("maps/default")]
    public async Task<ActionResult<RobotMonitoringMapDto>> GetDefaultMap(CancellationToken cancellationToken)
    {
        var map = await _robotMonitoringService.GetDefaultMapAsync(cancellationToken);
        if (map == null)
        {
            return NotFound(new { Message = "No default map configuration found" });
        }
        return Ok(map);
    }

    /// <summary>
    /// Create a new map configuration
    /// </summary>
    [HttpPost("maps")]
    public async Task<ActionResult<RobotMonitoringMapDto>> CreateMap(
        [FromBody] CreateRobotMonitoringMapRequest request,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var username = GetUsername();
        var map = await _robotMonitoringService.CreateMapAsync(request, username, cancellationToken);
        return CreatedAtAction(nameof(GetMapById), new { id = map.Id }, map);
    }

    /// <summary>
    /// Update an existing map configuration
    /// </summary>
    [HttpPut("maps/{id}")]
    public async Task<ActionResult<RobotMonitoringMapDto>> UpdateMap(
        int id,
        [FromBody] UpdateRobotMonitoringMapRequest request,
        CancellationToken cancellationToken)
    {
        var username = GetUsername();
        var map = await _robotMonitoringService.UpdateMapAsync(id, request, username, cancellationToken);
        if (map == null)
        {
            return NotFound(new { Message = $"Map configuration with ID {id} not found" });
        }
        return Ok(map);
    }

    /// <summary>
    /// Delete a map configuration
    /// </summary>
    [HttpDelete("maps/{id}")]
    public async Task<IActionResult> DeleteMap(int id, CancellationToken cancellationToken)
    {
        var deleted = await _robotMonitoringService.DeleteMapAsync(id, cancellationToken);
        if (!deleted)
        {
            return NotFound(new { Message = $"Map configuration with ID {id} not found" });
        }
        return NoContent();
    }

    /// <summary>
    /// Upload a background image for a map configuration
    /// </summary>
    [HttpPost("maps/{id}/background")]
    public async Task<ActionResult<ImageUploadResponse>> UploadBackgroundImage(
        int id,
        IFormFile file,
        CancellationToken cancellationToken)
    {
        if (file == null || file.Length == 0)
        {
            return BadRequest(new ImageUploadResponse { Success = false, Message = "No file provided" });
        }

        var result = await _robotMonitoringService.UploadBackgroundImageAsync(id, file, cancellationToken);
        if (!result.Success)
        {
            return BadRequest(result);
        }
        return Ok(result);
    }

    /// <summary>
    /// Delete the background image for a map configuration
    /// </summary>
    [HttpDelete("maps/{id}/background")]
    public async Task<IActionResult> DeleteBackgroundImage(int id, CancellationToken cancellationToken)
    {
        var deleted = await _robotMonitoringService.DeleteBackgroundImageAsync(id, cancellationToken);
        if (!deleted)
        {
            return NotFound(new { Message = $"Map configuration with ID {id} not found or has no background image" });
        }
        return NoContent();
    }

    /// <summary>
    /// Get aggregated map data (nodes and zones) for rendering
    /// </summary>
    [HttpGet("map-data")]
    public async Task<ActionResult<MapDataDto>> GetMapData(
        [FromQuery] string? mapCode,
        [FromQuery] string? floorNumber,
        CancellationToken cancellationToken)
    {
        var data = await _robotMonitoringService.GetMapDataAsync(mapCode, floorNumber, cancellationToken);
        return Ok(data);
    }

    /// <summary>
    /// Get real-time robot positions
    /// </summary>
    [HttpGet("robots")]
    public async Task<ActionResult<RobotPositionsResponse>> GetRobotPositions(
        [FromQuery] string? mapCode,
        [FromQuery] string? floorNumber,
        [FromQuery] bool isFirst = false,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var realtimeData = await _robotRealtimeClient.GetRealtimeInfoAsync(floorNumber, mapCode, isFirst, cancellationToken);

            if (realtimeData == null)
            {
                return Ok(new RobotPositionsResponse
                {
                    Robots = new List<RobotPositionDto>(),
                    Timestamp = DateTime.UtcNow
                });
            }

            var robots = realtimeData.RobotRealtimeList?.Select(r => new RobotPositionDto
            {
                RobotId = r.RobotId ?? string.Empty,
                RobotTypeCode = r.RobotTypeCode,
                X = r.XCoordinate,
                Y = r.YCoordinate,
                Orientation = r.RobotOrientation,
                Status = r.RobotStatus,
                StatusText = GetStatusText(r.RobotStatus),
                OccupyStatus = 0, // Not available in RobotRealtimeDto
                BatteryLevel = r.BatteryLevel,
                MissionCode = r.MissionCode,
                LastNodeNumber = r.LastNodeNumber,
                WarningInfo = r.WarningCode,
                MapCode = r.MapCode ?? string.Empty,
                FloorNumber = r.FloorNumber ?? string.Empty
            }).ToList() ?? new List<RobotPositionDto>();

            return Ok(new RobotPositionsResponse
            {
                Robots = robots,
                Timestamp = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get real-time robot positions");
            return StatusCode(StatusCodes.Status502BadGateway, new
            {
                Message = "Failed to retrieve robot positions from external system"
            });
        }
    }

    /// <summary>
    /// Get available map codes
    /// </summary>
    [HttpGet("map-codes")]
    public async Task<ActionResult<IEnumerable<string>>> GetMapCodes(CancellationToken cancellationToken)
    {
        var mapCodes = await _robotMonitoringService.GetAvailableMapCodesAsync(cancellationToken);
        return Ok(mapCodes);
    }

    /// <summary>
    /// Get available floor numbers for a map code
    /// </summary>
    [HttpGet("floor-numbers")]
    public async Task<ActionResult<IEnumerable<string>>> GetFloorNumbers(
        [FromQuery] string? mapCode,
        CancellationToken cancellationToken)
    {
        var floorNumbers = await _robotMonitoringService.GetAvailableFloorNumbersAsync(mapCode, cancellationToken);
        return Ok(floorNumbers);
    }

    private string GetUsername()
    {
        return User.FindFirst(ClaimTypes.Name)?.Value
            ?? User.FindFirst("name")?.Value
            ?? User.FindFirst("sub")?.Value
            ?? "system";
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
            _ => "Unknown"
        };
    }
}
