using System.ComponentModel.DataAnnotations;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace QES_KUKA_AMR_WEB.Pages;

[IgnoreAntiforgeryToken]
public class MissionConfigurationModel : PageModel
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IConfiguration _configuration;
    private readonly ILogger<MissionConfigurationModel> _logger;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private static readonly JsonSerializerOptions JsonWriteOptions = new()
    {
        PropertyNamingPolicy = null,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    public MissionConfigurationModel(
        IHttpClientFactory httpClientFactory,
        IConfiguration configuration,
        ILogger<MissionConfigurationModel> logger)
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

    public async Task<IActionResult> OnGetMissionTypesAsync(CancellationToken cancellationToken)
    {
        var token = HttpContext.Session.GetString("JwtToken");
        if (string.IsNullOrEmpty(token))
        {
            return new JsonResult(new { success = false, message = "Session expired." }) { StatusCode = 401 };
        }

        try
        {
            using var client = CreateApiClient(token);
            using var response = await client.GetAsync("api/v1/mission-types", cancellationToken);
            var payload = await response.Content.ReadAsStringAsync(cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                var message = ExtractErrorMessage(payload);
                return StatusCode((int)response.StatusCode, new { success = false, message });
            }

            var envelope = JsonSerializer.Deserialize<ApiEnvelope<List<MissionTypeDto>>>(payload, JsonOptions);
            return new JsonResult(new
            {
                success = envelope?.Success ?? false,
                data = envelope?.Data ?? new List<MissionTypeDto>(),
                message = envelope?.Msg
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve mission types.");
            return StatusCode(500, new { success = false, message = "Failed to load mission types." });
        }
    }

    public async Task<IActionResult> OnGetRobotTypesAsync(CancellationToken cancellationToken)
    {
        var token = HttpContext.Session.GetString("JwtToken");
        if (string.IsNullOrEmpty(token))
        {
            return new JsonResult(new { success = false, message = "Session expired." }) { StatusCode = 401 };
        }

        try
        {
            using var client = CreateApiClient(token);
            using var response = await client.GetAsync("api/v1/robot-types", cancellationToken);
            var payload = await response.Content.ReadAsStringAsync(cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                var message = ExtractErrorMessage(payload);
                return StatusCode((int)response.StatusCode, new { success = false, message });
            }

            var envelope = JsonSerializer.Deserialize<ApiEnvelope<List<RobotTypeDto>>>(payload, JsonOptions);
            return new JsonResult(new
            {
                success = envelope?.Success ?? false,
                data = envelope?.Data ?? new List<RobotTypeDto>(),
                message = envelope?.Msg
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve robot types.");
            return StatusCode(500, new { success = false, message = "Failed to load robot types." });
        }
    }

    public async Task<IActionResult> OnGetShelfDecisionRulesAsync(CancellationToken cancellationToken)
    {
        var token = HttpContext.Session.GetString("JwtToken");
        if (string.IsNullOrEmpty(token))
        {
            return new JsonResult(new { success = false, message = "Session expired." }) { StatusCode = 401 };
        }

        try
        {
            using var client = CreateApiClient(token);
            using var response = await client.GetAsync("api/v1/shelf-decision-rules", cancellationToken);
            var payload = await response.Content.ReadAsStringAsync(cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                var message = ExtractErrorMessage(payload);
                return StatusCode((int)response.StatusCode, new { success = false, message });
            }

            var envelope = JsonSerializer.Deserialize<ApiEnvelope<List<ShelfDecisionRuleDto>>>(payload, JsonOptions);
            return new JsonResult(new
            {
                success = envelope?.Success ?? false,
                data = envelope?.Data ?? new List<ShelfDecisionRuleDto>(),
                message = envelope?.Msg
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve shelf decision rules.");
            return StatusCode(500, new { success = false, message = "Failed to load shelf decision rules." });
        }
    }

    public async Task<IActionResult> OnGetResumeStrategiesAsync(CancellationToken cancellationToken)
    {
        var token = HttpContext.Session.GetString("JwtToken");
        if (string.IsNullOrEmpty(token))
        {
            return new JsonResult(new { success = false, message = "Session expired." }) { StatusCode = 401 };
        }

        try
        {
            using var client = CreateApiClient(token);
            using var response = await client.GetAsync("api/v1/resume-strategies", cancellationToken);
            var payload = await response.Content.ReadAsStringAsync(cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                var message = ExtractErrorMessage(payload);
                return StatusCode((int)response.StatusCode, new { success = false, message });
            }

            var envelope = JsonSerializer.Deserialize<ApiEnvelope<List<ResumeStrategyDto>>>(payload, JsonOptions);
            return new JsonResult(new
            {
                success = envelope?.Success ?? false,
                data = envelope?.Data ?? new List<ResumeStrategyDto>(),
                message = envelope?.Msg
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve resume strategies.");
            return StatusCode(500, new { success = false, message = "Failed to load resume strategies." });
        }
    }

    public async Task<IActionResult> OnGetAreasAsync(CancellationToken cancellationToken)
    {
        var token = HttpContext.Session.GetString("JwtToken");
        if (string.IsNullOrEmpty(token))
        {
            return new JsonResult(new { success = false, message = "Session expired." }) { StatusCode = 401 };
        }

        try
        {
            using var client = CreateApiClient(token);
            using var response = await client.GetAsync("api/v1/areas", cancellationToken);
            var payload = await response.Content.ReadAsStringAsync(cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                var message = ExtractErrorMessage(payload);
                return StatusCode((int)response.StatusCode, new { success = false, message });
            }

            var envelope = JsonSerializer.Deserialize<ApiEnvelope<List<AreaDto>>>(payload, JsonOptions);
            return new JsonResult(new
            {
                success = envelope?.Success ?? false,
                data = envelope?.Data ?? new List<AreaDto>(),
                message = envelope?.Msg
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve areas.");
            return StatusCode(500, new { success = false, message = "Failed to load areas." });
        }
    }

    public async Task<IActionResult> OnPostCreateMissionTypeAsync(
        [FromBody] MissionTypeCreateInput request,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            var errors = string.Join("; ", ModelState.Values
                .SelectMany(v => v.Errors)
                .Select(e => e.ErrorMessage));

            return BadRequest(new { success = false, message = string.IsNullOrWhiteSpace(errors) ? "Invalid request." : errors });
        }

        var token = HttpContext.Session.GetString("JwtToken");
        if (string.IsNullOrEmpty(token))
        {
            return new JsonResult(new { success = false, message = "Session expired." }) { StatusCode = 401 };
        }

        try
        {
            using var client = CreateApiClient(token);
            var content = new StringContent(JsonSerializer.Serialize(request, JsonWriteOptions), Encoding.UTF8, "application/json");
            using var response = await client.PostAsync("api/v1/mission-types", content, cancellationToken);
            var payload = await response.Content.ReadAsStringAsync(cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                var message = ExtractErrorMessage(payload);
                return StatusCode((int)response.StatusCode, new { success = false, message });
            }

            var envelope = JsonSerializer.Deserialize<ApiEnvelope<MissionTypeDto>>(payload, JsonOptions);
            return new JsonResult(new
            {
                success = envelope?.Success ?? false,
                data = envelope?.Data,
                message = envelope?.Msg ?? "Mission type created."
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create mission type.");
            return StatusCode(500, new { success = false, message = "Failed to create mission type." });
        }
    }

    public async Task<IActionResult> OnPostUpdateMissionTypeAsync(
        [FromBody] MissionTypeUpdateInput request,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            var errors = string.Join("; ", ModelState.Values
                .SelectMany(v => v.Errors)
                .Select(e => e.ErrorMessage));

            return BadRequest(new { success = false, message = string.IsNullOrWhiteSpace(errors) ? "Invalid request." : errors });
        }

        var token = HttpContext.Session.GetString("JwtToken");
        if (string.IsNullOrEmpty(token))
        {
            return new JsonResult(new { success = false, message = "Session expired." }) { StatusCode = 401 };
        }

        try
        {
            using var client = CreateApiClient(token);
            var content = new StringContent(JsonSerializer.Serialize(request, JsonWriteOptions), Encoding.UTF8, "application/json");
            using var response = await client.PutAsync($"api/v1/mission-types/{request.Id}", content, cancellationToken);
            var payload = await response.Content.ReadAsStringAsync(cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                var message = ExtractErrorMessage(payload);
                return StatusCode((int)response.StatusCode, new { success = false, message });
            }

            var envelope = JsonSerializer.Deserialize<ApiEnvelope<MissionTypeDto>>(payload, JsonOptions);
            return new JsonResult(new
            {
                success = envelope?.Success ?? false,
                data = envelope?.Data,
                message = envelope?.Msg ?? "Mission type updated."
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update mission type {MissionTypeId}.", request.Id);
            return StatusCode(500, new { success = false, message = "Failed to update mission type." });
        }
    }

    public async Task<IActionResult> OnPostDeleteMissionTypeAsync(
        [FromBody] MissionTypeDeleteInput request,
        CancellationToken cancellationToken)
    {
        if (request.Id <= 0)
        {
            return BadRequest(new { success = false, message = "Invalid mission type id." });
        }

        var token = HttpContext.Session.GetString("JwtToken");
        if (string.IsNullOrEmpty(token))
        {
            return new JsonResult(new { success = false, message = "Session expired." }) { StatusCode = 401 };
        }

        try
        {
            using var client = CreateApiClient(token);
            using var response = await client.DeleteAsync($"api/v1/mission-types/{request.Id}", cancellationToken);
            var payload = await response.Content.ReadAsStringAsync(cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                var message = ExtractErrorMessage(payload);
                return StatusCode((int)response.StatusCode, new { success = false, message });
            }

            var envelope = JsonSerializer.Deserialize<ApiEnvelope<object>>(payload, JsonOptions);
            return new JsonResult(new
            {
                success = envelope?.Success ?? true,
                message = envelope?.Msg ?? "Mission type deleted."
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete mission type {MissionTypeId}.", request.Id);
            return StatusCode(500, new { success = false, message = "Failed to delete mission type." });
        }
    }

    public async Task<IActionResult> OnPostCreateRobotTypeAsync(
        [FromBody] RobotTypeCreateInput request,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            var errors = string.Join("; ", ModelState.Values
                .SelectMany(v => v.Errors)
                .Select(e => e.ErrorMessage));

            return BadRequest(new { success = false, message = string.IsNullOrWhiteSpace(errors) ? "Invalid request." : errors });
        }

        var token = HttpContext.Session.GetString("JwtToken");
        if (string.IsNullOrEmpty(token))
        {
            return new JsonResult(new { success = false, message = "Session expired." }) { StatusCode = 401 };
        }

        try
        {
            using var client = CreateApiClient(token);
            var content = new StringContent(JsonSerializer.Serialize(request, JsonWriteOptions), Encoding.UTF8, "application/json");
            using var response = await client.PostAsync("api/v1/robot-types", content, cancellationToken);
            var payload = await response.Content.ReadAsStringAsync(cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                var message = ExtractErrorMessage(payload);
                return StatusCode((int)response.StatusCode, new { success = false, message });
            }

            var envelope = JsonSerializer.Deserialize<ApiEnvelope<RobotTypeDto>>(payload, JsonOptions);
            return new JsonResult(new
            {
                success = envelope?.Success ?? false,
                data = envelope?.Data,
                message = envelope?.Msg ?? "Robot type created."
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create robot type.");
            return StatusCode(500, new { success = false, message = "Failed to create robot type." });
        }
    }

    public async Task<IActionResult> OnPostUpdateRobotTypeAsync(
        [FromBody] RobotTypeUpdateInput request,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            var errors = string.Join("; ", ModelState.Values
                .SelectMany(v => v.Errors)
                .Select(e => e.ErrorMessage));

            return BadRequest(new { success = false, message = string.IsNullOrWhiteSpace(errors) ? "Invalid request." : errors });
        }

        var token = HttpContext.Session.GetString("JwtToken");
        if (string.IsNullOrEmpty(token))
        {
            return new JsonResult(new { success = false, message = "Session expired." }) { StatusCode = 401 };
        }

        try
        {
            using var client = CreateApiClient(token);
            var content = new StringContent(JsonSerializer.Serialize(request, JsonWriteOptions), Encoding.UTF8, "application/json");
            using var response = await client.PutAsync($"api/v1/robot-types/{request.Id}", content, cancellationToken);
            var payload = await response.Content.ReadAsStringAsync(cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                var message = ExtractErrorMessage(payload);
                return StatusCode((int)response.StatusCode, new { success = false, message });
            }

            var envelope = JsonSerializer.Deserialize<ApiEnvelope<RobotTypeDto>>(payload, JsonOptions);
            return new JsonResult(new
            {
                success = envelope?.Success ?? false,
                data = envelope?.Data,
                message = envelope?.Msg ?? "Robot type updated."
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update robot type {RobotTypeId}.", request.Id);
            return StatusCode(500, new { success = false, message = "Failed to update robot type." });
        }
    }

    public async Task<IActionResult> OnPostDeleteRobotTypeAsync(
        [FromBody] RobotTypeDeleteInput request,
        CancellationToken cancellationToken)
    {
        if (request.Id <= 0)
        {
            return BadRequest(new { success = false, message = "Invalid robot type id." });
        }

        var token = HttpContext.Session.GetString("JwtToken");
        if (string.IsNullOrEmpty(token))
        {
            return new JsonResult(new { success = false, message = "Session expired." }) { StatusCode = 401 };
        }

        try
        {
            using var client = CreateApiClient(token);
            using var response = await client.DeleteAsync($"api/v1/robot-types/{request.Id}", cancellationToken);
            var payload = await response.Content.ReadAsStringAsync(cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                var message = ExtractErrorMessage(payload);
                return StatusCode((int)response.StatusCode, new { success = false, message });
            }

            var envelope = JsonSerializer.Deserialize<ApiEnvelope<object>>(payload, JsonOptions);
            return new JsonResult(new
            {
                success = envelope?.Success ?? true,
                message = envelope?.Msg ?? "Robot type deleted."
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete robot type {RobotTypeId}.", request.Id);
            return StatusCode(500, new { success = false, message = "Failed to delete robot type." });
        }
    }

    public async Task<IActionResult> OnPostCreateShelfDecisionRuleAsync(
        [FromBody] ShelfDecisionRuleCreateInput request,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            var errors = string.Join("; ", ModelState.Values
                .SelectMany(v => v.Errors)
                .Select(e => e.ErrorMessage));

            return BadRequest(new { success = false, message = string.IsNullOrWhiteSpace(errors) ? "Invalid request." : errors });
        }

        var token = HttpContext.Session.GetString("JwtToken");
        if (string.IsNullOrEmpty(token))
        {
            return new JsonResult(new { success = false, message = "Session expired." }) { StatusCode = 401 };
        }

        try
        {
            using var client = CreateApiClient(token);
            var content = new StringContent(JsonSerializer.Serialize(request, JsonWriteOptions), Encoding.UTF8, "application/json");
            using var response = await client.PostAsync("api/v1/shelf-decision-rules", content, cancellationToken);
            var payload = await response.Content.ReadAsStringAsync(cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                var message = ExtractErrorMessage(payload);
                return StatusCode((int)response.StatusCode, new { success = false, message });
            }

            var envelope = JsonSerializer.Deserialize<ApiEnvelope<ShelfDecisionRuleDto>>(payload, JsonOptions);
            return new JsonResult(new
            {
                success = envelope?.Success ?? false,
                data = envelope?.Data,
                message = envelope?.Msg ?? "Shelf decision rule created."
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create shelf decision rule.");
            return StatusCode(500, new { success = false, message = "Failed to create shelf decision rule." });
        }
    }

    public async Task<IActionResult> OnPostUpdateShelfDecisionRuleAsync(
        [FromBody] ShelfDecisionRuleUpdateInput request,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            var errors = string.Join("; ", ModelState.Values
                .SelectMany(v => v.Errors)
                .Select(e => e.ErrorMessage));

            return BadRequest(new { success = false, message = string.IsNullOrWhiteSpace(errors) ? "Invalid request." : errors });
        }

        var token = HttpContext.Session.GetString("JwtToken");
        if (string.IsNullOrEmpty(token))
        {
            return new JsonResult(new { success = false, message = "Session expired." }) { StatusCode = 401 };
        }

        try
        {
            using var client = CreateApiClient(token);
            var content = new StringContent(JsonSerializer.Serialize(request, JsonWriteOptions), Encoding.UTF8, "application/json");
            using var response = await client.PutAsync($"api/v1/shelf-decision-rules/{request.Id}", content, cancellationToken);
            var payload = await response.Content.ReadAsStringAsync(cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                var message = ExtractErrorMessage(payload);
                return StatusCode((int)response.StatusCode, new { success = false, message });
            }

            var envelope = JsonSerializer.Deserialize<ApiEnvelope<ShelfDecisionRuleDto>>(payload, JsonOptions);
            return new JsonResult(new
            {
                success = envelope?.Success ?? false,
                data = envelope?.Data,
                message = envelope?.Msg ?? "Shelf decision rule updated."
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update shelf decision rule {ShelfDecisionRuleId}.", request.Id);
            return StatusCode(500, new { success = false, message = "Failed to update shelf decision rule." });
        }
    }

    public async Task<IActionResult> OnPostDeleteShelfDecisionRuleAsync(
        [FromBody] ShelfDecisionRuleDeleteInput request,
        CancellationToken cancellationToken)
    {
        if (request.Id <= 0)
        {
            return BadRequest(new { success = false, message = "Invalid shelf decision rule id." });
        }

        var token = HttpContext.Session.GetString("JwtToken");
        if (string.IsNullOrEmpty(token))
        {
            return new JsonResult(new { success = false, message = "Session expired." }) { StatusCode = 401 };
        }

        try
        {
            using var client = CreateApiClient(token);
            using var response = await client.DeleteAsync($"api/v1/shelf-decision-rules/{request.Id}", cancellationToken);
            var payload = await response.Content.ReadAsStringAsync(cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                var message = ExtractErrorMessage(payload);
                return StatusCode((int)response.StatusCode, new { success = false, message });
            }

            var envelope = JsonSerializer.Deserialize<ApiEnvelope<object>>(payload, JsonOptions);
            return new JsonResult(new
            {
                success = envelope?.Success ?? true,
                message = envelope?.Msg ?? "Shelf decision rule deleted."
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete shelf decision rule {ShelfDecisionRuleId}.", request.Id);
            return StatusCode(500, new { success = false, message = "Failed to delete shelf decision rule." });
        }
    }

    public async Task<IActionResult> OnPostCreateResumeStrategyAsync(
        [FromBody] ResumeStrategyCreateInput request,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            var errors = string.Join("; ", ModelState.Values
                .SelectMany(v => v.Errors)
                .Select(e => e.ErrorMessage));

            return BadRequest(new { success = false, message = string.IsNullOrWhiteSpace(errors) ? "Invalid request." : errors });
        }

        var token = HttpContext.Session.GetString("JwtToken");
        if (string.IsNullOrEmpty(token))
        {
            return new JsonResult(new { success = false, message = "Session expired." }) { StatusCode = 401 };
        }

        try
        {
            using var client = CreateApiClient(token);
            var content = new StringContent(JsonSerializer.Serialize(request, JsonWriteOptions), Encoding.UTF8, "application/json");
            using var response = await client.PostAsync("api/v1/resume-strategies", content, cancellationToken);
            var payload = await response.Content.ReadAsStringAsync(cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                var message = ExtractErrorMessage(payload);
                return StatusCode((int)response.StatusCode, new { success = false, message });
            }

            var envelope = JsonSerializer.Deserialize<ApiEnvelope<ResumeStrategyDto>>(payload, JsonOptions);
            return new JsonResult(new
            {
                success = envelope?.Success ?? false,
                data = envelope?.Data,
                message = envelope?.Msg ?? "Resume strategy created."
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create resume strategy.");
            return StatusCode(500, new { success = false, message = "Failed to create resume strategy." });
        }
    }

    public async Task<IActionResult> OnPostUpdateResumeStrategyAsync(
        [FromBody] ResumeStrategyUpdateInput request,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            var errors = string.Join("; ", ModelState.Values
                .SelectMany(v => v.Errors)
                .Select(e => e.ErrorMessage));

            return BadRequest(new { success = false, message = string.IsNullOrWhiteSpace(errors) ? "Invalid request." : errors });
        }

        var token = HttpContext.Session.GetString("JwtToken");
        if (string.IsNullOrEmpty(token))
        {
            return new JsonResult(new { success = false, message = "Session expired." }) { StatusCode = 401 };
        }

        try
        {
            using var client = CreateApiClient(token);
            var content = new StringContent(JsonSerializer.Serialize(request, JsonWriteOptions), Encoding.UTF8, "application/json");
            using var response = await client.PutAsync($"api/v1/resume-strategies/{request.Id}", content, cancellationToken);
            var payload = await response.Content.ReadAsStringAsync(cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                var message = ExtractErrorMessage(payload);
                return StatusCode((int)response.StatusCode, new { success = false, message });
            }

            var envelope = JsonSerializer.Deserialize<ApiEnvelope<ResumeStrategyDto>>(payload, JsonOptions);
            return new JsonResult(new
            {
                success = envelope?.Success ?? false,
                data = envelope?.Data,
                message = envelope?.Msg ?? "Resume strategy updated."
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update resume strategy {ResumeStrategyId}.", request.Id);
            return StatusCode(500, new { success = false, message = "Failed to update resume strategy." });
        }
    }

    public async Task<IActionResult> OnPostDeleteResumeStrategyAsync(
        [FromBody] ResumeStrategyDeleteInput request,
        CancellationToken cancellationToken)
    {
        if (request.Id <= 0)
        {
            return BadRequest(new { success = false, message = "Invalid resume strategy id." });
        }

        var token = HttpContext.Session.GetString("JwtToken");
        if (string.IsNullOrEmpty(token))
        {
            return new JsonResult(new { success = false, message = "Session expired." }) { StatusCode = 401 };
        }

        try
        {
            using var client = CreateApiClient(token);
            using var response = await client.DeleteAsync($"api/v1/resume-strategies/{request.Id}", cancellationToken);
            var payload = await response.Content.ReadAsStringAsync(cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                var message = ExtractErrorMessage(payload);
                return StatusCode((int)response.StatusCode, new { success = false, message });
            }

            var envelope = JsonSerializer.Deserialize<ApiEnvelope<object>>(payload, JsonOptions);
            return new JsonResult(new
            {
                success = envelope?.Success ?? true,
                message = envelope?.Msg ?? "Resume strategy deleted."
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete resume strategy {ResumeStrategyId}.", request.Id);
            return StatusCode(500, new { success = false, message = "Failed to delete resume strategy." });
        }
    }

    public async Task<IActionResult> OnPostCreateAreaAsync(
        [FromBody] AreaCreateInput request,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            var errors = string.Join("; ", ModelState.Values
                .SelectMany(v => v.Errors)
                .Select(e => e.ErrorMessage));

            return BadRequest(new { success = false, message = string.IsNullOrWhiteSpace(errors) ? "Invalid request." : errors });
        }

        var token = HttpContext.Session.GetString("JwtToken");
        if (string.IsNullOrEmpty(token))
        {
            return new JsonResult(new { success = false, message = "Session expired." }) { StatusCode = 401 };
        }

        try
        {
            using var client = CreateApiClient(token);
            var content = new StringContent(JsonSerializer.Serialize(request, JsonWriteOptions), Encoding.UTF8, "application/json");
            using var response = await client.PostAsync("api/v1/areas", content, cancellationToken);
            var payload = await response.Content.ReadAsStringAsync(cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                var message = ExtractErrorMessage(payload);
                return StatusCode((int)response.StatusCode, new { success = false, message });
            }

            var envelope = JsonSerializer.Deserialize<ApiEnvelope<AreaDto>>(payload, JsonOptions);
            return new JsonResult(new
            {
                success = envelope?.Success ?? false,
                data = envelope?.Data,
                message = envelope?.Msg ?? "Area created."
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create area.");
            return StatusCode(500, new { success = false, message = "Failed to create area." });
        }
    }

    public async Task<IActionResult> OnPostUpdateAreaAsync(
        [FromBody] AreaUpdateInput request,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            var errors = string.Join("; ", ModelState.Values
                .SelectMany(v => v.Errors)
                .Select(e => e.ErrorMessage));

            return BadRequest(new { success = false, message = string.IsNullOrWhiteSpace(errors) ? "Invalid request." : errors });
        }

        var token = HttpContext.Session.GetString("JwtToken");
        if (string.IsNullOrEmpty(token))
        {
            return new JsonResult(new { success = false, message = "Session expired." }) { StatusCode = 401 };
        }

        try
        {
            using var client = CreateApiClient(token);
            var content = new StringContent(JsonSerializer.Serialize(request, JsonWriteOptions), Encoding.UTF8, "application/json");
            using var response = await client.PutAsync($"api/v1/areas/{request.Id}", content, cancellationToken);
            var payload = await response.Content.ReadAsStringAsync(cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                var message = ExtractErrorMessage(payload);
                return StatusCode((int)response.StatusCode, new { success = false, message });
            }

            var envelope = JsonSerializer.Deserialize<ApiEnvelope<AreaDto>>(payload, JsonOptions);
            return new JsonResult(new
            {
                success = envelope?.Success ?? false,
                data = envelope?.Data,
                message = envelope?.Msg ?? "Area updated."
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update area {AreaId}.", request.Id);
            return StatusCode(500, new { success = false, message = "Failed to update area." });
        }
    }

    public async Task<IActionResult> OnPostDeleteAreaAsync(
        [FromBody] AreaDeleteInput request,
        CancellationToken cancellationToken)
    {
        if (request.Id <= 0)
        {
            return BadRequest(new { success = false, message = "Invalid area id." });
        }

        var token = HttpContext.Session.GetString("JwtToken");
        if (string.IsNullOrEmpty(token))
        {
            return new JsonResult(new { success = false, message = "Session expired." }) { StatusCode = 401 };
        }

        try
        {
            using var client = CreateApiClient(token);
            using var response = await client.DeleteAsync($"api/v1/areas/{request.Id}", cancellationToken);
            var payload = await response.Content.ReadAsStringAsync(cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                var message = ExtractErrorMessage(payload);
                return StatusCode((int)response.StatusCode, new { success = false, message });
            }

            var envelope = JsonSerializer.Deserialize<ApiEnvelope<object>>(payload, JsonOptions);
            return new JsonResult(new
            {
                success = envelope?.Success ?? true,
                message = envelope?.Msg ?? "Area deleted."
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete area {AreaId}.", request.Id);
            return StatusCode(500, new { success = false, message = "Failed to delete area." });
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

    private static string ExtractErrorMessage(string? payload)
    {
        if (string.IsNullOrWhiteSpace(payload))
        {
            return "An unexpected error occurred.";
        }

        try
        {
            using var document = JsonDocument.Parse(payload);
            var root = document.RootElement;

            if (root.TryGetProperty("detail", out var detail) && detail.ValueKind == JsonValueKind.String)
            {
                return detail.GetString() ?? "An unexpected error occurred.";
            }

            if (root.TryGetProperty("message", out var message) && message.ValueKind == JsonValueKind.String)
            {
                return message.GetString() ?? "An unexpected error occurred.";
            }

            if (root.TryGetProperty("msg", out var msg) && msg.ValueKind == JsonValueKind.String)
            {
                return msg.GetString() ?? "An unexpected error occurred.";
            }

            if (root.TryGetProperty("title", out var title) && title.ValueKind == JsonValueKind.String)
            {
                return title.GetString() ?? "An unexpected error occurred.";
            }
        }
        catch (JsonException)
        {
            // Ignore parsing errors and fall back to default message.
        }

        return "An unexpected error occurred.";
    }
}

public class ApiEnvelope<T>
{
    [JsonPropertyName("success")]
    public bool Success { get; set; }

    [JsonPropertyName("msg")]
    public string? Msg { get; set; }

    [JsonPropertyName("message")]
    public string? Message
    {
        get => Msg;
        set => Msg = value;
    }

    [JsonPropertyName("data")]
    public T? Data { get; set; }
}

public class MissionTypeDto
{
    public int Id { get; set; }
    public string DisplayName { get; set; } = string.Empty;
    public string ActualValue { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedUtc { get; set; }
    public DateTime UpdatedUtc { get; set; }
}

public class MissionTypeCreateInput
{
    [Required]
    [MaxLength(128)]
    public string DisplayName { get; set; } = string.Empty;

    [Required]
    [MaxLength(128)]
    public string ActualValue { get; set; } = string.Empty;

    [MaxLength(512)]
    public string? Description { get; set; }

    public bool IsActive { get; set; } = true;
}

public class MissionTypeUpdateInput : MissionTypeCreateInput
{
    [Range(1, int.MaxValue, ErrorMessage = "Invalid mission type id.")]
    public int Id { get; set; }
}

public class MissionTypeDeleteInput
{
    public int Id { get; set; }
}

public class RobotTypeDto
{
    public int Id { get; set; }
    public string DisplayName { get; set; } = string.Empty;
    public string ActualValue { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedUtc { get; set; }
    public DateTime UpdatedUtc { get; set; }
}

public class RobotTypeCreateInput
{
    [Required]
    [MaxLength(128)]
    public string DisplayName { get; set; } = string.Empty;

    [Required]
    [MaxLength(128)]
    public string ActualValue { get; set; } = string.Empty;

    [MaxLength(512)]
    public string? Description { get; set; }

    public bool IsActive { get; set; } = true;
}

public class RobotTypeUpdateInput : RobotTypeCreateInput
{
    [Range(1, int.MaxValue, ErrorMessage = "Invalid robot type id.")]
    public int Id { get; set; }
}

public class RobotTypeDeleteInput
{
    public int Id { get; set; }
}

public class ShelfDecisionRuleDto
{
    public int Id { get; set; }
    public string DisplayName { get; set; } = string.Empty;
    public string ActualValue { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedUtc { get; set; }
    public DateTime UpdatedUtc { get; set; }
}

public class ShelfDecisionRuleCreateInput
{
    [Required]
    [MaxLength(128)]
    public string DisplayName { get; set; } = string.Empty;

    [Required]
    [MaxLength(128)]
    public string ActualValue { get; set; } = string.Empty;

    [MaxLength(512)]
    public string? Description { get; set; }

    public bool IsActive { get; set; } = true;
}

public class ShelfDecisionRuleUpdateInput : ShelfDecisionRuleCreateInput
{
    [Range(1, int.MaxValue, ErrorMessage = "Invalid shelf decision rule id.")]
    public int Id { get; set; }
}

public class ShelfDecisionRuleDeleteInput
{
    public int Id { get; set; }
}

public class ResumeStrategyDto
{
    public int Id { get; set; }
    public string DisplayName { get; set; } = string.Empty;
    public string ActualValue { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedUtc { get; set; }
    public DateTime UpdatedUtc { get; set; }
}

public class ResumeStrategyCreateInput
{
    [Required]
    [MaxLength(128)]
    public string DisplayName { get; set; } = string.Empty;

    [Required]
    [MaxLength(128)]
    public string ActualValue { get; set; } = string.Empty;

    [MaxLength(512)]
    public string? Description { get; set; }

    public bool IsActive { get; set; } = true;
}

public class ResumeStrategyUpdateInput : ResumeStrategyCreateInput
{
    [Range(1, int.MaxValue, ErrorMessage = "Invalid resume strategy id.")]
    public int Id { get; set; }
}

public class ResumeStrategyDeleteInput
{
    public int Id { get; set; }
}

public class AreaDto
{
    public int Id { get; set; }
    public string DisplayName { get; set; } = string.Empty;
    public string ActualValue { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedUtc { get; set; }
    public DateTime UpdatedUtc { get; set; }
}

public class AreaCreateInput
{
    [Required]
    [MaxLength(128)]
    public string DisplayName { get; set; } = string.Empty;

    [Required]
    [MaxLength(128)]
    public string ActualValue { get; set; } = string.Empty;

    [MaxLength(512)]
    public string? Description { get; set; }

    public bool IsActive { get; set; } = true;
}

public class AreaUpdateInput : AreaCreateInput
{
    [Range(1, int.MaxValue, ErrorMessage = "Invalid area id.")]
    public int Id { get; set; }
}

public class AreaDeleteInput
{
    public int Id { get; set; }
}
