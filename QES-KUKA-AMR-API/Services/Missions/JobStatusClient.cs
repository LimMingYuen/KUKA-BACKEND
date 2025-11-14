using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using QES_KUKA_AMR_API.Models.Jobs;
using QES_KUKA_AMR_API.Options;

namespace QES_KUKA_AMR_API.Services.Missions;

public interface IJobStatusClient
{
    Task<IReadOnlyList<string>> GetRobotIdsAsync(CancellationToken cancellationToken = default);
}

public class JobStatusClient : IJobStatusClient
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly MissionServiceOptions _missionServiceOptions;
    private readonly ILogger<JobStatusClient> _logger;

    public JobStatusClient(
        IHttpClientFactory httpClientFactory,
        IOptions<MissionServiceOptions> missionServiceOptions,
        ILogger<JobStatusClient> logger)
    {
        _httpClientFactory = httpClientFactory;
        _missionServiceOptions = missionServiceOptions.Value;
        _logger = logger;
    }

    public async Task<IReadOnlyList<string>> GetRobotIdsAsync(CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_missionServiceOptions.JobQueryUrl))
        {
            _logger.LogWarning("MissionServiceOptions.JobQueryUrl is not configured.");
            return Array.Empty<string>();
        }

        var request = new JobQueryRequest
        {
            Limit = 50
        };

        // Log request details
        var requestBody = JsonSerializer.Serialize(request);
        _logger.LogInformation("Job Query Request - URL: {Url}, Body: {Body}",
            _missionServiceOptions.JobQueryUrl, requestBody);

        try
        {
            var httpClient = _httpClientFactory.CreateClient();
            using var httpRequest = new HttpRequestMessage(HttpMethod.Post, _missionServiceOptions.JobQueryUrl)
            {
                Content = JsonContent.Create(request)
            };

            httpRequest.Content.Headers.ContentType = new MediaTypeHeaderValue("application/json")
            {
                CharSet = "UTF-8"
            };

            httpRequest.Headers.TryAddWithoutValidation("language", "en");
            httpRequest.Headers.TryAddWithoutValidation("accept", "*/*");
            httpRequest.Headers.TryAddWithoutValidation("wizards", "FRONT_END");

            using var response = await httpClient.SendAsync(httpRequest, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Job status query returned {StatusCode}", response.StatusCode);
                return Array.Empty<string>();
            }

            var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);
            _logger.LogInformation("Job Query Response - Status: {StatusCode}, Body: {Body}",
                response.StatusCode, responseBody);

            var payload = JsonSerializer.Deserialize<JobQueryResponse>(responseBody);
            if (payload?.Data == null || payload.Data.Count == 0)
            {
                return Array.Empty<string>();
            }

            return payload.Data
                .Where(job => !string.IsNullOrWhiteSpace(job.RobotId))
                .Select(job => job.RobotId!.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(robotId => robotId, StringComparer.OrdinalIgnoreCase)
                .ToList();
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            _logger.LogWarning(ex, "Error querying job status service for robot IDs.");
            return Array.Empty<string>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error when querying job status service.");
            return Array.Empty<string>();
        }
    }
}
