using System.Net.Http.Headers;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace QES_KUKA_AMR_WEB.Pages
{
    public class AreaModel : PageModel
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfiguration _configuration;
        private readonly ILogger<AreaModel> _logger;

        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNameCaseInsensitive = true
        };

        // Map Zone properties
        public IReadOnlyList<MapZoneRow> MapZones { get; private set; } = Array.Empty<MapZoneRow>();

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

        // Common properties
        public string? StatusMessage { get; private set; }
        public string StatusType { get; private set; } = "info";

        public AreaModel(
            IHttpClientFactory httpClientFactory,
            IConfiguration configuration,
            ILogger<AreaModel> logger)
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

            await LoadMapZonesAsync(token, cancellationToken);
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
                using var response = await client.PostAsync("api/mapzones/sync", content: null, cancellationToken);
                var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);

                if (response.IsSuccessStatusCode)
                {
                    var syncResult = JsonSerializer.Deserialize<MapZoneSyncResult>(responseContent, JsonOptions);
                    StatusMessage = syncResult is null
                        ? "Area synchronization completed."
                        : $"Synced {syncResult.Total} area(s). Inserted: {syncResult.Inserted}, Updated: {syncResult.Updated}.";
                    StatusType = "success";
                }
                else
                {
                    var errorMessage = ExtractErrorMessage(responseContent);
                    StatusMessage = errorMessage ?? $"Sync failed with status code {(int)response.StatusCode}.";
                    StatusType = "danger";
                    _logger.LogWarning("Area sync failed. StatusCode: {StatusCode}, Message: {Message}", response.StatusCode, StatusMessage);
                }
            }
            catch (Exception ex)
            {
                StatusMessage = "An error occurred while syncing areas. Please try again.";
                StatusType = "danger";
                _logger.LogError(ex, "Error occurred during area synchronization.");
            }

            await LoadMapZonesAsync(token, cancellationToken);
            return Page();
        }

        private async Task LoadMapZonesAsync(string token, CancellationToken cancellationToken)
        {
            try
            {
                using var client = CreateApiClient(token);
                using var response = await client.GetAsync("api/mapzones", cancellationToken);

                if (!response.IsSuccessStatusCode)
                {
                    var message = await response.Content.ReadAsStringAsync(cancellationToken);
                    StatusMessage = ExtractErrorMessage(message) ?? $"Failed to load areas. Status code {(int)response.StatusCode}.";
                    StatusType = "warning";
                    _logger.LogWarning("Failed to load map zones. StatusCode: {StatusCode}", response.StatusCode);
                    MapZones = Array.Empty<MapZoneRow>();
                    return;
                }

                var contentStream = await response.Content.ReadAsStreamAsync(cancellationToken);
                var mapZones = await JsonSerializer.DeserializeAsync<List<MapZoneRow>>(contentStream, JsonOptions, cancellationToken);
                var list = mapZones ?? new List<MapZoneRow>();

                var filtered = ApplyMapZoneFiltering(list, SearchTerm);
                var sorted = ApplyMapZoneSorting(filtered, SortColumn, SortDirection);

                TotalRecords = sorted.Count;
                TotalPages = (int)Math.Ceiling((double)TotalRecords / PageSize);

                if (CurrentPage < 1) CurrentPage = 1;
                if (CurrentPage > TotalPages && TotalPages > 0) CurrentPage = TotalPages;

                MapZones = sorted
                    .Skip((CurrentPage - 1) * PageSize)
                    .Take(PageSize)
                    .ToList();
            }
            catch (Exception ex)
            {
                StatusMessage = "Unable to load areas at this time. Please try again later.";
                StatusType = "danger";
                MapZones = Array.Empty<MapZoneRow>();
                _logger.LogError(ex, "Exception thrown while loading map zones.");
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

        private static List<MapZoneRow> ApplyMapZoneFiltering(
            IEnumerable<MapZoneRow> mapZones,
            string? searchTerm)
        {
            if (string.IsNullOrWhiteSpace(searchTerm))
            {
                return mapZones.ToList();
            }

            var term = searchTerm.Trim().ToLowerInvariant();

            return mapZones.Where(m =>
                m.ZoneName.ToLowerInvariant().Contains(term) ||
                m.ZoneCode.ToLowerInvariant().Contains(term) ||
                m.Layout.ToLowerInvariant().Contains(term)
            ).ToList();
        }

        private static IReadOnlyList<MapZoneRow> ApplyMapZoneSorting(
            IEnumerable<MapZoneRow> mapZones,
            string sortColumn,
            string sortDirection)
        {
            var normalizedColumn = (sortColumn ?? string.Empty).Trim().ToLowerInvariant();
            var normalizedDirection = string.Equals(sortDirection, "desc", StringComparison.OrdinalIgnoreCase)
                ? "desc"
                : "asc";

            Func<MapZoneRow, IComparable?> keySelector = normalizedColumn switch
            {
                "id" => m => m.Id,
                "zonename" => m => m.ZoneName,
                "zonecode" => m => m.ZoneCode,
                "layout" => m => m.Layout,
                "areapurpose" => m => m.AreaPurpose,
                "statustext" => m => m.StatusText,
                _ => m => m.Id
            };

            var ordered = normalizedDirection == "desc"
                ? mapZones.OrderByDescending(keySelector).ThenBy(m => m.Id)
                : mapZones.OrderBy(keySelector).ThenBy(m => m.Id);

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

        public class MapZoneRow
        {
            public int Id { get; set; }
            public string ZoneName { get; set; } = string.Empty;
            public string ZoneCode { get; set; } = string.Empty;
            public string Layout { get; set; } = string.Empty;
            public string AreaPurpose { get; set; } = string.Empty;
            public string StatusText { get; set; } = string.Empty;
        }

        private class MapZoneSyncResult
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
