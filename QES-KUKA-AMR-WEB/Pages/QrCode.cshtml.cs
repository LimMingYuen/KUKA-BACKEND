using System.Net.Http.Headers;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace QES_KUKA_AMR_WEB.Pages
{
    public class QrCodeModel : PageModel
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfiguration _configuration;
        private readonly ILogger<QrCodeModel> _logger;

        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNameCaseInsensitive = true
        };

        // QR Code properties
        public IReadOnlyList<QrCodeRow> QrCodes { get; private set; } = Array.Empty<QrCodeRow>();

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

        public QrCodeModel(
            IHttpClientFactory httpClientFactory,
            IConfiguration configuration,
            ILogger<QrCodeModel> logger)
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

            await LoadQrCodesAsync(token, cancellationToken);
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
                using var response = await client.PostAsync("api/qrcodes/sync", content: null, cancellationToken);
                var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);

                if (response.IsSuccessStatusCode)
                {
                    var syncResult = JsonSerializer.Deserialize<QrCodeSyncResult>(responseContent, JsonOptions);
                    StatusMessage = syncResult is null
                        ? "QR Code synchronization completed."
                        : $"Synced {syncResult.Total} QR code(s). Inserted: {syncResult.Inserted}, Updated: {syncResult.Updated}.";
                    StatusType = "success";
                }
                else
                {
                    var errorMessage = ExtractErrorMessage(responseContent);
                    StatusMessage = errorMessage ?? $"Sync failed with status code {(int)response.StatusCode}.";
                    StatusType = "danger";
                    _logger.LogWarning("QR Code sync failed. StatusCode: {StatusCode}, Message: {Message}", response.StatusCode, StatusMessage);
                }
            }
            catch (Exception ex)
            {
                StatusMessage = "An error occurred while syncing QR codes. Please try again.";
                StatusType = "danger";
                _logger.LogError(ex, "Error occurred during QR code synchronization.");
            }

            await LoadQrCodesAsync(token, cancellationToken);
            return Page();
        }

        private async Task LoadQrCodesAsync(string token, CancellationToken cancellationToken)
        {
            try
            {
                using var client = CreateApiClient(token);
                using var response = await client.GetAsync("api/qrcodes", cancellationToken);

                if (!response.IsSuccessStatusCode)
                {
                    var message = await response.Content.ReadAsStringAsync(cancellationToken);
                    StatusMessage = ExtractErrorMessage(message) ?? $"Failed to load QR codes. Status code {(int)response.StatusCode}.";
                    StatusType = "warning";
                    _logger.LogWarning("Failed to load QR codes. StatusCode: {StatusCode}", response.StatusCode);
                    QrCodes = Array.Empty<QrCodeRow>();
                    return;
                }

                var contentStream = await response.Content.ReadAsStreamAsync(cancellationToken);
                var qrCodes = await JsonSerializer.DeserializeAsync<List<QrCodeRow>>(contentStream, JsonOptions, cancellationToken);
                var list = qrCodes ?? new List<QrCodeRow>();

                var filtered = ApplyQrCodeFiltering(list, SearchTerm);
                var sorted = ApplyQrCodeSorting(filtered, SortColumn, SortDirection);

                TotalRecords = sorted.Count;
                TotalPages = (int)Math.Ceiling((double)TotalRecords / PageSize);

                if (CurrentPage < 1) CurrentPage = 1;
                if (CurrentPage > TotalPages && TotalPages > 0) CurrentPage = TotalPages;

                QrCodes = sorted
                    .Skip((CurrentPage - 1) * PageSize)
                    .Take(PageSize)
                    .ToList();
            }
            catch (Exception ex)
            {
                StatusMessage = "Unable to load QR codes at this time. Please try again later.";
                StatusType = "danger";
                QrCodes = Array.Empty<QrCodeRow>();
                _logger.LogError(ex, "Exception thrown while loading QR codes.");
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

        private static List<QrCodeRow> ApplyQrCodeFiltering(
            IEnumerable<QrCodeRow> qrCodes,
            string? searchTerm)
        {
            if (string.IsNullOrWhiteSpace(searchTerm))
            {
                return qrCodes.ToList();
            }

            var term = searchTerm.Trim().ToLowerInvariant();

            return qrCodes.Where(q =>
                q.NodeLabel.ToLowerInvariant().Contains(term) ||
                q.MapCode.ToLowerInvariant().Contains(term) ||
                q.FloorNumber.ToLowerInvariant().Contains(term)
            ).ToList();
        }

        private static IReadOnlyList<QrCodeRow> ApplyQrCodeSorting(
            IEnumerable<QrCodeRow> qrCodes,
            string sortColumn,
            string sortDirection)
        {
            var normalizedColumn = (sortColumn ?? string.Empty).Trim().ToLowerInvariant();
            var normalizedDirection = string.Equals(sortDirection, "desc", StringComparison.OrdinalIgnoreCase)
                ? "desc"
                : "asc";

            Func<QrCodeRow, IComparable?> keySelector = normalizedColumn switch
            {
                "id" => q => q.Id,
                "nodelabel" => q => q.NodeLabel,
                "mapcode" => q => q.MapCode,
                "floornumber" => q => q.FloorNumber,
                "nodenumber" => q => q.NodeNumber,
                "reliability" => q => q.Reliability,
                "reporttimes" => q => q.ReportTimes,
                "lastupdatetime" => q => q.LastUpdateTime,
                _ => q => q.Id
            };

            var ordered = normalizedDirection == "desc"
                ? qrCodes.OrderByDescending(keySelector).ThenBy(q => q.Id)
                : qrCodes.OrderBy(keySelector).ThenBy(q => q.Id);

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

        public class QrCodeRow
        {
            public int Id { get; set; }
            public string NodeLabel { get; set; } = string.Empty;
            public string MapCode { get; set; } = string.Empty;
            public string FloorNumber { get; set; } = string.Empty;
            public int NodeNumber { get; set; }
            public int Reliability { get; set; }
            public int ReportTimes { get; set; }
            public string LastUpdateTime { get; set; } = string.Empty;
        }

        private class QrCodeSyncResult
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
