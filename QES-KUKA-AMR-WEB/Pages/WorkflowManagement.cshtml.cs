using System.Net.Http.Headers;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace QES_KUKA_AMR_WEB.Pages
{
    public class WorkflowManagementModel : PageModel
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfiguration _configuration;
        private readonly ILogger<WorkflowManagementModel> _logger;

        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNameCaseInsensitive = true
        };

        public IReadOnlyList<WorkflowRow> Workflows { get; private set; } = Array.Empty<WorkflowRow>();

        public string? StatusMessage { get; private set; }

        public string StatusType { get; private set; } = "info";

        [BindProperty(SupportsGet = true)]
        public string SortColumn { get; set; } = "Id";

        [BindProperty(SupportsGet = true)]
        public string SortDirection { get; set; } = "asc";

        [BindProperty(SupportsGet = true)]
        public int CurrentPage { get; set; } = 1;

        [BindProperty(SupportsGet = true)]
        public int PageSize { get; set; } = 20;

        [BindProperty(SupportsGet = true)]
        public string? SearchTerm { get; set; }

        public int TotalPages { get; private set; }

        public int TotalRecords { get; private set; }

        public int DisplayStart => Math.Min((CurrentPage - 1) * PageSize + 1, TotalRecords);

        public int DisplayEnd => Math.Min(CurrentPage * PageSize, TotalRecords);

        public WorkflowManagementModel(
            IHttpClientFactory httpClientFactory,
            IConfiguration configuration,
            ILogger<WorkflowManagementModel> logger)
        {
            _httpClientFactory = httpClientFactory;
            _configuration = configuration;
            _logger = logger;
        }

        public async Task<IActionResult> OnGetAsync(CancellationToken cancellationToken)
        {
            var token = HttpContext.Session.GetString("JwtToken");
            if (string.IsNullOrEmpty(token))
            {
                return RedirectToPage("/Login");
            }

            await LoadWorkflowsAsync(token, cancellationToken);
            return Page();
        }

        public async Task<IActionResult> OnPostSyncAsync(CancellationToken cancellationToken)
        {
            var token = HttpContext.Session.GetString("JwtToken");
            if (string.IsNullOrEmpty(token))
            {
                return RedirectToPage("/Login");
            }

            try
            {
                using var client = CreateApiClient(token);
                using var response = await client.PostAsync("api/workflows/sync", content: null, cancellationToken);
                var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);

                if (response.IsSuccessStatusCode)
                {
                    var syncResult = JsonSerializer.Deserialize<WorkflowSyncResult>(responseContent, JsonOptions);
                    StatusMessage = syncResult is null
                        ? "Workflow synchronization completed."
                        : $"Synced {syncResult.Total} workflow(s). Inserted: {syncResult.Inserted}, Updated: {syncResult.Updated}.";
                    StatusType = "success";
                }
                else
                {
                    var errorMessage = ExtractErrorMessage(responseContent);
                    StatusMessage = errorMessage ?? $"Sync failed with status code {(int)response.StatusCode}.";
                    StatusType = "danger";
                    _logger.LogWarning("Workflow sync failed. StatusCode: {StatusCode}, Message: {Message}", response.StatusCode, StatusMessage);
                }
            }
            catch (Exception ex)
            {
                StatusMessage = "An error occurred while syncing workflows. Please try again.";
                StatusType = "danger";
                _logger.LogError(ex, "Error occurred during workflow synchronization.");
            }

            await LoadWorkflowsAsync(token, cancellationToken);
            return Page();
        }

        private async Task LoadWorkflowsAsync(string token, CancellationToken cancellationToken)
        {
            try
            {
                using var client = CreateApiClient(token);
                using var response = await client.GetAsync("api/workflows", cancellationToken);

                if (!response.IsSuccessStatusCode)
                {
                    var message = await response.Content.ReadAsStringAsync(cancellationToken);
                    StatusMessage = ExtractErrorMessage(message) ?? $"Failed to load workflows. Status code {(int)response.StatusCode}.";
                    StatusType = "warning";
                    _logger.LogWarning("Failed to load workflows. StatusCode: {StatusCode}, Message: {Message}", response.StatusCode, StatusMessage);
                    Workflows = Array.Empty<WorkflowRow>();
                    return;
                }

                var contentStream = await response.Content.ReadAsStreamAsync(cancellationToken);
                var workflows = await JsonSerializer.DeserializeAsync<List<WorkflowRow>>(contentStream, JsonOptions, cancellationToken);
                var list = workflows ?? new List<WorkflowRow>();

                // Apply filtering
                var filtered = ApplyFiltering(list, SearchTerm);

                // Apply sorting
                var sorted = ApplySorting(filtered, SortColumn, SortDirection);

                // Calculate pagination
                TotalRecords = sorted.Count;
                TotalPages = (int)Math.Ceiling((double)TotalRecords / PageSize);

                // Ensure current page is valid
                if (CurrentPage < 1) CurrentPage = 1;
                if (CurrentPage > TotalPages && TotalPages > 0) CurrentPage = TotalPages;

                // Apply pagination
                Workflows = sorted
                    .Skip((CurrentPage - 1) * PageSize)
                    .Take(PageSize)
                    .ToList();
            }
            catch (Exception ex)
            {
                StatusMessage = "Unable to load workflows at this time. Please try again later.";
                StatusType = "danger";
                Workflows = Array.Empty<WorkflowRow>();
                _logger.LogError(ex, "Exception thrown while loading workflows.");
            }
        }

        private HttpClient CreateApiClient(string token)
        {
            var apiBaseUrl = _configuration["ApiSettings:BaseUrl"] ?? "https://localhost:7255";
            var client = _httpClientFactory.CreateClient();
            client.BaseAddress = new Uri(apiBaseUrl);
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            return client;
        }

        private static List<WorkflowRow> ApplyFiltering(
            IEnumerable<WorkflowRow> workflows,
            string? searchTerm)
        {
            if (string.IsNullOrWhiteSpace(searchTerm))
            {
                return workflows.ToList();
            }

            var term = searchTerm.Trim().ToLowerInvariant();

            return workflows.Where(w =>
                w.Name.ToLowerInvariant().Contains(term) ||
                w.Number.ToLowerInvariant().Contains(term) ||
                w.ExternalCode.ToLowerInvariant().Contains(term) ||
                w.LayoutCode.ToLowerInvariant().Contains(term)
            ).ToList();
        }

        private static IReadOnlyList<WorkflowRow> ApplySorting(
            IEnumerable<WorkflowRow> workflows,
            string sortColumn,
            string sortDirection)
        {
            var normalizedColumn = (sortColumn ?? string.Empty).Trim().ToLowerInvariant();
            var normalizedDirection = string.Equals(sortDirection, "desc", StringComparison.OrdinalIgnoreCase)
                ? "desc"
                : "asc";

            Func<WorkflowRow, IComparable?> keySelector = normalizedColumn switch
            {
                "id" => w => w.Id,
                "number" => w => w.Number,
                "externalcode" => w => w.ExternalCode,
                "status" => w => w.Status,
                "layoutcode" => w => w.LayoutCode,
                _ => w => w.Name
            };

            var ordered = normalizedDirection == "desc"
                ? workflows.OrderByDescending(keySelector).ThenBy(w => w.Id)
                : workflows.OrderBy(keySelector).ThenBy(w => w.Id);

            return ordered.ToList();
        }

        private static string? ExtractErrorMessage(string responseContent)
        {
            try
            {
                var error = JsonSerializer.Deserialize<ApiError>(responseContent, JsonOptions);
                return error?.Message ?? error?.Msg;
            }
            catch
            {
                return null;
            }
        }

        public class WorkflowRow
        {
            public int Id { get; set; }
            public string Name { get; set; } = string.Empty;
            public string Number { get; set; } = string.Empty;
            public string ExternalCode { get; set; } = string.Empty;
            public int Status { get; set; }
            public string LayoutCode { get; set; } = string.Empty;
        }

        private class WorkflowSyncResult
        {
            public int Total { get; set; }
            public int Inserted { get; set; }
            public int Updated { get; set; }
        }

        private class ApiError
        {
            public string? Message { get; set; }
            public string? Msg { get; set; }
        }
    }
}
