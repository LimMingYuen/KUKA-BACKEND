using System.Net.Http.Headers;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using QES_KUKA_AMR_API.Models.Missions;
using QES_KUKA_AMR_API.Options;

namespace QES_KUKA_AMR_API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class MissionDataController : ControllerBase
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<MissionDataController> _logger;
    private readonly MissionListServiceOptions _missionListOptions;

    public MissionDataController(
        IHttpClientFactory httpClientFactory,
        ILogger<MissionDataController> logger,
        IOptions<MissionListServiceOptions> missionListOptions)
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;
        _missionListOptions = missionListOptions.Value;
    }

    [HttpPost("mission/list")]
    public async Task<ActionResult<MissionListApiResponse>> GetMissionListAsync(
        [FromBody] MissionListRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            // Extract JWT token from request header
            if (!AuthenticationHeaderValue.TryParse(Request.Headers.Authorization, out var authHeader) ||
                string.IsNullOrWhiteSpace(authHeader.Parameter))
            {
                return StatusCode(StatusCodes.Status401Unauthorized, new
                {
                    success = false,
                    code = "401",
                    message = "Unauthorized: Missing or invalid authorization token"
                });
            }

            var token = authHeader.Parameter;

            // Validate configuration
            if (string.IsNullOrEmpty(_missionListOptions.MissionListUrl))
            {
                _logger.LogError("MissionListUrl is not configured");
                return StatusCode(StatusCodes.Status500InternalServerError, new
                {
                    success = false,
                    code = "500",
                    message = "Service configuration error"
                });
            }

            var httpClient = _httpClientFactory.CreateClient();

            // Create HTTP request
            var apiRequest = new HttpRequestMessage(HttpMethod.Post, _missionListOptions.MissionListUrl)
            {
                Content = JsonContent.Create(request)
            };

            // Forward JWT token
            apiRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

            // Add required headers for simulator
            apiRequest.Headers.Add("language", "en");
            apiRequest.Headers.Add("accept", "*/*");
            apiRequest.Headers.Add("wizards", "FRONT_END");

            _logger.LogInformation("Forwarding mission list request to simulator at {Url}", _missionListOptions.MissionListUrl);

            // Send request to simulator
            using var response = await httpClient.SendAsync(apiRequest, cancellationToken);

            // Read and parse response
            var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);

            if (string.IsNullOrEmpty(responseContent))
            {
                _logger.LogError("Empty response from simulator");
                return StatusCode(StatusCodes.Status502BadGateway, new
                {
                    success = false,
                    code = "502",
                    message = "Bad Gateway: Empty response from upstream service"
                });
            }

            _logger.LogInformation("Received response from simulator: {StatusCode}", response.StatusCode);

            // Try to deserialize the response
            try
            {
                var serviceResponse = JsonSerializer.Deserialize<MissionListApiResponse>(responseContent);

                if (serviceResponse is null)
                {
                    _logger.LogError("Failed to deserialize simulator response");
                    return StatusCode(StatusCodes.Status502BadGateway, new
                    {
                        success = false,
                        code = "502",
                        message = "Bad Gateway: Invalid response format from upstream service"
                    });
                }

                // Return the response from simulator
                return StatusCode((int)response.StatusCode, serviceResponse);
            }
            catch (JsonException ex)
            {
                _logger.LogError(ex, "Failed to parse simulator response: {Content}", responseContent);
                return StatusCode(StatusCodes.Status502BadGateway, new
                {
                    success = false,
                    code = "502",
                    message = "Bad Gateway: Failed to parse response from upstream service"
                });
            }
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Error while calling mission list service at {Url}", _missionListOptions.MissionListUrl);
            return StatusCode(StatusCodes.Status502BadGateway, new
            {
                success = false,
                code = "502",
                message = "Bad Gateway: Failed to communicate with upstream service"
            });
        }
        catch (TaskCanceledException ex) when (ex.InnerException is TimeoutException)
        {
            _logger.LogError(ex, "Timeout while calling mission list service at {Url}", _missionListOptions.MissionListUrl);
            return StatusCode(StatusCodes.Status504GatewayTimeout, new
            {
                success = false,
                code = "504",
                message = "Gateway Timeout: Upstream service request timed out"
            });
        }
        catch (TaskCanceledException)
        {
            _logger.LogWarning("Mission list request was cancelled");
            return StatusCode(StatusCodes.Status499ClientClosedRequest, new
            {
                success = false,
                code = "499",
                message = "Client closed request"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error in GetMissionListAsync");
            return StatusCode(StatusCodes.Status500InternalServerError, new
            {
                success = false,
                code = "500",
                message = "Internal server error"
            });
        }
    }
}
