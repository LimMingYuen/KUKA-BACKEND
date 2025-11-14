using System.Text.Json;
using System.Linq;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.WebUtilities;

namespace QES_KUKA_AMR_WEB.Pages.Analytics;

[IgnoreAntiforgeryToken]
public class RobotUtilizationModel : PageModel
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IConfiguration _configuration;
    private readonly ILogger<RobotUtilizationModel> _logger;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public RobotUtilizationModel(
        IHttpClientFactory httpClientFactory,
        IConfiguration configuration,
        ILogger<RobotUtilizationModel> logger)
    {
        _httpClientFactory = httpClientFactory;
        _configuration = configuration;
        _logger = logger;
    }

    public IActionResult OnGet()
    {
        var token = HttpContext.Session.GetString("JwtToken");
        if (string.IsNullOrEmpty(token))
        {
            return RedirectToPage("/Login");
        }

        return Page();
    }

    public async Task<IActionResult> OnGetRobotsAsync(CancellationToken cancellationToken)
    {
        var token = HttpContext.Session.GetString("JwtToken");
        if (string.IsNullOrEmpty(token))
        {
            return new JsonResult(new { success = false, message = "Session expired" }) { StatusCode = 401 };
        }

        try
        {
            using var client = CreateApiClient(token);
            var request = new JobQueryRequest { Limit = 50 };

            using var response = await client.PostAsJsonAsync("api/missions/jobs/query", request, cancellationToken);
            var rawContent = await response.Content.ReadAsStringAsync(cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Failed to fetch robot list from job query. Status: {StatusCode}", response.StatusCode);
                return new JsonResult(new { success = false, message = "Unable to retrieve robot list." }) { StatusCode = (int)response.StatusCode };
            }

            var payload = JsonSerializer.Deserialize<JobQueryResponse>(rawContent, JsonOptions);
            var robotIds = payload?.Data?
                .Where(job => !string.IsNullOrWhiteSpace(job.RobotId))
                .Select(job => job.RobotId!.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(id => id, StringComparer.OrdinalIgnoreCase)
                .ToList() ?? new List<string>();

            return new JsonResult(new { success = true, data = robotIds });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving robot list from job status endpoint.");
            return StatusCode(500, new { success = false, message = "Error retrieving robot list." });
        }
    }

    public async Task<IActionResult> OnGetRobotListAsync()
    {
        var token = HttpContext.Session.GetString("JwtToken");
        if (string.IsNullOrEmpty(token))
        {
            return new JsonResult(new { error = "Unauthorized" }) { StatusCode = 401 };
        }

        try
        {
            var httpClient = _httpClientFactory.CreateClient();
            var apiBaseUrl = _configuration["ApiSettings:BaseUrl"] ?? "http://localhost:5109";
            var apiUrl = $"{apiBaseUrl}/api/mobilerobot";

            httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

            var response = await httpClient.GetAsync(apiUrl);

            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadAsStringAsync();
                return new JsonResult(JsonSerializer.Deserialize<JsonElement>(json));
            }

            return new JsonResult(new { error = "Failed to load robots" }) { StatusCode = (int)response.StatusCode };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting robot list");
            return new JsonResult(new { error = ex.Message }) { StatusCode = 500 };
        }
    }

    public async Task<IActionResult> OnGetUtilizationAsync(
        [FromQuery] string? robotId,
        [FromQuery] DateTime? start,
        [FromQuery] DateTime? end,
        [FromQuery] string? groupBy,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("OnGetUtilizationAsync called - robotId: {RobotId}, start: {Start}, end: {End}, groupBy: {GroupBy}",
            robotId, start, end, groupBy);

        // Extract JWT token from session for charging data API calls
        var token = HttpContext.Session.GetString("JwtToken");
        if (string.IsNullOrEmpty(token))
        {
            _logger.LogWarning("No JWT token found in session. Charging data will not be available.");
        }

        try
        {
            var apiBaseUrl = _configuration["ApiSettings:BaseUrl"] ?? "http://localhost:5109";
            _logger.LogInformation("Using API base URL: {ApiBaseUrl}", apiBaseUrl);

            var client = _httpClientFactory.CreateClient();
            client.BaseAddress = new Uri(apiBaseUrl);

            // Add JWT token for charging data authentication
            if (!string.IsNullOrEmpty(token))
            {
                client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
            }

            var query = new Dictionary<string, string?>();
            if (!string.IsNullOrWhiteSpace(robotId))
            {
                query["robotId"] = robotId;
            }
            if (start.HasValue)
            {
                query["start"] = start.Value.ToUniversalTime().ToString("o");
            }
            if (end.HasValue)
            {
                query["end"] = end.Value.ToUniversalTime().ToString("o");
            }
            if (!string.IsNullOrWhiteSpace(groupBy))
            {
                query["groupBy"] = groupBy;
            }

            var url = "api/v1/robots/utilization";
            if (query.Count > 0)
            {
                url = QueryHelpers.AddQueryString(url, query!);
            }

            _logger.LogInformation("Calling API endpoint: {Url}", url);
            using var response = await client.GetAsync(url, cancellationToken);
            var payload = await response.Content.ReadAsStringAsync(cancellationToken);

            _logger.LogInformation("API response status: {StatusCode}, Content length: {ContentLength}",
                response.StatusCode, payload.Length);

            return new ContentResult
            {
                StatusCode = (int)response.StatusCode,
                ContentType = "application/json",
                Content = payload
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving robot utilization analytics.");
            return new JsonResult(new { success = false, message = "Error retrieving robot utilization." })
            {
                StatusCode = 500
            };
        }
    }

    private HttpClient CreateApiClient(string token)
    {
        var apiBaseUrl = _configuration["ApiSettings:BaseUrl"] ?? "https://localhost:7255";
        var client = _httpClientFactory.CreateClient();
        client.BaseAddress = new Uri(apiBaseUrl);
        client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
        return client;
    }

    private sealed class JobQueryRequest
    {
        public int? Limit { get; set; }
    }

    private sealed class JobDto
    {
        public string? RobotId { get; set; }
    }

    private sealed class JobQueryResponse
    {
        public List<JobDto>? Data { get; set; }
    }
}
