using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using QES_KUKA_AMR_API.Data;
using QES_KUKA_AMR_API.Data.Entities;
using QES_KUKA_AMR_API.Models.Config;
using QES_KUKA_AMR_API.Models.QrCode;
using QES_KUKA_AMR_API.Options;

namespace QES_KUKA_AMR_API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class QrCodesController : ControllerBase
{
    private const int SyncPageNumber = 1;
    private const int SyncPageSize = 10_000;

    private readonly ApplicationDbContext _dbContext;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<QrCodesController> _logger;
    private readonly QrCodeServiceOptions _qrCodeOptions;

    public QrCodesController(
        ApplicationDbContext dbContext,
        IHttpClientFactory httpClientFactory,
        ILogger<QrCodesController> logger,
        IOptions<QrCodeServiceOptions> qrCodeOptions)
    {
        _dbContext = dbContext;
        _httpClientFactory = httpClientFactory;
        _logger = logger;
        _qrCodeOptions = qrCodeOptions.Value;
    }

    [HttpPost("sync")]
    public async Task<ActionResult<QrCodeSyncResultDto>> SyncAsync(CancellationToken cancellationToken)
    {
        if (!AuthenticationHeaderValue.TryParse(Request.Headers.Authorization, out var authHeader) ||
            string.IsNullOrWhiteSpace(authHeader.Parameter))
        {
            return StatusCode(StatusCodes.Status401Unauthorized, new
            {
                Code = StatusCodes.Status401Unauthorized,
                Message = "Missing or invalid Authorization header."
            });
        }

        // Strip "Bearer " prefix if it was accidentally included in the parameter
        var token = authHeader.Parameter;
        if (token.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
        {
            token = token.Substring(7).Trim();
        }

        if (string.IsNullOrWhiteSpace(_qrCodeOptions.QrCodeListUrl) ||
            !Uri.TryCreate(_qrCodeOptions.QrCodeListUrl, UriKind.Absolute, out var requestUri))
        {
            _logger.LogError("QR Code list URL is not configured correctly.");
            return StatusCode(StatusCodes.Status500InternalServerError, new
            {
                Code = StatusCodes.Status500InternalServerError,
                Message = "QR Code list URL is not configured."
            });
        }

        var httpClient = _httpClientFactory.CreateClient();

        var apiRequest = new HttpRequestMessage(HttpMethod.Post, requestUri)
        {
            Content = JsonContent.Create(new QueryQrCodesRequest
            {
                PageNum = SyncPageNumber,
                PageSize = SyncPageSize
            })
        };
        apiRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Add custom headers required by real backend
        apiRequest.Content.Headers.ContentType = new MediaTypeHeaderValue("application/json")
        {
            CharSet = "UTF-8"
        };
        apiRequest.Headers.Add("language", "en");
        apiRequest.Headers.Add("accept", "*/*");
        apiRequest.Headers.Add("wizards", "FRONT_END");

        // Forward any cookies from the original request
        if (Request.Headers.TryGetValue("Cookie", out var cookies))
        {
            apiRequest.Headers.Add("Cookie", cookies.ToString());
        }

        // Log request details for debugging
        _logger.LogInformation("=== QR Code Sync Request Debug ===");
        _logger.LogInformation("Target URI: {Uri}", requestUri);
        _logger.LogInformation("Auth Scheme: {Scheme}", authHeader.Scheme);
        _logger.LogInformation("Cleaned Token: {Token}", token);
        _logger.LogInformation("=== End Request Debug ===");

        try
        {
            using var response = await httpClient.SendAsync(apiRequest, cancellationToken);

            // Log response for debugging
            var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);

            _logger.LogInformation("=== QR Code Sync Response Debug ===");
            _logger.LogInformation("Response Status: {StatusCode} ({StatusCodeInt})", response.StatusCode, (int)response.StatusCode);
            _logger.LogInformation("Response Body: {Content}", responseContent);
            _logger.LogInformation("=== End Response Debug ===");

            if (string.IsNullOrWhiteSpace(responseContent) || responseContent.TrimStart().StartsWith('<'))
            {
                _logger.LogError("API returned HTML instead of JSON. Status: {StatusCode}", response.StatusCode);
                return StatusCode(StatusCodes.Status502BadGateway, new
                {
                    Code = (int)HttpStatusCode.BadGateway,
                    Message = $"Backend returned HTML instead of JSON. Status: {response.StatusCode}"
                });
            }

            var jsonOptions = new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true };

            // Try parsing as real backend format first (has "success" field)
            if (responseContent.Contains("\"success\""))
            {
                var realBackendResponse = System.Text.Json.JsonSerializer.Deserialize<RealBackendApiResponse<QrCodePage>>(
                    responseContent, jsonOptions);

                if (realBackendResponse is null)
                {
                    _logger.LogError("Failed to parse real backend response. Status Code: {StatusCode}", response.StatusCode);
                    return StatusCode(StatusCodes.Status502BadGateway, new
                    {
                        Code = (int)HttpStatusCode.BadGateway,
                        Message = "Failed to parse response from the backend."
                    });
                }

                if (!realBackendResponse.Success)
                {
                    var message = realBackendResponse.Message ?? realBackendResponse.Code ?? "Authentication failed";
                    _logger.LogWarning("QR Code sync failed. Status: {Status}, Message: {Message}",
                        response.StatusCode, message);

                    return StatusCode((int)response.StatusCode, new
                    {
                        Code = realBackendResponse.Code ?? "ERROR",
                        Message = message
                    });
                }

                var qrCodes = realBackendResponse.Data?.Content;

                if (qrCodes is null || qrCodes.Count == 0)
                {
                    return Ok(new QrCodeSyncResultDto
                    {
                        Total = 0,
                        Inserted = 0,
                        Updated = 0
                    });
                }

                // Process QR codes
                return await ProcessQrCodesAsync(qrCodes, cancellationToken);
            }
            else
            {
                // Parse as simulator format (has "succ" field)
                var simulatorResponse = System.Text.Json.JsonSerializer.Deserialize<SimulatorApiResponse<QrCodePage>>(
                    responseContent, jsonOptions);

                if (simulatorResponse is null)
                {
                    _logger.LogError("API simulator returned no content. Status Code: {StatusCode}", response.StatusCode);
                    return StatusCode(StatusCodes.Status502BadGateway, new
                    {
                        Code = (int)HttpStatusCode.BadGateway,
                        Message = "Failed to retrieve a response from the API simulator."
                    });
                }

                if (!response.IsSuccessStatusCode || simulatorResponse.Succ is not true)
                {
                    var message = simulatorResponse.Msg ?? "Failed to sync QR codes from the API simulator.";
                    _logger.LogWarning("QR Code sync failed. Status: {Status}, Message: {Message}", response.StatusCode, message);

                    return StatusCode((int)response.StatusCode, new
                    {
                        Code = (int)response.StatusCode,
                        Message = message
                    });
                }

                var qrCodes = simulatorResponse.Data?.Content;

                if (qrCodes is null || qrCodes.Count == 0)
                {
                    return Ok(new QrCodeSyncResultDto
                    {
                        Total = 0,
                        Inserted = 0,
                        Updated = 0
                    });
                }

                // Process QR codes
                return await ProcessQrCodesAsync(qrCodes, cancellationToken);
            }
        }
        catch (HttpRequestException httpRequestException)
        {
            _logger.LogError(httpRequestException, "Error while calling API simulator at {BaseUrl}", requestUri);
            return StatusCode(StatusCodes.Status502BadGateway, new
            {
                Code = (int)HttpStatusCode.BadGateway,
                Message = "Unable to reach the API simulator."
            });
        }
    }

    private async Task<ActionResult<QrCodeSyncResultDto>> ProcessQrCodesAsync(
        IEnumerable<QrCodeDto> qrCodes,
        CancellationToken cancellationToken)
    {
        var qrCodeList = qrCodes.ToList();

        // Load all existing QR codes from database
        var allExistingQrCodes = await _dbContext.QrCodes.ToListAsync(cancellationToken);

        // Create a dictionary for quick lookup by NodeLabel and MapCode
        var existingQrCodes = allExistingQrCodes
            .ToDictionary(q => $"{q.NodeLabel}|{q.MapCode}");

        var inserted = 0;
        var updated = 0;

        foreach (var qrCode in qrCodeList)
        {
            if (string.IsNullOrWhiteSpace(qrCode.NodeLabel) || string.IsNullOrWhiteSpace(qrCode.MapCode))
            {
                _logger.LogWarning("Skipped QR code with missing node label or map code.");
                continue;
            }

            var key = $"{qrCode.NodeLabel}|{qrCode.MapCode}";

            if (!existingQrCodes.TryGetValue(key, out var entity))
            {
                entity = new QrCode
                {
                    NodeLabel = qrCode.NodeLabel,
                    MapCode = qrCode.MapCode
                };
                _dbContext.QrCodes.Add(entity);
                existingQrCodes[key] = entity;
                inserted++;
            }
            else
            {
                updated++;
            }

            entity.CreateTime = ParseDateTime(qrCode.CreateTime) ??
                                (entity.CreateTime == default ? DateTime.UtcNow : entity.CreateTime);
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

        return Ok(new QrCodeSyncResultDto
        {
            Total = qrCodeList.Count,
            Inserted = inserted,
            Updated = updated
        });
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<QrCodeSummaryDto>>> GetAsync(CancellationToken cancellationToken)
    {
        var qrCodes = await _dbContext.QrCodes
            .AsNoTracking()
            .OrderBy(q => q.NodeLabel)
            .Select(q => new QrCodeSummaryDto
            {
                Id = q.Id,
                NodeLabel = q.NodeLabel,
                MapCode = q.MapCode,
                FloorNumber = q.FloorNumber,
                NodeNumber = q.NodeNumber,
                Reliability = q.Reliability,
                ReportTimes = q.ReportTimes,
                LastUpdateTime = q.LastUpdateTime.ToString("yyyy-MM-dd HH:mm:ss")
            })
            .ToListAsync(cancellationToken);

        _logger.LogInformation("Retrieved {Count} QR codes", qrCodes.Count);

        return Ok(qrCodes);
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
