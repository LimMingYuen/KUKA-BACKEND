using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace QES_KUKA_AMR_WEB.Pages;

public class MissionHistoryModel : PageModel
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IConfiguration _configuration;
    private readonly ILogger<MissionHistoryModel> _logger;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    public List<MissionHistoryItem> Missions { get; private set; } = new();

    public string? StatusMessage { get; private set; }
    public string StatusType { get; private set; } = "info";

    public int TotalRecords { get; private set; }
    public int MaxRecords { get; private set; } = 5000;

    // Sorting and Pagination properties
    [BindProperty(SupportsGet = true)]
    public string SortColumn { get; set; } = "CreatedDate";

    [BindProperty(SupportsGet = true)]
    public string SortDirection { get; set; } = "asc";

    [BindProperty(SupportsGet = true)]
    public int CurrentPage { get; set; } = 1;

    [BindProperty(SupportsGet = true)]
    public int PageSize { get; set; } = 20;

    [BindProperty(SupportsGet = true)]
    public string? SearchTerm { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? StatusFilter { get; set; }

    public int TotalPages => PageSize > 0 ? (int)Math.Ceiling(TotalRecords / (double)PageSize) : 1;
    public int DisplayStart => TotalRecords == 0 ? 0 : ((CurrentPage - 1) * PageSize) + 1;
    public int DisplayEnd => Math.Min(CurrentPage * PageSize, TotalRecords);

    public MissionHistoryModel(
        IHttpClientFactory httpClientFactory,
        IConfiguration configuration,
        ILogger<MissionHistoryModel> logger)
    {
        _httpClientFactory = httpClientFactory;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<IActionResult> OnGetAsync(CancellationToken cancellationToken)
    {
        if (!await LoadMissionHistoryAsync(cancellationToken))
        {
            return RedirectToPage("/Login");
        }

        return Page();
    }

    public async Task<IActionResult> OnPostClearAsync(CancellationToken cancellationToken)
    {
        var token = HttpContext.Session.GetString("JwtToken");
        if (string.IsNullOrEmpty(token))
        {
            return RedirectToPage("/Login");
        }

        try
        {
            using var client = CreateApiClient(token);
            using var response = await client.DeleteAsync("api/mission-history/clear", cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                StatusMessage = "Mission history cleared successfully.";
                StatusType = "success";

                // Reset to first page since data was cleared
                CurrentPage = 1;
            }
            else
            {
                StatusMessage = "Failed to clear mission history.";
                StatusType = "danger";
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error clearing mission history");
            StatusMessage = "Error clearing mission history.";
            StatusType = "danger";
        }

        await LoadMissionHistoryAsync(cancellationToken);
        return Page();
    }

    private async Task<bool> LoadMissionHistoryAsync(CancellationToken cancellationToken)
    {
        var token = HttpContext.Session.GetString("JwtToken");
        if (string.IsNullOrEmpty(token))
        {
            StatusMessage = "Please login to view mission history.";
            StatusType = "warning";
            return false;
        }

        try
        {
            using var client = CreateApiClient(token);

            // Get mission history
            using var historyResponse = await client.GetAsync("api/mission-history", cancellationToken);

            if (historyResponse.StatusCode == System.Net.HttpStatusCode.Unauthorized)
            {
                StatusMessage = "Session expired. Please login again.";
                StatusType = "warning";
                return false;
            }

            historyResponse.EnsureSuccessStatusCode();

            await using var historyStream = await historyResponse.Content.ReadAsStreamAsync(cancellationToken);
            var allMissions = await JsonSerializer.DeserializeAsync<List<MissionHistoryItem>>(historyStream, JsonOptions, cancellationToken);
            allMissions ??= new List<MissionHistoryItem>();

            // Apply search filter
            var filteredMissions = allMissions.AsEnumerable();

            if (!string.IsNullOrWhiteSpace(SearchTerm))
            {
                var searchLower = SearchTerm.ToLower();
                filteredMissions = filteredMissions.Where(m =>
                    m.MissionCode.ToLower().Contains(searchLower) ||
                    m.RequestId.ToLower().Contains(searchLower) ||
                    m.WorkflowName.ToLower().Contains(searchLower));
            }

            // Apply status filter
            if (!string.IsNullOrWhiteSpace(StatusFilter))
            {
                filteredMissions = filteredMissions.Where(m =>
                    m.Status.Equals(StatusFilter, StringComparison.OrdinalIgnoreCase));
            }

            // Apply sorting
            filteredMissions = SortColumn.ToLower() switch
            {
                "id" => SortDirection == "asc"
                    ? filteredMissions.OrderBy(m => m.Id)
                    : filteredMissions.OrderByDescending(m => m.Id),
                "missioncode" => SortDirection == "asc"
                    ? filteredMissions.OrderBy(m => m.MissionCode)
                    : filteredMissions.OrderByDescending(m => m.MissionCode),
                "requestid" => SortDirection == "asc"
                    ? filteredMissions.OrderBy(m => m.RequestId)
                    : filteredMissions.OrderByDescending(m => m.RequestId),
                "workflowname" => SortDirection == "asc"
                    ? filteredMissions.OrderBy(m => m.WorkflowName)
                    : filteredMissions.OrderByDescending(m => m.WorkflowName),
                "status" => SortDirection == "asc"
                    ? filteredMissions.OrderBy(m => m.Status)
                    : filteredMissions.OrderByDescending(m => m.Status),
                "createddate" => SortDirection == "asc"
                    ? filteredMissions.OrderBy(m => m.CreatedDate)
                    : filteredMissions.OrderByDescending(m => m.CreatedDate),
                _ => filteredMissions.OrderByDescending(m => m.CreatedDate)
            };

            // Get total count before pagination
            TotalRecords = filteredMissions.Count();

            // Apply pagination
            Missions = filteredMissions
                .Skip((CurrentPage - 1) * PageSize)
                .Take(PageSize)
                .ToList();

            // Get max records limit
            using var countResponse = await client.GetAsync("api/mission-history/count", cancellationToken);
            if (countResponse.IsSuccessStatusCode)
            {
                await using var countStream = await countResponse.Content.ReadAsStreamAsync(cancellationToken);
                var countResult = await JsonSerializer.DeserializeAsync<CountResponse>(countStream, JsonOptions, cancellationToken);
                MaxRecords = countResult?.MaxRecords ?? 5000;
            }
        }
        catch (HttpRequestException httpEx)
        {
            _logger.LogError(httpEx, "Unable to load mission history.");
            StatusMessage = "Unable to load mission history from the API.";
            StatusType = "danger";
            Missions = new List<MissionHistoryItem>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error loading mission history.");
            StatusMessage = "Unexpected error while loading mission history.";
            StatusType = "danger";
            Missions = new List<MissionHistoryItem>();
        }

        return true;
    }

    private HttpClient CreateApiClient(string token)
    {
        var apiBaseUrl = _configuration["ApiSettings:BaseUrl"] ?? "https://localhost:7255";
        var client = _httpClientFactory.CreateClient();
        client.BaseAddress = new Uri(apiBaseUrl);
        client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
        return client;
    }
}

public class MissionHistoryItem
{
    public int Id { get; set; }
    public string MissionCode { get; set; } = string.Empty;
    public string RequestId { get; set; } = string.Empty;
    public string? WorkflowName { get; set; }
    public string? MissionType { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime CreatedDate { get; set; }
}

public class CountResponse
{
    public int Count { get; set; }
    public int MaxRecords { get; set; }
}
