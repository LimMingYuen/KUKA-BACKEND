using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using QES_KUKA_AMR_API.Data;
using QES_KUKA_AMR_API.Data.Entities;
using QES_KUKA_AMR_API.Models.AutoSync;
using QES_KUKA_AMR_API.Models.Config;
using QES_KUKA_AMR_API.Models.MapZone;
using QES_KUKA_AMR_API.Models.MobileRobot;
using QES_KUKA_AMR_API.Models.QrCode;
using QES_KUKA_AMR_API.Models.Workflow;
using QES_KUKA_AMR_API.Options;
using QES_KUKA_AMR_API.Services.Auth;

namespace QES_KUKA_AMR_API.Services.Sync;

/// <summary>
/// Service for synchronizing data from external APIs
/// </summary>
public class SyncService : ISyncService
{
    private const int SyncPageNumber = 1;
    private const int SyncPageSize = 10_000;

    private readonly ApplicationDbContext _dbContext;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<SyncService> _logger;
    private readonly IExternalApiTokenService _externalApiTokenService;
    private readonly MissionServiceOptions _missionOptions;
    private readonly QrCodeServiceOptions _qrCodeOptions;
    private readonly MapZoneServiceOptions _mapZoneOptions;
    private readonly MobileRobotServiceOptions _mobileRobotOptions;

    public SyncService(
        ApplicationDbContext dbContext,
        IHttpClientFactory httpClientFactory,
        ILogger<SyncService> logger,
        IExternalApiTokenService externalApiTokenService,
        IOptions<MissionServiceOptions> missionOptions,
        IOptions<QrCodeServiceOptions> qrCodeOptions,
        IOptions<MapZoneServiceOptions> mapZoneOptions,
        IOptions<MobileRobotServiceOptions> mobileRobotOptions)
    {
        _dbContext = dbContext;
        _httpClientFactory = httpClientFactory;
        _logger = logger;
        _externalApiTokenService = externalApiTokenService;
        _missionOptions = missionOptions.Value;
        _qrCodeOptions = qrCodeOptions.Value;
        _mapZoneOptions = mapZoneOptions.Value;
        _mobileRobotOptions = mobileRobotOptions.Value;
    }

    #region Workflows Sync

    public async Task<SyncResultDto> SyncWorkflowsAsync(CancellationToken cancellationToken = default)
    {
        const string apiName = "Workflows";

        try
        {
            if (string.IsNullOrWhiteSpace(_missionOptions.WorkflowQueryUrl) ||
                !Uri.TryCreate(_missionOptions.WorkflowQueryUrl, UriKind.Absolute, out var requestUri))
            {
                return CreateErrorResult(apiName, "Workflow query URL is not configured.");
            }

            var token = await GetTokenAsync(cancellationToken);
            if (token == null)
            {
                return CreateErrorResult(apiName, "Failed to authenticate with external API.");
            }

            var request = CreateRequest(requestUri, token, new QueryWorkflowDiagramsRequest
            {
                PageNum = SyncPageNumber,
                PageSize = SyncPageSize
            });

            var (success, content, error) = await SendRequestAsync(request, cancellationToken);
            if (!success)
            {
                return CreateErrorResult(apiName, error ?? "Request failed");
            }

            var workflows = ParseResponse<WorkflowDiagramPage, WorkflowDiagramDto>(content!, apiName);
            if (workflows == null)
            {
                return CreateErrorResult(apiName, "Failed to parse response");
            }

            if (workflows.Count == 0)
            {
                return new SyncResultDto { ApiName = apiName, Success = true, Total = 0, Inserted = 0, Updated = 0 };
            }

            return await ProcessWorkflowsAsync(workflows, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error syncing {ApiName}", apiName);
            return CreateErrorResult(apiName, ex.Message);
        }
    }

    private async Task<SyncResultDto> ProcessWorkflowsAsync(
        List<WorkflowDiagramDto> workflows,
        CancellationToken cancellationToken)
    {
        var workflowCodes = workflows.Select(w => w.WorkflowCode).Where(code => !string.IsNullOrWhiteSpace(code)).ToArray();
        var existingWorkflows = await _dbContext.WorkflowDiagrams
            .Where(w => workflowCodes.Contains(w.WorkflowCode))
            .ToDictionaryAsync(w => w.WorkflowCode, cancellationToken);

        var inserted = 0;
        var updated = 0;

        foreach (var workflow in workflows)
        {
            if (string.IsNullOrWhiteSpace(workflow.WorkflowCode))
            {
                continue;
            }

            if (!existingWorkflows.TryGetValue(workflow.WorkflowCode, out var entity))
            {
                entity = new WorkflowDiagram { WorkflowCode = workflow.WorkflowCode };
                _dbContext.WorkflowDiagrams.Add(entity);
                existingWorkflows[workflow.WorkflowCode] = entity;
                inserted++;
            }
            else
            {
                updated++;
            }

            entity.ExternalWorkflowId = workflow.Id;
            entity.WorkflowOuterCode = workflow.WorkflowOuterCode ?? string.Empty;
            entity.WorkflowName = workflow.WorkflowName ?? string.Empty;
            entity.WorkflowModel = workflow.WorkflowModel;
            entity.RobotTypeClass = workflow.RobotTypeClass;
            entity.MapCode = workflow.MapCode ?? string.Empty;
            entity.ButtonName = workflow.ButtonName;
            entity.CreateUsername = workflow.CreateUsername ?? string.Empty;
            entity.CreateTime = ParseDateTime(workflow.CreateTime) ?? (entity.CreateTime == default ? DateTime.UtcNow : entity.CreateTime);
            entity.UpdateUsername = workflow.UpdateUsername ?? string.Empty;
            entity.UpdateTime = ParseDateTime(workflow.UpdateTime) ?? DateTime.UtcNow;
            entity.Status = workflow.Status;
            entity.NeedConfirm = workflow.NeedConfirm;
            entity.LockRobotAfterFinish = workflow.LockRobotAfterFinish;
            entity.WorkflowPriority = workflow.WorkflowPriority;
            entity.TargetAreaCode = workflow.TargetAreaCode;
            entity.PreSelectedRobotCellCode = workflow.PreSelectedRobotCellCode;
            entity.PreSelectedRobotId = workflow.PreSelectedRobotId;
        }

        await _dbContext.SaveChangesAsync(cancellationToken);

        return new SyncResultDto
        {
            ApiName = "Workflows",
            Success = true,
            Total = workflows.Count,
            Inserted = inserted,
            Updated = updated
        };
    }

    #endregion

    #region QR Codes Sync

    public async Task<SyncResultDto> SyncQrCodesAsync(CancellationToken cancellationToken = default)
    {
        const string apiName = "QrCodes";

        try
        {
            if (string.IsNullOrWhiteSpace(_qrCodeOptions.QrCodeListUrl) ||
                !Uri.TryCreate(_qrCodeOptions.QrCodeListUrl, UriKind.Absolute, out var requestUri))
            {
                return CreateErrorResult(apiName, "QR Code list URL is not configured.");
            }

            var token = await GetTokenAsync(cancellationToken);
            if (token == null)
            {
                return CreateErrorResult(apiName, "Failed to authenticate with external API.");
            }

            var request = CreateRequest(requestUri, token, new QueryQrCodesRequest
            {
                PageNum = SyncPageNumber,
                PageSize = SyncPageSize
            });

            var (success, content, error) = await SendRequestAsync(request, cancellationToken);
            if (!success)
            {
                return CreateErrorResult(apiName, error ?? "Request failed");
            }

            var qrCodes = ParseResponse<QrCodePage, QrCodeDto>(content!, apiName);
            if (qrCodes == null)
            {
                return CreateErrorResult(apiName, "Failed to parse response");
            }

            if (qrCodes.Count == 0)
            {
                return new SyncResultDto { ApiName = apiName, Success = true, Total = 0, Inserted = 0, Updated = 0 };
            }

            return await ProcessQrCodesAsync(qrCodes, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error syncing {ApiName}", apiName);
            return CreateErrorResult(apiName, ex.Message);
        }
    }

    private async Task<SyncResultDto> ProcessQrCodesAsync(
        List<QrCodeDto> qrCodes,
        CancellationToken cancellationToken)
    {
        var allExistingQrCodes = await _dbContext.QrCodes.ToListAsync(cancellationToken);
        var existingQrCodes = allExistingQrCodes.ToDictionary(q => $"{q.NodeLabel}|{q.MapCode}");

        var inserted = 0;
        var updated = 0;

        foreach (var qrCode in qrCodes)
        {
            if (string.IsNullOrWhiteSpace(qrCode.NodeLabel) || string.IsNullOrWhiteSpace(qrCode.MapCode))
            {
                continue;
            }

            var key = $"{qrCode.NodeLabel}|{qrCode.MapCode}";

            if (!existingQrCodes.TryGetValue(key, out var entity))
            {
                entity = new QrCode { NodeLabel = qrCode.NodeLabel, MapCode = qrCode.MapCode };
                _dbContext.QrCodes.Add(entity);
                existingQrCodes[key] = entity;
                inserted++;
            }
            else
            {
                updated++;
            }

            entity.ExternalQrCodeId = qrCode.Id;
            entity.CreateTime = ParseDateTime(qrCode.CreateTime) ?? (entity.CreateTime == default ? DateTime.UtcNow : entity.CreateTime);
            entity.CreateBy = qrCode.CreateBy ?? string.Empty;
            entity.CreateApp = qrCode.CreateApp ?? string.Empty;
            entity.LastUpdateTime = ParseDateTime(qrCode.LastUpdateTime) ?? DateTime.UtcNow;
            entity.LastUpdateBy = qrCode.LastUpdateBy ?? string.Empty;
            entity.LastUpdateApp = qrCode.LastUpdateApp ?? string.Empty;
            entity.Reliability = qrCode.Reliability;
            entity.FloorNumber = qrCode.FloorNumber ?? string.Empty;
            entity.NodeNumber = qrCode.NodeNumber;
            entity.ReportTimes = qrCode.ReportTimes;
        }

        await _dbContext.SaveChangesAsync(cancellationToken);

        return new SyncResultDto
        {
            ApiName = "QrCodes",
            Success = true,
            Total = qrCodes.Count,
            Inserted = inserted,
            Updated = updated
        };
    }

    #endregion

    #region Map Zones Sync

    public async Task<SyncResultDto> SyncMapZonesAsync(CancellationToken cancellationToken = default)
    {
        const string apiName = "MapZones";

        try
        {
            if (string.IsNullOrWhiteSpace(_mapZoneOptions.MapZoneListUrl) ||
                !Uri.TryCreate(_mapZoneOptions.MapZoneListUrl, UriKind.Absolute, out var requestUri))
            {
                return CreateErrorResult(apiName, "Map Zone list URL is not configured.");
            }

            var token = await GetTokenAsync(cancellationToken);
            if (token == null)
            {
                return CreateErrorResult(apiName, "Failed to authenticate with external API.");
            }

            var request = CreateRequest(requestUri, token, new QueryMapZonesRequest
            {
                PageNum = SyncPageNumber,
                PageSize = SyncPageSize
            });

            var (success, content, error) = await SendRequestAsync(request, cancellationToken);
            if (!success)
            {
                return CreateErrorResult(apiName, error ?? "Request failed");
            }

            var mapZones = ParseResponse<MapZonePage, MapZoneDto>(content!, apiName);
            if (mapZones == null)
            {
                return CreateErrorResult(apiName, "Failed to parse response");
            }

            if (mapZones.Count == 0)
            {
                return new SyncResultDto { ApiName = apiName, Success = true, Total = 0, Inserted = 0, Updated = 0 };
            }

            return await ProcessMapZonesAsync(mapZones, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error syncing {ApiName}", apiName);
            return CreateErrorResult(apiName, ex.Message);
        }
    }

    private async Task<SyncResultDto> ProcessMapZonesAsync(
        List<MapZoneDto> mapZones,
        CancellationToken cancellationToken)
    {
        var allExistingMapZones = await _dbContext.MapZones.ToListAsync(cancellationToken);
        var existingMapZones = allExistingMapZones.ToDictionary(m => m.ZoneCode);

        var inserted = 0;
        var updated = 0;

        foreach (var mapZone in mapZones)
        {
            if (string.IsNullOrWhiteSpace(mapZone.ZoneCode))
            {
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
            entity.CreateTime = ParseDateTime(mapZone.CreateTime) ?? (entity.CreateTime == default ? DateTime.UtcNow : entity.CreateTime);
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

            if (mapZone.Configs != null)
            {
                entity.Configs = JsonSerializer.Serialize(mapZone.Configs);
            }
        }

        await _dbContext.SaveChangesAsync(cancellationToken);

        return new SyncResultDto
        {
            ApiName = "MapZones",
            Success = true,
            Total = mapZones.Count,
            Inserted = inserted,
            Updated = updated
        };
    }

    #endregion

    #region Mobile Robots Sync

    public async Task<SyncResultDto> SyncMobileRobotsAsync(CancellationToken cancellationToken = default)
    {
        const string apiName = "MobileRobots";

        try
        {
            if (string.IsNullOrWhiteSpace(_mobileRobotOptions.MobileRobotListUrl) ||
                !Uri.TryCreate(_mobileRobotOptions.MobileRobotListUrl, UriKind.Absolute, out var requestUri))
            {
                return CreateErrorResult(apiName, "Mobile Robot list URL is not configured.");
            }

            var token = await GetTokenAsync(cancellationToken);
            if (token == null)
            {
                return CreateErrorResult(apiName, "Failed to authenticate with external API.");
            }

            // Mobile robot uses specific request format matching QueryMobileRobotRequest
            var request = CreateRequest(requestUri, token, new
            {
                query = new List<string>(),
                pageNum = -1,
                pageSize = SyncPageSize,
                orderBy = "lastUpdateTime",
                asc = false
            });

            var (success, content, error) = await SendRequestAsync(request, cancellationToken);
            if (!success)
            {
                return CreateErrorResult(apiName, error ?? "Request failed");
            }

            var mobileRobots = ParseMobileRobotResponse(content!, apiName);
            if (mobileRobots == null)
            {
                return CreateErrorResult(apiName, "Failed to parse response");
            }

            if (mobileRobots.Count == 0)
            {
                return new SyncResultDto { ApiName = apiName, Success = true, Total = 0, Inserted = 0, Updated = 0 };
            }

            return await ProcessMobileRobotsAsync(mobileRobots, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error syncing {ApiName}", apiName);
            return CreateErrorResult(apiName, ex.Message);
        }
    }

    private List<MobileRobotDto>? ParseMobileRobotResponse(string content, string apiName)
    {
        var jsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            NumberHandling = System.Text.Json.Serialization.JsonNumberHandling.AllowReadingFromString
        };

        try
        {
            // Try real backend format first (has "success" field with boolean value)
            if (content.Contains("\"success\":") || content.Contains("\"success\" :"))
            {
                _logger.LogDebug("{ApiName}: Attempting to parse as real backend format", apiName);
                var realBackendResponse = JsonSerializer.Deserialize<RealBackendApiResponse<MobileRobotPageWrapper>>(content, jsonOptions);
                if (realBackendResponse?.Success == true)
                {
                    var robots = realBackendResponse.Data?.PageData?.Content;
                    _logger.LogDebug("{ApiName}: Real backend format parsed successfully, {Count} robots found", apiName, robots?.Count ?? 0);
                    return robots;
                }
                _logger.LogWarning("{ApiName}: Real backend response success=false or null. Message: {Message}",
                    apiName, realBackendResponse?.Message ?? "null response");
            }

            // Try simulator format (has "succ" field)
            if (content.Contains("\"succ\":") || content.Contains("\"succ\" :"))
            {
                _logger.LogDebug("{ApiName}: Attempting to parse as simulator format", apiName);
                var simulatorResponse = JsonSerializer.Deserialize<SimulatorApiResponse<MobileRobotPage>>(content, jsonOptions);
                if (simulatorResponse?.Succ == true)
                {
                    var robots = simulatorResponse.Data?.Content;
                    _logger.LogDebug("{ApiName}: Simulator format parsed successfully, {Count} robots found", apiName, robots?.Count ?? 0);
                    return robots;
                }
                _logger.LogWarning("{ApiName}: Simulator response succ=false or null. Message: {Message}",
                    apiName, simulatorResponse?.Msg ?? "null response");
            }

            // Log the response content preview if no format matched
            var preview = content.Length > 500 ? content.Substring(0, 500) + "..." : content;
            _logger.LogWarning("{ApiName}: Could not detect response format. Preview: {Preview}", apiName, preview);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to parse {ApiName} response. Content preview: {Preview}",
                apiName, content.Length > 300 ? content.Substring(0, 300) + "..." : content);
        }

        return null;
    }

    private async Task<SyncResultDto> ProcessMobileRobotsAsync(
        List<MobileRobotDto> mobileRobots,
        CancellationToken cancellationToken)
    {
        var allExistingMobileRobots = await _dbContext.MobileRobots.ToListAsync(cancellationToken);
        var existingMobileRobots = allExistingMobileRobots.ToDictionary(m => m.RobotId);

        var inserted = 0;
        var updated = 0;

        foreach (var robot in mobileRobots)
        {
            if (string.IsNullOrWhiteSpace(robot.RobotId))
            {
                continue;
            }

            if (!existingMobileRobots.TryGetValue(robot.RobotId, out var entity))
            {
                entity = new MobileRobot { RobotId = robot.RobotId };
                _dbContext.MobileRobots.Add(entity);
                existingMobileRobots[robot.RobotId] = entity;
                inserted++;
            }
            else
            {
                updated++;
            }

            entity.ExternalMobileRobotId = robot.Id;
            entity.CreateTime = ParseDateTime(robot.CreateTime) ?? (entity.CreateTime == default ? DateTime.UtcNow : entity.CreateTime);
            entity.CreateBy = robot.CreateBy ?? string.Empty;
            entity.CreateApp = robot.CreateApp ?? string.Empty;
            entity.LastUpdateTime = ParseDateTime(robot.LastUpdateTime) ?? DateTime.UtcNow;
            entity.LastUpdateBy = robot.LastUpdateBy ?? string.Empty;
            entity.LastUpdateApp = robot.LastUpdateApp ?? string.Empty;
            entity.RobotId = robot.RobotId ?? string.Empty;
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

        return new SyncResultDto
        {
            ApiName = "MobileRobots",
            Success = true,
            Total = mobileRobots.Count,
            Inserted = inserted,
            Updated = updated
        };
    }

    #endregion

    #region Run All Syncs

    public async Task<AutoSyncRunResultDto> RunAllEnabledSyncsAsync(
        bool syncWorkflows,
        bool syncQrCodes,
        bool syncMapZones,
        bool syncMobileRobots,
        CancellationToken cancellationToken = default)
    {
        var result = new AutoSyncRunResultDto();

        if (syncWorkflows)
        {
            result.Results.Add(await SyncWorkflowsAsync(cancellationToken));
        }

        if (syncQrCodes)
        {
            result.Results.Add(await SyncQrCodesAsync(cancellationToken));
        }

        if (syncMapZones)
        {
            result.Results.Add(await SyncMapZonesAsync(cancellationToken));
        }

        if (syncMobileRobots)
        {
            result.Results.Add(await SyncMobileRobotsAsync(cancellationToken));
        }

        return result;
    }

    #endregion

    #region Helper Methods

    private async Task<string?> GetTokenAsync(CancellationToken cancellationToken)
    {
        try
        {
            return await _externalApiTokenService.GetTokenAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to obtain external API token");
            return null;
        }
    }

    private HttpRequestMessage CreateRequest<T>(Uri requestUri, string token, T requestBody)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, requestUri)
        {
            Content = JsonContent.Create(requestBody)
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        request.Content.Headers.ContentType = new MediaTypeHeaderValue("application/json") { CharSet = "UTF-8" };
        request.Headers.Add("language", "en");
        request.Headers.Add("accept", "*/*");
        request.Headers.Add("wizards", "FRONT_END");

        return request;
    }

    private async Task<(bool Success, string? Content, string? Error)> SendRequestAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        try
        {
            var httpClient = _httpClientFactory.CreateClient();
            using var response = await httpClient.SendAsync(request, cancellationToken);
            var content = await response.Content.ReadAsStringAsync(cancellationToken);

            _logger.LogDebug("Sync response status: {StatusCode}", response.StatusCode);

            if (string.IsNullOrWhiteSpace(content) || content.TrimStart().StartsWith('<'))
            {
                return (false, null, "Backend returned HTML instead of JSON");
            }

            return (true, content, null);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "HTTP request failed");
            return (false, null, "Unable to reach the external API");
        }
    }

    private List<TItem>? ParseResponse<TPage, TItem>(string content, string apiName)
        where TPage : class
    {
        var jsonOptions = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

        try
        {
            // Try real backend format (has "success" field)
            if (content.Contains("\"success\""))
            {
                var realBackendResponse = JsonSerializer.Deserialize<RealBackendApiResponse<TPage>>(content, jsonOptions);
                if (realBackendResponse?.Success == true && realBackendResponse.Data != null)
                {
                    var contentProperty = typeof(TPage).GetProperty("Content");
                    return contentProperty?.GetValue(realBackendResponse.Data) as List<TItem>;
                }
            }
            else
            {
                // Simulator format (has "succ" field)
                var simulatorResponse = JsonSerializer.Deserialize<SimulatorApiResponse<TPage>>(content, jsonOptions);
                if (simulatorResponse?.Succ == true && simulatorResponse.Data != null)
                {
                    var contentProperty = typeof(TPage).GetProperty("Content");
                    return contentProperty?.GetValue(simulatorResponse.Data) as List<TItem>;
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to parse {ApiName} response", apiName);
        }

        return null;
    }

    private static SyncResultDto CreateErrorResult(string apiName, string errorMessage)
    {
        return new SyncResultDto
        {
            ApiName = apiName,
            Success = false,
            ErrorMessage = errorMessage
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

    #endregion
}
