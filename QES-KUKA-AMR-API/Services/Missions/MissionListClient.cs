using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using QES_KUKA_AMR_API.Models.Missions;
using QES_KUKA_AMR_API.Options;

namespace QES_KUKA_AMR_API.Services.Missions;

public interface IMissionListClient
{
    Task<MissionListApiResponse?> GetMissionListAsync(MissionListRequest request, string? jwtToken = null, CancellationToken cancellationToken = default);
}

public class MissionListClient : IMissionListClient
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly MissionListServiceOptions _missionListServiceOptions;
    private readonly ILogger<MissionListClient> _logger;

    public MissionListClient(
        IHttpClientFactory httpClientFactory,
        IOptions<MissionListServiceOptions> missionListServiceOptions,
        ILogger<MissionListClient> logger)
    {
        _httpClientFactory = httpClientFactory;
        _missionListServiceOptions = missionListServiceOptions.Value;
        _logger = logger;
    }

    public async Task<MissionListApiResponse?> GetMissionListAsync(
        MissionListRequest request,
        string? jwtToken = null,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_missionListServiceOptions.MissionListUrl))
        {
            _logger.LogWarning("MissionListServiceOptions.MissionListUrl is not configured.");
            return null;
        }

        // Log request details
        var requestBody = JsonSerializer.Serialize(request);
        _logger.LogInformation("Mission List Request - URL: {Url}, Body: {Body}, HasJwtToken: {HasToken}",
            _missionListServiceOptions.MissionListUrl, requestBody, !string.IsNullOrWhiteSpace(jwtToken));

        // Clean the JWT token - strip "Bearer " prefix if present
        var cleanedToken = jwtToken;
        if (!string.IsNullOrWhiteSpace(jwtToken))
        {
            _logger.LogInformation("JWT token provided to MissionListClient. Length: {Length}, Preview: {Preview}...",
                jwtToken.Length, jwtToken.Length > 20 ? jwtToken.Substring(0, 20) : jwtToken);

            // Strip "Bearer " prefix if present to avoid double-prefix
            if (jwtToken.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
            {
                cleanedToken = jwtToken.Substring("Bearer ".Length).Trim();
                _logger.LogInformation("Stripped 'Bearer ' prefix from token. New length: {Length}", cleanedToken.Length);
            }
        }
        else
        {
            _logger.LogWarning("No JWT token provided to MissionListClient - request will fail with 401");
        }

        try
        {
            var httpClient = _httpClientFactory.CreateClient();
            using var httpRequest = new HttpRequestMessage(HttpMethod.Post, _missionListServiceOptions.MissionListUrl)
            {
                Content = JsonContent.Create(request)
            };

            httpRequest.Content.Headers.ContentType = new MediaTypeHeaderValue("application/json")
            {
                CharSet = "UTF-8"
            };

            // Add JWT token if provided
            if (!string.IsNullOrWhiteSpace(cleanedToken))
            {
                httpRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", cleanedToken);
                _logger.LogInformation("Authorization header added to mission list request");
            }

            // Add custom headers required by AMR system
            httpRequest.Headers.TryAddWithoutValidation("language", "en");
            httpRequest.Headers.TryAddWithoutValidation("accept", "*/*");
            httpRequest.Headers.TryAddWithoutValidation("wizards", "FRONT_END");

            using var response = await httpClient.SendAsync(httpRequest, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                {
                    _logger.LogWarning("Mission list query returned 401 Unauthorized. JWT token may be missing or invalid.");
                }
                else
                {
                    _logger.LogWarning("Mission list query returned {StatusCode}", response.StatusCode);
                }
                return null;
            }

            var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);
            _logger.LogInformation("Mission List Response - Status: {StatusCode}, Body: {Body}",
                response.StatusCode, responseBody);

            var payload = JsonSerializer.Deserialize<MissionListApiResponse>(responseBody,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            if (payload == null || !payload.Success || payload.Data == null)
            {
                _logger.LogWarning("Mission list query returned unsuccessful or empty response");
                return null;
            }

            return payload;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogWarning(ex, "HTTP error querying mission list service.");
            return null;
        }
        catch (TaskCanceledException ex)
        {
            _logger.LogWarning(ex, "Mission list query timed out.");
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error when querying mission list service.");
            return null;
        }
    }
}
