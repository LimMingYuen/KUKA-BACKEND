using System.ComponentModel.DataAnnotations;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace QES_KUKA_AMR_WEB.Pages
{
    public class LoginModel : PageModel
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfiguration _configuration;
        private readonly ILogger<LoginModel> _logger;

        [BindProperty]
        [Required(ErrorMessage = "Username is required")]
        public string Username { get; set; } = string.Empty;

        [BindProperty]
        [Required(ErrorMessage = "Password is required")]
        public string Password { get; set; } = string.Empty;

        public string? ErrorMessage { get; set; }

        public LoginModel(
            IHttpClientFactory httpClientFactory,
            IConfiguration configuration,
            ILogger<LoginModel> logger)
        {
            _httpClientFactory = httpClientFactory;
            _configuration = configuration;
            _logger = logger;
        }

        public void OnGet()
        {
            // Clear any existing session
            HttpContext.Session.Clear();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                return Page();
            }

            try
            {
                var apiBaseUrl = _configuration["ApiSettings:BaseUrl"] ?? "https://localhost:7255";
                var httpClient = _httpClientFactory.CreateClient();
                httpClient.BaseAddress = new Uri(apiBaseUrl);

                var loginRequest = new
                {
                    username = Username,
                    password = Password
                };

                var jsonContent = JsonSerializer.Serialize(loginRequest);
                using var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

                var response = await httpClient.PostAsync("api/login", content);
                var responseContent = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    ErrorMessage = ExtractErrorMessage(responseContent) ??
                                   "Unable to connect to the authentication server. Please try again.";
                    _logger.LogWarning(
                        "Login API returned status code {StatusCode} for user {Username}",
                        response.StatusCode,
                        Username);
                    return Page();
                }

                var loginResponse = JsonSerializer.Deserialize<LoginApiResponse>(
                    responseContent,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                if (loginResponse?.Success == true &&
                    !string.IsNullOrEmpty(loginResponse.Data?.Token))
                {
                    // Store token, refresh token, and user info in session
                    HttpContext.Session.SetString("JwtToken", loginResponse.Data.Token);
                    HttpContext.Session.SetString("Username", Username);
                    
                    // Fetch user roles from API
                    var userRoles = await FetchUserRolesAsync(loginResponse.Data.Token, Username);
                    HttpContext.Session.SetString("Role", JsonSerializer.Serialize(userRoles));

                    // Store refresh token if available
                    if (!string.IsNullOrEmpty(loginResponse.Data.RefreshToken))
                    {
                        HttpContext.Session.SetString("RefreshToken", loginResponse.Data.RefreshToken);

                        // Store token expiration time for auto-refresh logic
                        if (loginResponse.Data.TokenExpiresUtc != default)
                        {
                            HttpContext.Session.SetString("TokenExpiresUtc",
                                loginResponse.Data.TokenExpiresUtc.ToString("O")); // ISO 8601 format
                        }

                        _logger.LogInformation("User {Username} logged in successfully. Token expires at {ExpiresUtc}",
                            Username, loginResponse.Data.TokenExpiresUtc);
                    }
                    else
                    {
                        _logger.LogInformation("User {Username} logged in successfully", Username);
                    }

                    return RedirectToPage("/WorkflowManagement");
                }

                ErrorMessage = loginResponse?.Msg ?? "Login failed. Please check your credentials.";
                _logger.LogWarning("Login failed for user {Username}: {Error}", Username, ErrorMessage);
            }
            catch (Exception ex)
            {
                ErrorMessage = "An error occurred during login. Please try again.";
                _logger.LogError(ex, "Exception occurred during login for user {Username}", Username);
            }

            return Page();
        }

        private async Task<List<string>> FetchUserRolesAsync(string token, string username)
        {
            try
            {
                var apiBaseUrl = _configuration["ApiSettings:BaseUrl"] ?? "https://localhost:7255";
                var httpClient = _httpClientFactory.CreateClient();
                httpClient.BaseAddress = new Uri(apiBaseUrl);
                httpClient.DefaultRequestHeaders.Authorization = 
                    new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

                var response = await httpClient.GetAsync($"api/User/roles?username={Uri.EscapeDataString(username)}");
                
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var roles = JsonSerializer.Deserialize<List<string>>(content, 
                        new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                    return roles ?? new List<string>();
                }
                
                _logger.LogWarning("Failed to fetch roles for user {Username}", username);
                return new List<string>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching user roles for {Username}", username);
                return new List<string>();
            }
        }

        private static string? ExtractErrorMessage(string responseContent)
        {
            try
            {
                var errorResponse = JsonSerializer.Deserialize<LoginApiResponse>(
                    responseContent,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                return errorResponse?.Msg;
            }
            catch
            {
                return null;
            }
        }

        private class LoginApiResponse
        {
            public bool Success { get; set; }
            public string? Msg { get; set; }
            public LoginApiResponseData? Data { get; set; }
        }

        private class LoginApiResponseData
        {
            public string? Token { get; set; }
            public string? RefreshToken { get; set; }
            public DateTime TokenExpiresUtc { get; set; }
        }
    }
}
