using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using QES_KUKA_AMR_API.Models.Missions;
using QES_KUKA_AMR_API.Options;

namespace QES_KUKA_AMR_API.Controllers;

[ApiController]
[Route("api/operation-feedback")]
public class OperationFeedbackController : ControllerBase
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<OperationFeedbackController> _logger;
    private readonly AmrServiceOptions _amrOptions;

    public OperationFeedbackController(
        IHttpClientFactory httpClientFactory,
        ILogger<OperationFeedbackController> logger,
        IOptions<AmrServiceOptions> amrOptions)
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;
        _amrOptions = amrOptions.Value;
    }

    [HttpPost]
    public async Task<ActionResult<OperationFeedbackResponse>> SendOperationFeedbackAsync(
        [FromBody] OperationFeedbackRequest request,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("=== OperationFeedbackController.SendOperationFeedbackAsync DEBUG ===");
        _logger.LogInformation("Received operation feedback - MissionCode={MissionCode}, Position={Position}, RequestId={RequestId}",
            request.MissionCode, request.Position, request.RequestId);

        if (string.IsNullOrWhiteSpace(_amrOptions.OperationFeedbackUrl) ||
            !Uri.TryCreate(_amrOptions.OperationFeedbackUrl, UriKind.Absolute, out var requestUri))
        {
            _logger.LogError("Operation feedback URL is not configured correctly.");
            return StatusCode(StatusCodes.Status500InternalServerError, new OperationFeedbackResponse
            {
                Code = "AMR_SERVICE_CONFIGURATION_ERROR",
                Message = "Operation feedback URL is not configured.",
                Success = false
            });
        }

        var httpClient = _httpClientFactory.CreateClient();

        var apiRequest = new HttpRequestMessage(HttpMethod.Post, requestUri)
        {
            Content = JsonContent.Create(request)
        };

        // Add custom headers required by AMR backend
        apiRequest.Content.Headers.ContentType = new MediaTypeHeaderValue("application/json")
        {
            CharSet = "UTF-8"
        };
        apiRequest.Headers.Add("language", "en");
        apiRequest.Headers.Add("accept", "*/*");
        apiRequest.Headers.Add("wizards", "FRONT_END");

        try
        {
            using var response = await httpClient.SendAsync(apiRequest, cancellationToken);

            var serviceResponse =
                await response.Content.ReadFromJsonAsync<OperationFeedbackResponse>(cancellationToken: cancellationToken);

            if (serviceResponse is null)
            {
                _logger.LogError(
                    "AMR service returned no content for operation feedback. Status: {StatusCode}",
                    response.StatusCode);

                return StatusCode(StatusCodes.Status502BadGateway, new OperationFeedbackResponse
                {
                    Code = "AMR_SERVICE_EMPTY_RESPONSE",
                    Message = "Failed to retrieve a response from the AMR service.",
                    Success = false
                });
            }

            _logger.LogInformation("✓ Operation feedback sent successfully for mission {MissionCode}", request.MissionCode);
            _logger.LogInformation("=== END OperationFeedbackController.SendOperationFeedbackAsync DEBUG ===");

            return StatusCode((int)response.StatusCode, serviceResponse);
        }
        catch (HttpRequestException httpRequestException)
        {
            _logger.LogError(
                httpRequestException,
                "Error while calling operation feedback endpoint at {Url}",
                _amrOptions.OperationFeedbackUrl);

            return StatusCode(StatusCodes.Status502BadGateway, new OperationFeedbackResponse
            {
                Code = "AMR_SERVICE_HTTP_ERROR",
                Message = $"HTTP error while contacting AMR service: {httpRequestException.Message}",
                Success = false
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error while sending operation feedback");

            return StatusCode(StatusCodes.Status500InternalServerError, new OperationFeedbackResponse
            {
                Code = "INTERNAL_SERVER_ERROR",
                Message = $"Unexpected error: {ex.Message}",
                Success = false
            });
        }
    }
}
