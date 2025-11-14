using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace QES_KUKA_AMR_WEB.Pages
{
    public class RolePermissionConfigModel : PageModel
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfiguration _configuration;
        private readonly ILogger<RolePermissionConfigModel> _logger;

        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNameCaseInsensitive = true
        };
        public IReadOnlyList<Role> Roles { get; private set; } = Array.Empty<Role>();
        public IReadOnlyList<PagePermission> Pages { get; private set; } = Array.Empty<PagePermission>();
        public IReadOnlyList<RolePermissionRow> RolePermissions { get; private set; } = Array.Empty<RolePermissionRow>();

        // --- Pagination / Sorting / Filter ---
        [BindProperty(SupportsGet = true)]
        public string SortColumn { get; set; } = "Role";

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

        [BindProperty]
        public NewPermissionModel NewPermission { get; set; } = new();

        [BindProperty]
        public UpdatePermissionModel EditPermission { get; set; } = new();

        [BindProperty]
        public int DeleteRoleId { get; set; }

        public RolePermissionConfigModel(
            IHttpClientFactory httpClientFactory,
            IConfiguration configuration,
            ILogger<RolePermissionConfigModel> logger)
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

            await LoadRolesAsync(token, cancellationToken);
            await LoadPagesAsync(token, cancellationToken);

            await LoadPagePermissionsAsync(token, cancellationToken);
            return Page();
        }

        public async Task<IActionResult> OnPostAddAsync(CancellationToken cancellationToken)
        {
            var token = HttpContext.Session.GetString("JwtToken");
            if (string.IsNullOrEmpty(token))
                return RedirectToPage("/Login");

            await LoadRolesAsync(token, cancellationToken);
            await LoadPagesAsync(token, cancellationToken);

            if (NewPermission.RoleId <= 0)
            {
                StatusMessage = "Please select a role.";
                StatusType = "warning";
                return Page();
            }

            if (NewPermission.PageIds == null || !NewPermission.PageIds.Any())
            {
                StatusMessage = "Please select at least one page.";
                StatusType = "warning";
                return Page();
            }

            try
            {
                using var client = CreateApiClient(token);

                var json = JsonSerializer.Serialize(NewPermission);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await client.PostAsync("api/RolePermission/add", content, cancellationToken);
                var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);

                if (response.IsSuccessStatusCode)
                {
                    StatusMessage = responseBody.Contains("updated", StringComparison.OrdinalIgnoreCase)
                        ? "Permissions updated successfully."
                        : "Permissions added successfully.";
                    StatusType = "success";
                }
                else
                {
                    StatusMessage = ExtractErrorMessage(responseBody) ?? "Failed to add permission.";
                    StatusType = "danger";
                    return Page();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding role permission");
                StatusMessage = "An unexpected error occurred.";
                StatusType = "danger";
            }

            await LoadPagePermissionsAsync(token, cancellationToken);
            return Page();
        }

        public async Task<IActionResult> OnPostUpdateAsync(CancellationToken cancellationToken)
        {
            var token = HttpContext.Session.GetString("JwtToken");
            if (string.IsNullOrEmpty(token))
                return RedirectToPage("/Login");

            await LoadRolesAsync(token, cancellationToken);
            await LoadPagesAsync(token, cancellationToken);

            if (EditPermission.RoleId <= 0)
            {
                StatusMessage = "Invalid role ID.";
                StatusType = "warning";
                await LoadPagePermissionsAsync(token, cancellationToken);
                return Page();
            }

            if (EditPermission.PageIds == null || !EditPermission.PageIds.Any())
            {
                StatusMessage = "Please select at least one page.";
                StatusType = "warning";
                await LoadPagePermissionsAsync(token, cancellationToken);
                return Page();
            }

            try
            {
                using var client = CreateApiClient(token);

                var json = JsonSerializer.Serialize(new
                {
                    RoleId = EditPermission.RoleId,
                    PageIds = EditPermission.PageIds
                });
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await client.PostAsync("api/RolePermission/add", content, cancellationToken);
                var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);

                if (response.IsSuccessStatusCode)
                {
                    StatusMessage = "Permissions updated successfully.";
                    StatusType = "success";
                }
                else
                {
                    StatusMessage = ExtractErrorMessage(responseBody) ?? "Failed to update permission.";
                    StatusType = "danger";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating role permission");
                StatusMessage = "An unexpected error occurred.";
                StatusType = "danger";
            }

            await LoadPagePermissionsAsync(token, cancellationToken);
            return Page();
        }

        public async Task<IActionResult> OnPostDeleteAsync(CancellationToken cancellationToken)
        {
            var token = HttpContext.Session.GetString("JwtToken");
            if (string.IsNullOrEmpty(token))
                return RedirectToPage("/Login");

            await LoadRolesAsync(token, cancellationToken);
            await LoadPagesAsync(token, cancellationToken);

            if (DeleteRoleId <= 0)
            {
                StatusMessage = "Invalid role ID.";
                StatusType = "warning";
                await LoadPagePermissionsAsync(token, cancellationToken);
                return Page();
            }

            try
            {
                using var client = CreateApiClient(token);
                var response = await client.DeleteAsync($"api/RolePermission/{DeleteRoleId}", cancellationToken);
                var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);

                if (response.IsSuccessStatusCode)
                {
                    StatusMessage = "Permissions deleted successfully.";
                    StatusType = "success";
                }
                else
                {
                    StatusMessage = ExtractErrorMessage(responseBody) ?? "Failed to delete permission.";
                    StatusType = "danger";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting role permission");
                StatusMessage = "An unexpected error occurred.";
                StatusType = "danger";
            }

            await LoadPagePermissionsAsync(token, cancellationToken);
            return Page();
        }

        private async Task LoadPagePermissionsAsync(string token, CancellationToken cancellationToken)
        {
            try
            {
                using var client = CreateApiClient(token);

                using var response = await client.GetAsync("api/RolePermission", cancellationToken);

                if (response.IsSuccessStatusCode)
                {
                    var contentStream = await response.Content.ReadAsStreamAsync(cancellationToken);
                    var permissions = await JsonSerializer.DeserializeAsync<List<RolePermissionRow>>(contentStream, JsonOptions, cancellationToken);
                    var list = permissions ?? new List<RolePermissionRow>();

                    var filtered = ApplyPermissionFiltering(list, SearchTerm);
                    var sorted = ApplyPermissionSorting(filtered, SortColumn, SortDirection);

                    TotalRecords = sorted.Count;
                    TotalPages = (int)Math.Ceiling((double)TotalRecords / PageSize);

                    RolePermissions = sorted
                        .Skip((CurrentPage - 1) * PageSize)
                        .Take(PageSize)
                        .ToList();
                }
                else
                {
                    _logger.LogError("Failed to load role permissions. Status: {StatusCode}", response.StatusCode);
                    RolePermissions = Array.Empty<RolePermissionRow>();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading role permissions");
                StatusMessage = "Error loading role permissions.";
                StatusType = "danger";
                RolePermissions = Array.Empty<RolePermissionRow>();
            }
        }

        private async Task LoadRolesAsync(string token, CancellationToken cancellationToken)
        {
            try
            {
                using var client = CreateApiClient(token);
                using var response = await client.GetAsync("api/RolePermission/Roles", cancellationToken);

                if (response.IsSuccessStatusCode)
                {
                    var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
                    var roles = await JsonSerializer.DeserializeAsync<List<Role>>(stream, JsonOptions, cancellationToken);
                    Roles = roles ?? new List<Role>();
                }
                else
                {
                    Roles = Array.Empty<Role>();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading roles");
                Roles = Array.Empty<Role>();
            }
        }

        private async Task LoadPagesAsync(string token, CancellationToken cancellationToken)
        {
            try
            {
                using var client = CreateApiClient(token);
                using var response = await client.GetAsync("api/RolePermission/Pages", cancellationToken);

                if (response.IsSuccessStatusCode)
                {
                    var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
                    var pages = await JsonSerializer.DeserializeAsync<List<PagePermission>>(stream, JsonOptions, cancellationToken);
                    Pages = pages ?? new List<PagePermission>();
                }
                else
                {
                    Pages = Array.Empty<PagePermission>();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading Pages");
                Pages = Array.Empty<PagePermission>();
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

        private static List<RolePermissionRow> ApplyPermissionFiltering(IEnumerable<RolePermissionRow> items, string? searchTerm)
        {
            if (string.IsNullOrWhiteSpace(searchTerm))
                return items.ToList();

            var term = searchTerm.Trim().ToLowerInvariant();
            return items.Where(p =>
                p.RoleName.ToLowerInvariant().Contains(term) ||
                p.PageNames.Any(pg => pg.ToLowerInvariant().Contains(term))
            ).ToList();
        }

        private static IReadOnlyList<RolePermissionRow> ApplyPermissionSorting(IEnumerable<RolePermissionRow> items, string sortColumn, string sortDirection)
        {
            var normalizedColumn = (sortColumn ?? "").Trim().ToLowerInvariant();
            var isDesc = string.Equals(sortDirection, "desc", StringComparison.OrdinalIgnoreCase);

            Func<RolePermissionRow, IComparable?> keySelector = normalizedColumn switch
            {
                "role" => p => p.RoleName,
                "accessibleto" => p => string.Join(",", p.PageNames),
                _ => p => p.RoleName
            };

            return isDesc
                ? items.OrderByDescending(keySelector).ToList()
                : items.OrderBy(keySelector).ToList();
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


        public class RolePermissionRow
        {
            public int RoleId { get; set; }
            public string RoleName { get; set; } = string.Empty;
            public List<string> PageNames { get; set; } = new();
        }

        public class PagePermission
        {
            public int Id { get; set; }
            public string PageName { get; set; } = string.Empty;
            public string PagePath { get; set; } = string.Empty;
        }

        public class Role
        {
            public int Id { get; set; }
            public string Name { get; set; } = string.Empty;
        }

        public class NewPermissionModel
        {
            public int RoleId { get; set; }
            public List<int> PageIds { get; set; } = new();
        }

        public class UpdatePermissionModel
        {
            public int RoleId { get; set; }
            public List<int> PageIds { get; set; } = new();
        }

        private class ApiError
        {
            public string? Message { get; set; }
            public string? Msg { get; set; }
        }
    }
}
