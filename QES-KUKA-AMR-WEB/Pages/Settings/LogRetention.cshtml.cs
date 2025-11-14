using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Net.Http.Headers;
using System.Text.Json;

namespace QES_KUKA_AMR_WEB.Pages.Settings
{
    public class LogRetentionModel : PageModel
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfiguration _configuration;
        private readonly ILogger<LogRetentionModel> _logger;

        public int RetentionMonths { get; set; } = 1;
        public string? StatusMessage { get; set; }
        public string StatusType { get; set; } = "info";

        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNameCaseInsensitive = true
        };

        public LogRetentionModel(
            IHttpClientFactory httpClientFactory,
            IConfiguration configuration,
            ILogger<LogRetentionModel> logger)
        {
            _httpClientFactory = httpClientFactory;
            _configuration = configuration;
            _logger = logger;
        }

        public async Task<IActionResult> OnGetAsync(CancellationToken cancellationToken)
        {
            var token = HttpContext.Session.GetString("JwtToken");
            if (string.IsNullOrEmpty(token))
                return RedirectToPage("/Login");

            try
            {
                using var client = CreateApiClient(token);
                var response = await client.GetAsync("api/LogCleanup/setting", cancellationToken);

                if (!response.IsSuccessStatusCode)
                {
                    StatusMessage = $"Failed to load setting (HTTP {(int)response.StatusCode}).";
                    StatusType = "warning";
                    return Page();
                }

                var json = await response.Content.ReadAsStringAsync(cancellationToken);
                var result = JsonSerializer.Deserialize<LogSettingResponse>(json, JsonOptions);
                if (result != null)
                    RetentionMonths = result.Value;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading log retention setting");
                StatusMessage = "Unable to load log retention settings.";
                StatusType = "danger";
            }

            return Page();
        }

        public async Task<IActionResult> OnPostAsync(CancellationToken cancellationToken)
        {
            var token = HttpContext.Session.GetString("JwtToken");
            if (string.IsNullOrEmpty(token))
                return RedirectToPage("/Login");

            var formValue = Request.Form["RetentionMonths"];
            if (!int.TryParse(formValue, out var months))
                months = 1;

            months = Math.Clamp(months, 1, 12);

            try
            {
                using var client = CreateApiClient(token);
                var content = JsonContent.Create(new { RetentionMonths = months });

                var response = await client.PostAsync("api/LogCleanup/setting", content, cancellationToken);
                var json = await response.Content.ReadAsStringAsync(cancellationToken);

                if (response.IsSuccessStatusCode)
                {
                    StatusMessage = $"Log retention updated to {months} month(s).";
                    StatusType = "success";
                    RetentionMonths = months;
                }
                else
                {
                    _logger.LogWarning("Failed to update setting: {Msg}", json);
                    StatusMessage = $"Failed to update setting (HTTP {(int)response.StatusCode}).";
                    StatusType = "danger";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving log retention setting");
                StatusMessage = "Error saving log retention setting.";
                StatusType = "danger";
            }

            return Page();
        }

        public async Task<IActionResult> OnPostRunCleanupAsync(CancellationToken cancellationToken)
        {
            var token = HttpContext.Session.GetString("JwtToken");
            if (string.IsNullOrEmpty(token))
                return RedirectToPage("/Login");

            try
            {
                using var client = CreateApiClient(token);
                var response = await client.PostAsync("api/LogCleanup/run", null, cancellationToken);
                var json = await response.Content.ReadAsStringAsync(cancellationToken);

                if (response.IsSuccessStatusCode)
                {
                    StatusMessage = "Log cleanup executed successfully.";
                    StatusType = "success";
                }
                else
                {
                    _logger.LogWarning("Cleanup failed: {Response}", json);
                    StatusMessage = "Failed to run log cleanup.";
                    StatusType = "danger";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error running log cleanup");
                StatusMessage = "Error running log cleanup.";
                StatusType = "danger";
            }

            await OnGetAsync(cancellationToken); 
            return Page();
        }

        private HttpClient CreateApiClient(string token)
        {
            var apiBase = _configuration["ApiSettings:BaseUrl"] ?? "https://localhost:7255";
            var client = _httpClientFactory.CreateClient();
            client.BaseAddress = new Uri(apiBase);
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            return client;
        }

        private class LogSettingResponse
        {
            public string Key { get; set; } = string.Empty;
            public int Value { get; set; }
        }
    }
}
