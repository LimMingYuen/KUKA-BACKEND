using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Net.Http.Headers;
using System.Text.Json;

namespace QES_KUKA_AMR_WEB.Pages
{
    public class UserListModel : PageModel
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfiguration _configuration;
        private readonly ILogger<UserListModel> _logger;

        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNameCaseInsensitive = true
        };

        public IReadOnlyList<UserRow> Users { get; private set; } = Array.Empty<UserRow>();

        [BindProperty(SupportsGet = true)]
        public string SortColumn { get; set; } = "Username";

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

        public string? StatusMessage { get; private set; }
        public string StatusType { get; private set; } = "info";

        public UserListModel(
            IHttpClientFactory httpClientFactory,
            IConfiguration configuration,
            ILogger<UserListModel> logger)
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

            await LoadUsersAsync(token, cancellationToken);
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
                using var response = await client.PostAsync("api/User/sync", content: null, cancellationToken);
                var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);

                if (response.IsSuccessStatusCode)
                {
                    var syncResult = JsonSerializer.Deserialize<UserSyncResult>(responseContent, JsonOptions);
                    StatusMessage = syncResult is null
                        ? "Mobile Robot synchronization completed."
                        : $"Synced {syncResult.Total} User(s). Inserted: {syncResult.Inserted}, Updated: {syncResult.Updated}.";
                    StatusType = "success";
                }
                else
                {
                    var errorMessage = ExtractErrorMessage(responseContent);
                    StatusMessage = errorMessage ?? $"Sync failed with status code {(int)response.StatusCode}.";
                    StatusType = "danger";
                    _logger.LogWarning("User sync failed. StatusCode: {StatusCode}, Message: {Message}", response.StatusCode, StatusMessage);
                }
            }
            catch (Exception ex)
            {
                StatusMessage = "An error occurred while syncing Users. Please try again.";
                StatusType = "danger";
                _logger.LogError(ex, "Error occurred during User synchronization.");
            }

            await LoadUsersAsync(token, cancellationToken);
            return Page();
        }

        private async Task LoadUsersAsync(string token, CancellationToken cancellationToken)
        {
            try
            {
                using var client = CreateApiClient(token);
                using var response = await client.GetAsync("api/User", cancellationToken);

                if (!response.IsSuccessStatusCode)
                {
                    var message = await response.Content.ReadAsStringAsync(cancellationToken);
                    StatusMessage = ExtractErrorMessage(message) ?? $"Failed to load User list. Status code {(int)response.StatusCode}.";
                    StatusType = "warning";
                    Users = Array.Empty<UserRow>();
                    return;
                }

                var contentStream = await response.Content.ReadAsStreamAsync(cancellationToken);
                var users = await JsonSerializer.DeserializeAsync<List<UserRow>>(contentStream, JsonOptions, cancellationToken);
                var list = users ?? new List<UserRow>();

                var filtered = ApplyUserFiltering(list, SearchTerm);
                var sorted = ApplyUserSorting(filtered, SortColumn, SortDirection);

                TotalRecords = sorted.Count;
                TotalPages = (int)Math.Ceiling((double)TotalRecords / PageSize);

                if (CurrentPage < 1) CurrentPage = 1;
                if (CurrentPage > TotalPages && TotalPages > 0) CurrentPage = TotalPages;

                Users = sorted
                    .Skip((CurrentPage - 1) * PageSize)
                    .Take(PageSize)
                    .ToList();
            }
            catch (Exception ex)
            {
                StatusMessage = "Unable to load user role list at this time. Please try again later.";
                StatusType = "danger";
                Users = Array.Empty<UserRow>();
                _logger.LogError(ex, "Error occurred while loading user role list.");
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

        private static List<UserRow> ApplyUserFiltering(IEnumerable<UserRow> users, string? searchTerm)
        {
            if (string.IsNullOrWhiteSpace(searchTerm))
                return users.ToList();

            var term = searchTerm.Trim().ToLowerInvariant();

            return users.Where(u =>
                u.Username.ToLowerInvariant().Contains(term) ||
                (u.RoleName != null && u.RoleName.Any(role => role.ToLowerInvariant().Contains(term))) ||
                u.LastUpdateTime.ToLowerInvariant().Contains(term)
            ).ToList();
        }

        private static IReadOnlyList<UserRow> ApplyUserSorting(IEnumerable<UserRow> users, string sortColumn, string sortDirection)
        {
            var normalizedColumn = (sortColumn ?? string.Empty).Trim().ToLowerInvariant();
            var isDesc = string.Equals(sortDirection, "desc", StringComparison.OrdinalIgnoreCase);

            Func<UserRow, IComparable?> keySelector = normalizedColumn switch
            {
                "username" => u => u.Username,
                "role" => u => u.RoleName != null && u.RoleName.Any()
                            ? string.Join(", ", u.RoleName) 
                            : string.Empty,
                "lastupdatetime" => u => u.LastUpdateTime,
                _ => u => u.Username
            };

            return isDesc
                ? users.OrderByDescending(keySelector).ThenBy(u => u.Username).ToList()
                : users.OrderBy(keySelector).ThenBy(u => u.Username).ToList();
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

        public class UserRow
        {
            public string Username { get; set; } = string.Empty;
            public List<string> RoleName{ get; set; } = new List<string>();
            public string LastUpdateTime { get; set; } = string.Empty;
        }

        public class UserSyncResult
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
