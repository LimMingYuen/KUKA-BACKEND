using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using QES_KUKA_AMR_API.Data;
using QES_KUKA_AMR_API.Data.Entities;
using QES_KUKA_AMR_API.Models.Config;
using QES_KUKA_AMR_API.Models.MapZone;
using QES_KUKA_AMR_API.Models.MobileRobot;
using QES_KUKA_AMR_API.Options;
using QES_KUKA_AMR_API.Services.Auth;
using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;

namespace QES_KUKA_AMR_API.Controllers;
[ApiController]
[Route("api/[controller]")]
public class MobileRobotController : ControllerBase
{
    private const int SyncPageNumber = -1;
    private const int SyncPageSize = 10_000;

    private readonly ApplicationDbContext _dbContext;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<MobileRobotController> _logger;
    private readonly MobileRobotServiceOptions _mobileRobotOptions;
    private readonly IExternalApiTokenService _externalApiTokenService;

    public MobileRobotController(
        ApplicationDbContext dbContext,
        IHttpClientFactory httpClientFactory,
        ILogger<MobileRobotController> logger,
        IOptions<MobileRobotServiceOptions> mobileRobotOptions,
        IExternalApiTokenService externalApiTokenService)
    {
        _dbContext = dbContext;
        _httpClientFactory = httpClientFactory;
        _logger = logger;
        _mobileRobotOptions = mobileRobotOptions.Value;
        _externalApiTokenService = externalApiTokenService;
    }

    [HttpPost("sync")]
    public async Task<ActionResult<MobileRobotSyncResultDto>> SyncAsync(
        [FromBody] QueryMobileRobotRequest? request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(_mobileRobotOptions.MobileRobotListUrl) ||
            !Uri.TryCreate(_mobileRobotOptions.MobileRobotListUrl, UriKind.Absolute, out var requestUri))
        {
            _logger.LogError("Mobile Robot list URL is not configured correctly.");
            return StatusCode(StatusCodes.Status500InternalServerError, new
            {
                Code = StatusCodes.Status500InternalServerError,
                Message = "Mobile Robot list URL is not configured."
            });
        }

        // Get token for external API authentication
        string token;
        try
        {
            token = await _externalApiTokenService.GetTokenAsync(cancellationToken);
            _logger.LogInformation("Successfully obtained external API token");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to obtain external API token");
            return StatusCode(StatusCodes.Status502BadGateway, new
            {
                Code = StatusCodes.Status502BadGateway,
                Message = "Failed to authenticate with external API."
            });
        }

        // Use request parameters or defaults
        var queryRequest = request ?? new QueryMobileRobotRequest();

        var httpClient = _httpClientFactory.CreateClient();

        var apiRequest = new HttpRequestMessage(HttpMethod.Post, requestUri)
        {
            Content = JsonContent.Create(new
            {
                query = queryRequest.Query,
                pageNum = queryRequest.PageNum,
                pageSize = queryRequest.PageSize,
                orderBy = queryRequest.OrderBy,
                asc = queryRequest.Asc
            })
        };
        apiRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        apiRequest.Content.Headers.ContentType = new MediaTypeHeaderValue("application/json")
        {
            CharSet = "UTF-8"
        };
        apiRequest.Headers.Add("language", "en");
        apiRequest.Headers.Add("accept", "*/*");
        apiRequest.Headers.Add("wizards", "FRONT_END");

        _logger.LogInformation("=== Mobile Robot Sync Request Debug ===");
        _logger.LogInformation("Target URI: {Uri}", requestUri);
        _logger.LogInformation("Token Length: {Length}", token.Length);
        _logger.LogInformation("=== End Request Debug ===");

        try
        {
            using var response = await httpClient.SendAsync(apiRequest, cancellationToken);
            var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);

            _logger.LogInformation("=== Mobile Robot Sync Response Debug ===");
            _logger.LogInformation("Response Status: {StatusCode}", response.StatusCode);
            _logger.LogWarning("Response Content: {Content}", responseContent);
            _logger.LogInformation("=== End Response Debug ===");

            if (string.IsNullOrWhiteSpace(responseContent) || responseContent.TrimStart().StartsWith('<'))
            {
                _logger.LogError("API returned HTML instead of JSON.");
                return StatusCode(StatusCodes.Status502BadGateway, new
                {
                    Code = (int)HttpStatusCode.BadGateway,
                    Message = "Backend returned HTML instead of JSON."
                });
            }

            var jsonOptions = new JsonSerializerOptions 
            { 
                PropertyNameCaseInsensitive = true,
                NumberHandling = System.Text.Json.Serialization.JsonNumberHandling.AllowReadingFromString
            };

            // Try parsing as real backend format first
            if (responseContent.Contains("\"success\""))
            {
                // Real backend has nested structure: data.pageData.content
                var realBackendResponse = JsonSerializer.Deserialize<RealBackendApiResponse<MobileRobotPageWrapper>>(
                    responseContent, jsonOptions);

                if (realBackendResponse is null || !realBackendResponse.Success)
                {
                    var message = realBackendResponse?.Message ?? "Failed to sync mobile robot";
                    return StatusCode((int)response.StatusCode, new { Code = "ERROR", Message = message });
                }

                var mobileRobots = realBackendResponse.Data?.PageData?.Content;
                if (mobileRobots is null || mobileRobots.Count == 0)
                {
                    return Ok(new MobileRobotSyncResultDto { Total = 0, Inserted = 0, Updated = 0 });
                }

                return await ProcessMobileRobotAsync(mobileRobots, cancellationToken);
            }
            else
            {
                // Parse as simulator format - direct structure: data.content
                var simulatorResponse = JsonSerializer.Deserialize<SimulatorApiResponse<MobileRobotPage>>(
                    responseContent, jsonOptions);

                if (simulatorResponse is null || simulatorResponse.Succ is not true)
                {
                    var message = simulatorResponse?.Msg ?? "Failed to sync mobile robots from simulator";
                    return StatusCode((int)response.StatusCode, new { Code = (int)response.StatusCode, Message = message });
                }

                var mobileRobots = simulatorResponse.Data?.Content;
                if (mobileRobots is null || mobileRobots.Count == 0)
                {
                    return Ok(new MobileRobotSyncResultDto { Total = 0, Inserted = 0, Updated = 0 });
                }

                return await ProcessMobileRobotAsync(mobileRobots, cancellationToken);
            }

        }
        catch(HttpRequestException httpRequestException)
        {
            _logger.LogError(httpRequestException, "Error calling API simulator");
            return StatusCode(StatusCodes.Status502BadGateway, new
            {
                Code = (int)HttpStatusCode.BadGateway,
                Message = "Unable to reach the API simulator."
            });
        }
    }

    private async Task<ActionResult<MobileRobotSyncResultDto>> ProcessMobileRobotAsync(IEnumerable<MobileRobotDto> mobileRobots, CancellationToken cancellationToken)
    {
        var mobileRobotList = mobileRobots.ToList();

        //load all existing mobile robot from db
        var allExistingMobileRobots = await _dbContext.MobileRobots.ToListAsync(cancellationToken);
        var existingMobileRobots = allExistingMobileRobots.ToDictionary(m => m.RobotId);

        var inserted = 0;
        var updated = 0;

        foreach (var robot in mobileRobotList)
        {
            if (string.IsNullOrWhiteSpace(robot.RobotId))
            {
                _logger.LogWarning("Skipped mobile robot with missing robot Id.");
                continue;
            }

            if (!existingMobileRobots.TryGetValue(robot.RobotId, out var entity))
            {
                entity = new MobileRobot{ RobotId = robot.RobotId };
                _dbContext.MobileRobots.Add(entity);
                existingMobileRobots[robot.RobotId] = entity;
                inserted++;
            }
            else
            {
                updated++;
            }

            entity.CreateTime = ParseDateTime(robot.CreateTime) ?? (entity.CreateTime == default ? DateTime.UtcNow : entity.CreateTime);
            entity.CreateBy = robot.CreateBy ?? string.Empty;
            entity.CreateApp = robot.CreateApp ?? string.Empty;
            entity.LastUpdateTime = ParseDateTime(robot.LastUpdateTime) ?? DateTime.UtcNow;
            entity.LastUpdateBy = robot.LastUpdateBy ?? string.Empty;
            entity.LastUpdateApp = robot.LastUpdateApp ?? string.Empty;
            entity.RobotId = robot.RobotId?? string.Empty;
            entity.RobotTypeCode = robot.RobotTypeCode ?? string.Empty;
            entity.BuildingCode = robot.BuildingCode ?? string.Empty;
            entity.MapCode = robot.MapCode ?? string.Empty;
            entity.FloorNumber = robot.FloorNumber ?? string.Empty;
            entity.LastNodeNumber = robot.LastNodeNumber;
            entity.LastNodeDeleteFlag = robot.LastNodeDeleteFlag;
            entity.ContainerCode = robot.ContainerCode ?? string.Empty;
            entity.ActuatorType = robot.ActuatorType;
            entity.ActuatorStatusInfo = robot.ActuatorStatusInfo ?? string.Empty;
            entity.IpAddress = robot.IpAddress ?? string.Empty;
            entity.WarningInfo = robot.WarningInfo ?? string.Empty;
            entity.ConfigVersion = robot.ConfigVersion ?? string.Empty;
            entity.SendConfigVersion = robot.SendConfigVersion ?? string.Empty;
            entity.SendConfigTime = ParseDateTime(robot.SendConfigTime) ?? (entity.SendConfigTime == default ? DateTime.UtcNow : entity.SendConfigTime);
            entity.FirmwareVersion = robot.FirmwareVersion ?? string.Empty;
            entity.SendFirmwareVersion = robot.SendFirmwareVersion ?? string.Empty;
            entity.SendFirmwareTime = ParseDateTime(robot.SendFirmwareTime) ?? (entity.SendFirmwareTime == default ? DateTime.UtcNow : entity.SendFirmwareTime);
            entity.Status = robot.Status;
            entity.OccupyStatus = robot.OccupyStatus;
            entity.BatteryLevel = robot.BatteryLevel ?? 0;
            entity.Mileage = robot.Mileage ?? 0;
            entity.MissionCode = robot.MissionCode ?? string.Empty;
            entity.MeetObstacleStatus = robot.MeetObstacleStatus;
            entity.RobotOrientation = robot.RobotOrientation;
            entity.Reliability = robot.Reliability ?? 0;
            entity.RunTime = robot.RunTime;
            entity.RobotTypeClass = robot.RobotTypeClass;
            entity.TrailerNum = robot.TrailerNum ?? string.Empty;
            entity.TractionStatus = robot.TractionStatus ?? string.Empty;
            entity.XCoordinate = robot.XCoordinate ?? 0.00;
            entity.YCoordinate = robot.YCoordinate ?? 0.00;
        }

        await _dbContext.SaveChangesAsync(cancellationToken);

        return Ok(new MobileRobotSyncResultDto
        {
            Total = mobileRobotList.Count,
            Inserted = inserted,
            Updated = updated
        });
    }


    [HttpGet]
    public async Task<ActionResult<IEnumerable<MobileRobotSummaryDto>>> GetAsync(CancellationToken cancellationToken)
    {
        var mobileRobots = await _dbContext.MobileRobots.AsNoTracking().OrderBy(m => m.FloorNumber).Select(m => new MobileRobotSummaryDto
        {
            RobotId = m.RobotId,
            RobotTypeCode = m.RobotTypeCode,
            MapCode = m.MapCode,
            FloorNumber = m.FloorNumber,
            Reliability = m.Reliability,
            Status = m.Status,
            OccupyStatus = m.OccupyStatus,
            LastNodeNumber = m.LastNodeNumber,
            BatteryLevel = m.BatteryLevel,
            XCoordinate = m.XCoordinate,
            YCoordinate = m.YCoordinate,
            RobotOrientation = m.RobotOrientation
        })
            .ToListAsync(cancellationToken);

        _logger.LogInformation("Retrieved {Count} Mobile Robots", mobileRobots.Count);
        return Ok(mobileRobots);
    }

    [HttpGet("types")]
    public async Task<ActionResult<IEnumerable<string>>> GetRobotTypesAsync(CancellationToken cancellationToken)
    {
        var robotTypes = await _dbContext.MobileRobots
            .AsNoTracking()
            .Where(m => !string.IsNullOrEmpty(m.RobotTypeCode))
            .Select(m => m.RobotTypeCode)
            .Distinct()
            .OrderBy(t => t)
            .ToListAsync(cancellationToken);

        _logger.LogInformation("Retrieved {Count} distinct Robot Types", robotTypes.Count);
        return Ok(robotTypes);
    }

    [HttpGet("by-type/{typeCode}")]
    public async Task<ActionResult<IEnumerable<MobileRobotSummaryDto>>> GetByTypeAsync(string typeCode, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(typeCode))
        {
            return BadRequest(new { Message = "RobotTypeCode is required" });
        }

        var robots = await _dbContext.MobileRobots
            .AsNoTracking()
            .Where(m => m.RobotTypeCode == typeCode)
            .OrderBy(m => m.RobotId)
            .Select(m => new MobileRobotSummaryDto
            {
                RobotId = m.RobotId,
                RobotTypeCode = m.RobotTypeCode,
                MapCode = m.MapCode,
                FloorNumber = m.FloorNumber,
                Reliability = m.Reliability,
                Status = m.Status,
                OccupyStatus = m.OccupyStatus,
                LastNodeNumber = m.LastNodeNumber,
                BatteryLevel = m.BatteryLevel,
                XCoordinate = m.XCoordinate,
                YCoordinate = m.YCoordinate,
                RobotOrientation = m.RobotOrientation
            })
            .ToListAsync(cancellationToken);

        _logger.LogInformation("Retrieved {Count} robots with type {TypeCode}", robots.Count, typeCode);
        return Ok(robots);
    }

    private static DateTime? ParseDateTime(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        if (DateTime.TryParse(value, out var parsed))
        {
            return parsed;
        }

        return null;
    }

}
