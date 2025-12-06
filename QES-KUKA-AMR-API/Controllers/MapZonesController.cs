using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using QES_KUKA_AMR_API.Data;
using QES_KUKA_AMR_API.Data.Entities;
using QES_KUKA_AMR_API.Models.Config;
using QES_KUKA_AMR_API.Models.MapZone;
using QES_KUKA_AMR_API.Options;
using QES_KUKA_AMR_API.Services.Auth;

namespace QES_KUKA_AMR_API.Controllers;

[ApiController]
[Authorize]
[Route("api/[controller]")]
public class MapZonesController : ControllerBase
{
    private const int SyncPageNumber = 1;
    private const int SyncPageSize = 10_000;

    private readonly ApplicationDbContext _dbContext;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<MapZonesController> _logger;
    private readonly MapZoneServiceOptions _mapZoneOptions;
    private readonly IExternalApiTokenService _externalApiTokenService;

    public MapZonesController(
        ApplicationDbContext dbContext,
        IHttpClientFactory httpClientFactory,
        ILogger<MapZonesController> logger,
        IOptions<MapZoneServiceOptions> mapZoneOptions,
        IExternalApiTokenService externalApiTokenService)
    {
        _dbContext = dbContext;
        _httpClientFactory = httpClientFactory;
        _logger = logger;
        _mapZoneOptions = mapZoneOptions.Value;
        _externalApiTokenService = externalApiTokenService;
    }

    [HttpPost("sync")]
    public async Task<ActionResult<MapZoneSyncResultDto>> SyncAsync(CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(_mapZoneOptions.MapZoneListUrl) ||
            !Uri.TryCreate(_mapZoneOptions.MapZoneListUrl, UriKind.Absolute, out var requestUri))
        {
            _logger.LogError("Map Zone list URL is not configured correctly.");
            return StatusCode(StatusCodes.Status500InternalServerError, new
            {
                Code = StatusCodes.Status500InternalServerError,
                Message = "Map Zone list URL is not configured."
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

        var httpClient = _httpClientFactory.CreateClient();

        var apiRequest = new HttpRequestMessage(HttpMethod.Post, requestUri)
        {
            Content = JsonContent.Create(new QueryMapZonesRequest
            {
                PageNum = SyncPageNumber,
                PageSize = SyncPageSize
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

        _logger.LogInformation("=== Map Zone Sync Request Debug ===");
        _logger.LogInformation("Target URI: {Uri}", requestUri);
        _logger.LogInformation("Token Length: {Length}", token.Length);
        _logger.LogInformation("=== End Request Debug ===");

        try
        {
            using var response = await httpClient.SendAsync(apiRequest, cancellationToken);
            var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);

            _logger.LogInformation("=== Map Zone Sync Response Debug ===");
            _logger.LogInformation("Response Status: {StatusCode}", response.StatusCode);
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

            var jsonOptions = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

            // Try parsing as real backend format first
            if (responseContent.Contains("\"success\""))
            {
                var realBackendResponse = JsonSerializer.Deserialize<RealBackendApiResponse<MapZonePage>>(
                    responseContent, jsonOptions);

                if (realBackendResponse is null || !realBackendResponse.Success)
                {
                    var message = realBackendResponse?.Message ?? "Failed to sync map zones";
                    return StatusCode((int)response.StatusCode, new { Code = "ERROR", Message = message });
                }

                var mapZones = realBackendResponse.Data?.Content;
                if (mapZones is null || mapZones.Count == 0)
                {
                    return Ok(new MapZoneSyncResultDto { Total = 0, Inserted = 0, Updated = 0 });
                }

                return await ProcessMapZonesAsync(mapZones, cancellationToken);
            }
            else
            {
                // Parse as simulator format
                var simulatorResponse = JsonSerializer.Deserialize<SimulatorApiResponse<MapZonePage>>(
                    responseContent, jsonOptions);

                if (simulatorResponse is null || simulatorResponse.Succ is not true)
                {
                    var message = simulatorResponse?.Msg ?? "Failed to sync map zones from simulator";
                    return StatusCode((int)response.StatusCode, new { Code = (int)response.StatusCode, Message = message });
                }

                var mapZones = simulatorResponse.Data?.Content;
                if (mapZones is null || mapZones.Count == 0)
                {
                    return Ok(new MapZoneSyncResultDto { Total = 0, Inserted = 0, Updated = 0 });
                }

                return await ProcessMapZonesAsync(mapZones, cancellationToken);
            }
        }
        catch (HttpRequestException httpRequestException)
        {
            _logger.LogError(httpRequestException, "Error calling API simulator");
            return StatusCode(StatusCodes.Status502BadGateway, new
            {
                Code = (int)HttpStatusCode.BadGateway,
                Message = "Unable to reach the API simulator."
            });
        }
    }

    private async Task<ActionResult<MapZoneSyncResultDto>> ProcessMapZonesAsync(
        IEnumerable<MapZoneDto> mapZones,
        CancellationToken cancellationToken)
    {
        var mapZoneList = mapZones.ToList();

        // Load all existing map zones from database
        var allExistingMapZones = await _dbContext.MapZones.ToListAsync(cancellationToken);
        var existingMapZones = allExistingMapZones.ToDictionary(m => m.ZoneCode);

        var inserted = 0;
        var updated = 0;

        foreach (var mapZone in mapZoneList)
        {
            if (string.IsNullOrWhiteSpace(mapZone.ZoneCode))
            {
                _logger.LogWarning("Skipped map zone with missing zone code.");
                continue;
            }

            if (!existingMapZones.TryGetValue(mapZone.ZoneCode, out var entity))
            {
                entity = new MapZone { ZoneCode = mapZone.ZoneCode };
                _dbContext.MapZones.Add(entity);
                existingMapZones[mapZone.ZoneCode] = entity;
                inserted++;
            }
            else
            {
                updated++;
            }

            entity.ExternalMapZoneId = mapZone.Id;
            entity.CreateTime = ParseDateTime(mapZone.CreateTime) ??
                                (entity.CreateTime == default ? DateTime.UtcNow : entity.CreateTime);
            entity.CreateBy = mapZone.CreateBy ?? string.Empty;
            entity.CreateApp = mapZone.CreateApp ?? string.Empty;
            entity.LastUpdateTime = ParseDateTime(mapZone.LastUpdateTime) ?? DateTime.UtcNow;
            entity.LastUpdateBy = mapZone.LastUpdateBy ?? string.Empty;
            entity.LastUpdateApp = mapZone.LastUpdateApp ?? string.Empty;
            entity.ZoneName = mapZone.ZoneName ?? string.Empty;
            entity.ZoneDescription = mapZone.ZoneDescription ?? string.Empty;
            entity.ZoneColor = mapZone.ZoneColor ?? string.Empty;
            entity.MapCode = mapZone.MapCode ?? string.Empty;
            entity.FloorNumber = mapZone.FloorNumber ?? string.Empty;
            entity.Points = mapZone.Points ?? string.Empty;
            entity.Nodes = mapZone.Nodes ?? string.Empty;
            entity.Edges = mapZone.Edges ?? string.Empty;
            entity.CustomerUi = mapZone.CustomerUi ?? string.Empty;
            entity.ZoneType = mapZone.ZoneType ?? string.Empty;
            entity.Status = mapZone.Status;
            entity.BeginTime = ParseDateTime(mapZone.BeginTime);
            entity.EndTime = ParseDateTime(mapZone.EndTime);

            // Serialize configs to JSON
            if (mapZone.Configs != null)
            {
                entity.Configs = JsonSerializer.Serialize(mapZone.Configs);
            }
        }

        await _dbContext.SaveChangesAsync(cancellationToken);

        return Ok(new MapZoneSyncResultDto
        {
            Total = mapZoneList.Count,
            Inserted = inserted,
            Updated = updated
        });
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<MapZoneSummaryDto>>> GetAsync(CancellationToken cancellationToken)
    {
        var mapZones = await _dbContext.MapZones
            .AsNoTracking()
            .OrderBy(m => m.ZoneName)
            .ToListAsync(cancellationToken);

        var result = mapZones.Select(m => new MapZoneSummaryDto
        {
            Id = m.Id,
            ZoneName = m.ZoneName,
            ZoneCode = m.ZoneCode,
            Layout = $"{m.MapCode}/{m.FloorNumber}",
            AreaPurpose = GetAreaPurpose(m.ZoneType),
            StatusText = m.Status == 1 ? "Enabled" : "Disabled",
            CreateTime = m.CreateTime,
            LastUpdateTime = m.LastUpdateTime
        }).ToList();

        _logger.LogInformation("Retrieved {Count} map zones", result.Count);

        return Ok(result);
    }

    [HttpGet("with-nodes")]
    public async Task<ActionResult<IEnumerable<MapZoneWithNodesDto>>> GetWithNodesAsync(CancellationToken cancellationToken)
    {
        var mapZones = await _dbContext.MapZones
            .AsNoTracking()
            .Where(m => m.Status == 1) // Only active zones
            .OrderBy(m => m.ZoneName)
            .ToListAsync(cancellationToken);

        var result = mapZones.Select(m => new MapZoneWithNodesDto
        {
            Id = m.Id,
            ZoneName = m.ZoneName,
            ZoneCode = m.ZoneCode,
            Nodes = m.Nodes,
            MapCode = m.MapCode
        }).ToList();

        _logger.LogInformation("Retrieved {Count} map zones with nodes", result.Count);

        return Ok(result);
    }

    private static string GetAreaPurpose(string zoneType)
    {
        return zoneType switch
        {
            "3" => "Workflow Area",
            "6" => "Robot Zone",
            _ => $"Type {zoneType}"
        };
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
