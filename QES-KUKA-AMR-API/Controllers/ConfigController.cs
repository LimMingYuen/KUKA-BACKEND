using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using QES_KUKA_AMR_API.Models.Config;
using QES_KUKA_AMR_API.Options;
using QES_KUKA_AMR_API.Services.Auth;

namespace QES_KUKA_AMR_API.Controllers;

[ApiController]
[Route("api/config")]
public class ConfigController : ControllerBase
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<ConfigController> _logger;
    private readonly MissionServiceOptions _missionOptions;
    private readonly IExternalApiTokenService _externalApiTokenService;

    public ConfigController(
        IHttpClientFactory httpClientFactory,
        ILogger<ConfigController> logger,
        IOptions<MissionServiceOptions> missionOptions,
        IExternalApiTokenService externalApiTokenService)
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;
        _missionOptions = missionOptions.Value;
        _externalApiTokenService = externalApiTokenService;
    }

    [HttpPost("queryWorkflowDiagrams")]
    public async Task<IActionResult> QueryWorkflowDiagramsAsync(
        [FromBody] QueryWorkflowDiagramsRequest request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(_missionOptions.WorkflowQueryUrl) ||
            !Uri.TryCreate(_missionOptions.WorkflowQueryUrl, UriKind.Absolute, out var requestUri))
        {
            _logger.LogError("Workflow query URL is not configured correctly.");

            return StatusCode(StatusCodes.Status500InternalServerError, new SimulatorApiResponse<WorkflowDiagramPage>
            {
                Code = StatusCodes.Status500InternalServerError,
                Msg = "Workflow query URL is not configured.",
                Succ = false
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
            return StatusCode(StatusCodes.Status502BadGateway, new SimulatorApiResponse<WorkflowDiagramPage>
            {
                Code = StatusCodes.Status502BadGateway,
                Msg = "Failed to authenticate with external API.",
                Succ = false
            });
        }

        var httpClient = _httpClientFactory.CreateClient();

        var apiRequest = new HttpRequestMessage(
            HttpMethod.Post,
            requestUri)
        {
            Content = JsonContent.Create(request)
        };

        apiRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Add custom headers required by real backend
        apiRequest.Content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/json")
        {
            CharSet = "UTF-8"
        };
        apiRequest.Headers.Add("language", "en");
        apiRequest.Headers.Add("accept", "*/*");
        apiRequest.Headers.Add("wizards", "FRONT_END");

        try
        {
            using var response = await httpClient.SendAsync(apiRequest, cancellationToken);
            var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);

            var jsonOptions = new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true };

            // Try parsing as real backend format first (has "success" field)
            if (responseContent.Contains("\"success\""))
            {
                var realBackendResponse = System.Text.Json.JsonSerializer.Deserialize<RealBackendApiResponse<WorkflowDiagramPage>>(
                    responseContent, jsonOptions);

                if (realBackendResponse is null)
                {
                    _logger.LogError("Failed to parse real backend response. Status Code: {StatusCode}", response.StatusCode);
                    return StatusCode(StatusCodes.Status502BadGateway, new SimulatorApiResponse<WorkflowDiagramPage>
                    {
                        Code = (int)HttpStatusCode.BadGateway,
                        Msg = "Failed to parse response from the backend.",
                        Succ = false
                    });
                }

                if (!realBackendResponse.Success)
                {
                    var message = realBackendResponse.Message ?? realBackendResponse.Code ?? "Authentication failed";
                    _logger.LogWarning("Workflow query failed. Status: {Status}, Code: {Code}, Message: {Message}",
                        response.StatusCode, realBackendResponse.Code, message);

                    return StatusCode((int)response.StatusCode, new SimulatorApiResponse<WorkflowDiagramPage>
                    {
                        Code = (int)response.StatusCode,
                        Msg = message,
                        Succ = false
                    });
                }

                // Convert real backend format to simulator format for compatibility
                return Ok(new SimulatorApiResponse<WorkflowDiagramPage>
                {
                    Code = 0,
                    Msg = "Success",
                    Data = realBackendResponse.Data,
                    Succ = true
                });
            }
            else
            {
                // Parse as legacy simulator format (has "succ" field)
                var simulatorResponse = System.Text.Json.JsonSerializer.Deserialize<SimulatorApiResponse<WorkflowDiagramPage>>(
                    responseContent, jsonOptions);

                if (simulatorResponse is null)
                {
                    _logger.LogError("Mission service returned no content. Status Code: {StatusCode}", response.StatusCode);
                    return StatusCode(StatusCodes.Status502BadGateway, new SimulatorApiResponse<WorkflowDiagramPage>
                    {
                        Code = (int)HttpStatusCode.BadGateway,
                        Msg = "Failed to retrieve a response from the mission service.",
                        Succ = false
                    });
                }

                return StatusCode((int)response.StatusCode, simulatorResponse);
            }
        }
        catch (HttpRequestException httpRequestException)
        {
            _logger.LogError(
                httpRequestException,
                "Error while calling mission workflow endpoint at {BaseUrl}",
                requestUri);

            return StatusCode(StatusCodes.Status502BadGateway, new SimulatorApiResponse<WorkflowDiagramPage>
            {
                Code = (int)HttpStatusCode.BadGateway,
                Msg = "Unable to reach the mission workflow endpoint.",
                Succ = false
            });
        }
    }
}
