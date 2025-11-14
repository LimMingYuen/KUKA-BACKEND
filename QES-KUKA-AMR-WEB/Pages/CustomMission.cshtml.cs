using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace QES_KUKA_AMR_WEB.Pages
{
    public class CustomMissionModel : PageModel
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfiguration _configuration;
        private readonly ILogger<CustomMissionModel> _logger;

        public CustomMissionModel(
            IHttpClientFactory httpClientFactory,
            IConfiguration configuration,
            ILogger<CustomMissionModel> logger)
        {
            _httpClientFactory = httpClientFactory;
            _configuration = configuration;
            _logger = logger;
        }

        [BindProperty]
        public string MissionName { get; set; } = string.Empty;

        [BindProperty]
        public string? Description { get; set; }

        [BindProperty]
        public string? MissionType { get; set; }

        [BindProperty]
        public int Priority { get; set; } = 1;

        [BindProperty]
        public string? RobotModels { get; set; }

        [BindProperty]
        public string? RobotIds { get; set; }

        [BindProperty]
        public string? RobotType { get; set; }

        [BindProperty]
        public string? ContainerModelCode { get; set; }

        [BindProperty]
        public string? ContainerCode { get; set; }

        [BindProperty]
        public string? IdleNode { get; set; }

        [BindProperty]
        public List<MissionStepInput> Steps { get; set; } = new();

        [BindProperty]
        public int? EditingMissionId { get; set; }

        public string? SuccessMessage { get; set; }
        public string? ErrorMessage { get; set; }

        // Configuration data for dropdowns
        public List<ConfigOption> MissionTypes { get; set; } = new();
        public List<ConfigOption> RobotTypes { get; set; } = new();
        public List<ConfigOption> ShelfDecisionRules { get; set; } = new();
        public List<ConfigOption> ResumeStrategies { get; set; } = new();
        public List<QrCodeOption> QrCodes { get; set; } = new();
        public List<AreaOption> Areas { get; set; } = new();
        public List<MapZoneOption> MapZones { get; set; } = new();

        // List of all saved missions
        public List<SavedMissionDto> SavedMissions { get; set; } = new();

        public async Task<IActionResult> OnGetAsync()
        {
            // Check if user is logged in
            var token = HttpContext.Session.GetString("JwtToken");
            if (string.IsNullOrEmpty(token))
            {
                return RedirectToPage("/Login");
            }

            // Fetch all configuration data
            await FetchConfigurationDataAsync(token);

            // Load all saved missions
            await LoadSavedMissionsAsync(token);

            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            try
            {
                // Check if user is logged in
                var token = HttpContext.Session.GetString("JwtToken");
                if (string.IsNullOrEmpty(token))
                {
                    return RedirectToPage("/Login");
                }

                // Re-fetch configuration data for dropdowns
                await FetchConfigurationDataAsync(token);

                // Validate required fields
                if (string.IsNullOrWhiteSpace(MissionName))
                {
                    ErrorMessage = "Mission Name is required.";
                    await LoadSavedMissionsAsync(token);
                    return Page();
                }

                if (Steps == null || !Steps.Any())
                {
                    ErrorMessage = "Please add at least one mission step.";
                    await LoadSavedMissionsAsync(token);
                    return Page();
                }

                if (string.IsNullOrWhiteSpace(MissionType))
                {
                    ErrorMessage = "Mission Type is required.";
                    await LoadSavedMissionsAsync(token);
                    return Page();
                }

                if (string.IsNullOrWhiteSpace(RobotType))
                {
                    ErrorMessage = "Robot Type is required.";
                    await LoadSavedMissionsAsync(token);
                    return Page();
                }

                // Build save request
                var saveRequest = BuildSaveRequest();

                // Send to API
                var httpClient = _httpClientFactory.CreateClient();
                var apiBaseUrl = _configuration["ApiSettings:BaseUrl"] ?? "http://localhost:5000";

                httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

                var jsonContent = JsonSerializer.Serialize(saveRequest, new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                });

                var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

                HttpResponseMessage response;
                if (EditingMissionId.HasValue && EditingMissionId.Value > 0)
                {
                    // Update existing mission
                    var apiUrl = $"{apiBaseUrl}/api/saved-custom-missions/{EditingMissionId.Value}";
                    _logger.LogInformation("Updating custom mission ID {Id} '{MissionName}'", EditingMissionId.Value, MissionName);
                    response = await httpClient.PutAsync(apiUrl, content);
                }
                else
                {
                    // Create new mission
                    var apiUrl = $"{apiBaseUrl}/api/saved-custom-missions";
                    _logger.LogInformation("Creating custom mission '{MissionName}'", MissionName);
                    response = await httpClient.PostAsync(apiUrl, content);
                }

                if (response.IsSuccessStatusCode)
                {
                    var responseContent = await response.Content.ReadAsStringAsync();
                    _logger.LogInformation("Custom mission saved successfully: {Response}", responseContent);

                    SuccessMessage = EditingMissionId.HasValue
                        ? $"Mission '{MissionName}' has been updated successfully!"
                        : $"Mission '{MissionName}' has been created successfully!";

                    // Clear form for next mission
                    ClearForm();

                    // Re-fetch config data and missions
                    await FetchConfigurationDataAsync(token);
                    await LoadSavedMissionsAsync(token);

                    return Page();
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    _logger.LogError("Failed to save custom mission. Status: {StatusCode}, Error: {Error}",
                        response.StatusCode, errorContent);

                    ErrorMessage = $"Failed to save mission: {response.StatusCode}. {errorContent}";
                    await LoadSavedMissionsAsync(token);
                    return Page();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving custom mission");
                ErrorMessage = $"An error occurred: {ex.Message}";
                var token = HttpContext.Session.GetString("JwtToken");
                if (!string.IsNullOrEmpty(token))
                {
                    await LoadSavedMissionsAsync(token);
                }
                return Page();
            }
        }

        public async Task<IActionResult> OnPostDeleteAsync(int id)
        {
            var token = HttpContext.Session.GetString("JwtToken");
            if (string.IsNullOrEmpty(token))
            {
                return RedirectToPage("/Login");
            }

            try
            {
                var httpClient = _httpClientFactory.CreateClient();
                var apiBaseUrl = _configuration["ApiSettings:BaseUrl"] ?? "http://localhost:5000";
                var apiUrl = $"{apiBaseUrl}/api/saved-custom-missions/{id}";

                httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

                _logger.LogInformation("Deleting custom mission {Id}", id);

                var response = await httpClient.DeleteAsync(apiUrl);

                if (response.IsSuccessStatusCode)
                {
                    _logger.LogInformation("Deleted mission {Id} successfully", id);
                    SuccessMessage = "Mission deleted successfully!";
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    _logger.LogError("Failed to delete mission {Id}. Status: {StatusCode}, Error: {Error}",
                        id, response.StatusCode, errorContent);

                    ErrorMessage = $"Failed to delete mission: {response.StatusCode}";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting custom mission {Id}", id);
                ErrorMessage = $"An error occurred: {ex.Message}";
            }

            // Reload configuration and missions
            await FetchConfigurationDataAsync(token);
            await LoadSavedMissionsAsync(token);

            return Page();
        }

        public async Task<IActionResult> OnGetEditAsync(int id)
        {
            var token = HttpContext.Session.GetString("JwtToken");
            if (string.IsNullOrEmpty(token))
            {
                return RedirectToPage("/Login");
            }

            try
            {
                var httpClient = _httpClientFactory.CreateClient();
                var apiBaseUrl = _configuration["ApiSettings:BaseUrl"] ?? "http://localhost:5000";
                var apiUrl = $"{apiBaseUrl}/api/saved-custom-missions/{id}";

                httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

                var response = await httpClient.GetAsync(apiUrl);

                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    var result = JsonSerializer.Deserialize<ApiResponse<SavedMissionDto>>(
                        json,
                        new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                    if (result?.Data != null)
                    {
                        var mission = result.Data;

                        // Populate form fields
                        EditingMissionId = mission.Id;
                        MissionName = mission.MissionName;
                        Description = mission.Description;
                        MissionType = mission.MissionType;
                        RobotType = mission.RobotType;
                        Priority = mission.Priority;
                        RobotModels = mission.RobotModels;
                        RobotIds = mission.RobotIds;
                        ContainerModelCode = mission.ContainerModelCode;
                        ContainerCode = mission.ContainerCode;
                        IdleNode = mission.IdleNode;

                        // Deserialize steps
                        if (!string.IsNullOrWhiteSpace(mission.MissionStepsJson))
                        {
                            var stepsData = JsonSerializer.Deserialize<List<JsonElement>>(
                                mission.MissionStepsJson,
                                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                            Steps = stepsData?.Select(step => new MissionStepInput
                            {
                                Sequence = step.GetProperty("sequence").GetInt32(),
                                Position = step.GetProperty("position").GetString() ?? string.Empty,
                                Type = step.GetProperty("type").GetString() ?? string.Empty,
                                PutDown = step.GetProperty("putDown").GetBoolean(),
                                PassStrategy = step.GetProperty("passStrategy").GetString() ?? string.Empty,
                                WaitingMillis = step.GetProperty("waitingMillis").GetInt32()
                            }).ToList() ?? new List<MissionStepInput>();
                        }
                    }
                }
                else
                {
                    ErrorMessage = $"Failed to load mission: {response.StatusCode}";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading custom mission {Id}", id);
                ErrorMessage = $"An error occurred: {ex.Message}";
            }

            // Load configuration and missions
            await FetchConfigurationDataAsync(token);
            await LoadSavedMissionsAsync(token);

            return Page();
        }

        public async Task<IActionResult> OnGetGetMissionAsync(int id)
        {
            var token = HttpContext.Session.GetString("JwtToken");
            if (string.IsNullOrEmpty(token))
            {
                return new JsonResult(new { error = "Unauthorized" }) { StatusCode = 401 };
            }

            try
            {
                var httpClient = _httpClientFactory.CreateClient();
                var apiBaseUrl = _configuration["ApiSettings:BaseUrl"] ?? "http://localhost:5000";
                var apiUrl = $"{apiBaseUrl}/api/saved-custom-missions/{id}";

                httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

                var response = await httpClient.GetAsync(apiUrl);

                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    var result = JsonSerializer.Deserialize<ApiResponse<SavedMissionDto>>(
                        json,
                        new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                    if (result?.Data != null)
                    {
                        return new JsonResult(result.Data);
                    }
                }

                return new JsonResult(new { error = "Mission not found" }) { StatusCode = 404 };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting mission {Id}", id);
                return new JsonResult(new { error = ex.Message }) { StatusCode = 500 };
            }
        }

        public async Task<IActionResult> OnGetRobotTypesAsync()
        {
            var token = HttpContext.Session.GetString("JwtToken");
            if (string.IsNullOrEmpty(token))
            {
                return new JsonResult(new { error = "Unauthorized" }) { StatusCode = 401 };
            }

            try
            {
                var httpClient = _httpClientFactory.CreateClient();
                var apiBaseUrl = _configuration["ApiSettings:BaseUrl"] ?? "http://localhost:5000";
                var apiUrl = $"{apiBaseUrl}/api/mobilerobot/types";

                httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

                var response = await httpClient.GetAsync(apiUrl);

                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    var robotTypes = JsonSerializer.Deserialize<List<string>>(
                        json,
                        new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                    return new JsonResult(robotTypes);
                }

                return new JsonResult(new { error = "Failed to load robot types" }) { StatusCode = (int)response.StatusCode };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting robot types");
                return new JsonResult(new { error = ex.Message }) { StatusCode = 500 };
            }
        }

        public async Task<IActionResult> OnGetRobotsByTypeAsync(string typeCode)
        {
            var token = HttpContext.Session.GetString("JwtToken");
            if (string.IsNullOrEmpty(token))
            {
                return new JsonResult(new { error = "Unauthorized" }) { StatusCode = 401 };
            }

            if (string.IsNullOrWhiteSpace(typeCode))
            {
                return new JsonResult(new { error = "Type code is required" }) { StatusCode = 400 };
            }

            try
            {
                var httpClient = _httpClientFactory.CreateClient();
                var apiBaseUrl = _configuration["ApiSettings:BaseUrl"] ?? "http://localhost:5000";
                var apiUrl = $"{apiBaseUrl}/api/mobilerobot/by-type/{Uri.EscapeDataString(typeCode)}";

                httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

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
                _logger.LogError(ex, "Error getting robots by type {TypeCode}", typeCode);
                return new JsonResult(new { error = ex.Message }) { StatusCode = 500 };
            }
        }

        private void ClearForm()
        {
            EditingMissionId = null;
            MissionName = string.Empty;
            Description = null;
            MissionType = null;
            Priority = 1;
            RobotModels = null;
            RobotIds = null;
            RobotType = null;
            ContainerModelCode = null;
            ContainerCode = null;
            IdleNode = null;
            Steps = new List<MissionStepInput>();
        }

        private async Task LoadSavedMissionsAsync(string token)
        {
            try
            {
                var httpClient = _httpClientFactory.CreateClient();
                var apiBaseUrl = _configuration["ApiSettings:BaseUrl"] ?? "http://localhost:5000";
                var apiUrl = $"{apiBaseUrl}/api/saved-custom-missions";

                httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

                var response = await httpClient.GetAsync(apiUrl);

                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    var result = JsonSerializer.Deserialize<ApiResponse<List<SavedMissionDto>>>(
                        json,
                        new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                    SavedMissions = result?.Data ?? new List<SavedMissionDto>();
                }
                else
                {
                    _logger.LogError("Failed to load saved missions. Status: {StatusCode}", response.StatusCode);
                    SavedMissions = new List<SavedMissionDto>();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading saved missions");
                SavedMissions = new List<SavedMissionDto>();
            }
        }

        private async Task FetchConfigurationDataAsync(string token)
        {
            var httpClient = _httpClientFactory.CreateClient();
            var apiBaseUrl = _configuration["ApiSettings:BaseUrl"] ?? "http://localhost:5000";
            httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            try
            {
                // Fetch Mission Types
                var missionTypesResponse = await httpClient.GetAsync($"{apiBaseUrl}/api/v1/mission-types");
                if (missionTypesResponse.IsSuccessStatusCode)
                {
                    var json = await missionTypesResponse.Content.ReadAsStringAsync();
                    var response = JsonSerializer.Deserialize<ApiResponse<List<ConfigOption>>>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                    MissionTypes = response?.Data ?? new List<ConfigOption>();
                }

                // Fetch Robot Types
                var robotTypesResponse = await httpClient.GetAsync($"{apiBaseUrl}/api/v1/robot-types");
                if (robotTypesResponse.IsSuccessStatusCode)
                {
                    var json = await robotTypesResponse.Content.ReadAsStringAsync();
                    var response = JsonSerializer.Deserialize<ApiResponse<List<ConfigOption>>>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                    RobotTypes = response?.Data ?? new List<ConfigOption>();
                }

                // Fetch Shelf Decision Rules
                var shelfDecisionResponse = await httpClient.GetAsync($"{apiBaseUrl}/api/v1/shelf-decision-rules");
                if (shelfDecisionResponse.IsSuccessStatusCode)
                {
                    var json = await shelfDecisionResponse.Content.ReadAsStringAsync();
                    var response = JsonSerializer.Deserialize<ApiResponse<List<ConfigOption>>>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                    ShelfDecisionRules = response?.Data ?? new List<ConfigOption>();
                }

                // Fetch Resume Strategies
                var resumeStrategyResponse = await httpClient.GetAsync($"{apiBaseUrl}/api/v1/resume-strategies");
                if (resumeStrategyResponse.IsSuccessStatusCode)
                {
                    var json = await resumeStrategyResponse.Content.ReadAsStringAsync();
                    var response = JsonSerializer.Deserialize<ApiResponse<List<ConfigOption>>>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                    ResumeStrategies = response?.Data ?? new List<ConfigOption>();
                }

                // Fetch QR Codes
                var qrCodesResponse = await httpClient.GetAsync($"{apiBaseUrl}/api/QrCodes");
                if (qrCodesResponse.IsSuccessStatusCode)
                {
                    var json = await qrCodesResponse.Content.ReadAsStringAsync();
                    var response = JsonSerializer.Deserialize<List<QrCodeOption>>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                    // Create DisplayLabel in format: MapCode-FloorNumber-NodeNumber
                    QrCodes = response?.Select(qr => new QrCodeOption
                    {
                        Id = qr.Id,
                        NodeLabel = qr.NodeLabel,
                        MapCode = qr.MapCode,
                        FloorNumber = qr.FloorNumber,
                        NodeNumber = qr.NodeNumber,
                        DisplayLabel = $"{qr.MapCode}-{qr.FloorNumber}-{qr.NodeNumber}"
                    }).ToList() ?? new List<QrCodeOption>();
                }

                // Fetch Areas (used for mission step types)
                var areasResponse = await httpClient.GetAsync($"{apiBaseUrl}/api/v1/areas");
                if (areasResponse.IsSuccessStatusCode)
                {
                    var json = await areasResponse.Content.ReadAsStringAsync();
                    var response = JsonSerializer.Deserialize<ApiResponse<List<AreaOption>>>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                    Areas = response?.Data?.Where(area => area.IsActive).ToList() ?? new List<AreaOption>();
                }

                // Fetch MapZones (used for Location dropdown when Area type selected)
                var mapZonesResponse = await httpClient.GetAsync($"{apiBaseUrl}/api/mapzones");
                if (mapZonesResponse.IsSuccessStatusCode)
                {
                    var json = await mapZonesResponse.Content.ReadAsStringAsync();
                    var response = JsonSerializer.Deserialize<List<MapZoneOption>>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                    MapZones = response ?? new List<MapZoneOption>();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching configuration data");
            }
        }

        private object BuildSaveRequest()
        {
            // Build mission data from steps
            // Type field now contains the location value (Area ActualValue or Node DisplayLabel)
            var missionData = Steps.Select((step, index) => new
            {
                sequence = index + 1,
                position = step.Position ?? string.Empty,
                type = step.Type ?? string.Empty,
                putDown = step.PutDown,
                passStrategy = step.PassStrategy,
                waitingMillis = step.WaitingMillis
            }).ToList();

            // Serialize mission data to JSON string
            var missionStepsJson = JsonSerializer.Serialize(missionData, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });

            return new
            {
                missionName = MissionName,
                description = Description,
                missionType = MissionType,
                robotType = RobotType,
                priority = Priority,
                robotModels = RobotModels, // Comma-separated string
                robotIds = RobotIds, // Comma-separated string
                containerModelCode = ContainerModelCode,
                containerCode = ContainerCode,
                idleNode = IdleNode,
                missionStepsJson = missionStepsJson
            };
        }
    }

    public class MissionStepInput
    {
        public int Sequence { get; set; }
        public string Position { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public bool PutDown { get; set; }
        public string PassStrategy { get; set; } = string.Empty;
        public int WaitingMillis { get; set; }
    }

    public class ConfigOption
    {
        public int Id { get; set; }
        public string DisplayName { get; set; } = string.Empty;
        public string ActualValue { get; set; } = string.Empty;
    }

    public class QrCodeOption
    {
        public int Id { get; set; }
        public string NodeLabel { get; set; } = string.Empty;
        public string MapCode { get; set; } = string.Empty;
        public string FloorNumber { get; set; } = string.Empty;
        public int NodeNumber { get; set; }
        public string DisplayLabel { get; set; } = string.Empty;
    }

    public class AreaOption
    {
        public int Id { get; set; }
        public string DisplayName { get; set; } = string.Empty;
        public string ActualValue { get; set; } = string.Empty;
        public bool IsActive { get; set; }
    }

    public class MapZoneOption
    {
        public int Id { get; set; }
        public string ZoneName { get; set; } = string.Empty;
        public string ZoneCode { get; set; } = string.Empty;
    }

    public class ApiResponse<T>
    {
        public bool Success { get; set; }
        public T? Data { get; set; }
    }
}
