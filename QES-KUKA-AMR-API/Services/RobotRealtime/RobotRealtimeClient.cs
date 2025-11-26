using System.Text.Json;
using System.Web;
using Microsoft.Extensions.Options;
using QES_KUKA_AMR_API.Models.MobileRobot;
using QES_KUKA_AMR_API.Options;
using QES_KUKA_AMR_API.Services.Auth;

namespace QES_KUKA_AMR_API.Services.RobotRealtime;

public class RobotRealtimeClient : IRobotRealtimeClient
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly MobileRobotServiceOptions _options;
    private readonly IExternalApiTokenService _tokenService;
    private readonly ILogger<RobotRealtimeClient> _logger;

    public RobotRealtimeClient(
        IHttpClientFactory httpClientFactory,
        IOptions<MobileRobotServiceOptions> options,
        IExternalApiTokenService tokenService,
        ILogger<RobotRealtimeClient> logger)
    {
        _httpClientFactory = httpClientFactory;
        _options = options.Value;
        _tokenService = tokenService;
        _logger = logger;
    }

    public async Task<RealtimeInfoData?> GetRealtimeInfoAsync(
        string? floorNumber = null,
        string? mapCode = null,
        bool isFirst = false,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_options.RealtimeInfoUrl))
        {
            _logger.LogWarning("MobileRobotServiceOptions.RealtimeInfoUrl is not configured.");
            return null;
        }

        try
        {
            // Build query string
            var queryParams = HttpUtility.ParseQueryString(string.Empty);
            if (!string.IsNullOrEmpty(floorNumber))
                queryParams["floorNumber"] = floorNumber;
            if (!string.IsNullOrEmpty(mapCode))
                queryParams["mapCode"] = mapCode;
            queryParams["isFirst"] = isFirst.ToString().ToLower();

            var url = _options.RealtimeInfoUrl;
            if (queryParams.Count > 0)
            {
                url = $"{url}?{queryParams}";
            }

            _logger.LogDebug("Fetching realtime info from: {Url}", url);

            var httpClient = _httpClientFactory.CreateClient();

            // Get external API token
            var token = await _tokenService.GetTokenAsync(cancellationToken);

            using var request = new HttpRequestMessage(HttpMethod.Get, url);
            request.Headers.TryAddWithoutValidation("language", "en");
            request.Headers.TryAddWithoutValidation("accept", "*/*");
            request.Headers.TryAddWithoutValidation("wizards", "FRONT_END");

            if (!string.IsNullOrEmpty(token))
            {
                request.Headers.TryAddWithoutValidation("Authorization", $"Bearer {token}");
            }

            using var response = await httpClient.SendAsync(request, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Realtime info request failed with status {StatusCode}", response.StatusCode);
                return null;
            }

            var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);
            _logger.LogDebug("Realtime info response: {Response}", responseBody);

            var result = JsonSerializer.Deserialize<RealtimeInfoResponse>(responseBody);

            if (result?.Success != true)
            {
                _logger.LogWarning("Realtime info request returned success=false: {Message}", result?.Message);
                return null;
            }

            return result.Data;
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            _logger.LogWarning(ex, "Error fetching realtime info from external API.");
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error when fetching realtime info.");
            return null;
        }
    }
}
