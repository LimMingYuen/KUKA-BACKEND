using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Text.Json;

namespace QES_KUKA_AMR_WEB.Pages;

[IgnoreAntiforgeryToken]
public class QueueMonitorModel : PageModel
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IConfiguration _configuration;
    private readonly ILogger<QueueMonitorModel> _logger;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public QueueMonitorModel(
        IHttpClientFactory httpClientFactory,
        IConfiguration configuration,
        ILogger<QueueMonitorModel> logger)
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

    public async Task<IActionResult> OnGetStatsAsync(CancellationToken cancellationToken)
    {
        var token = HttpContext.Session.GetString("JwtToken");
        if (string.IsNullOrEmpty(token))
        {
            return new JsonResult(new { success = false, message = "Session expired" }) { StatusCode = 401 };
        }

        try
        {
            using var client = CreateApiClient(token);
            var response = await client.GetAsync("api/mission-queue/stats", cancellationToken);
            var content = await response.Content.ReadAsStringAsync(cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                return StatusCode((int)response.StatusCode, new { success = false, message = "Failed to retrieve queue stats" });
            }

            var stats = JsonSerializer.Deserialize<QueueStatsDto>(content, JsonOptions);
            return new JsonResult(stats);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving queue stats");
            return StatusCode(500, new { success = false, message = "Error retrieving queue stats" });
        }
    }

    public async Task<IActionResult> OnGetQueueAsync(
        [FromQuery] string? status,
        [FromQuery] string? searchTerm,
        [FromQuery] string sortColumn = "CreatedDate",
        [FromQuery] string sortDirection = "asc",
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var token = HttpContext.Session.GetString("JwtToken");
        if (string.IsNullOrEmpty(token))
        {
            return new JsonResult(new { success = false, message = "Session expired" }) { StatusCode = 401 };
        }

        try
        {
            using var client = CreateApiClient(token);
            var url = string.IsNullOrEmpty(status) ? "api/mission-queue" : $"api/mission-queue?status={status}";
            var response = await client.GetAsync(url, cancellationToken);
            var content = await response.Content.ReadAsStringAsync(cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                return StatusCode((int)response.StatusCode, new { success = false, message = "Failed to retrieve queue" });
            }

            var allQueue = JsonSerializer.Deserialize<List<QueueItemDto>>(content, JsonOptions) ?? new List<QueueItemDto>();

            // Apply search filter
            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                var searchLower = searchTerm.ToLower();
                allQueue = allQueue.Where(q =>
                    q.MissionCode.ToLower().Contains(searchLower) ||
                    q.WorkflowName.ToLower().Contains(searchLower) ||
                    q.WorkflowCode.ToLower().Contains(searchLower) ||
                    q.RequestId.ToLower().Contains(searchLower) ||
                    q.CreatedBy.ToLower().Contains(searchLower)
                ).ToList();
            }

            // Apply sorting
            var sortedQueue = sortColumn.ToLower() switch
            {
                "id" => sortDirection == "asc"
                    ? allQueue.OrderBy(q => q.Id)
                    : allQueue.OrderByDescending(q => q.Id),
                "missioncode" => sortDirection == "asc"
                    ? allQueue.OrderBy(q => q.MissionCode)
                    : allQueue.OrderByDescending(q => q.MissionCode),
                "workflowname" => sortDirection == "asc"
                    ? allQueue.OrderBy(q => q.WorkflowName)
                    : allQueue.OrderByDescending(q => q.WorkflowName),
                "priority" => sortDirection == "asc"
                    ? allQueue.OrderBy(q => q.Priority)
                    : allQueue.OrderByDescending(q => q.Priority),
                "status" => sortDirection == "asc"
                    ? allQueue.OrderBy(q => q.Status)
                    : allQueue.OrderByDescending(q => q.Status),
                "createddate" => sortDirection == "asc"
                    ? allQueue.OrderBy(q => q.CreatedDate)
                    : allQueue.OrderByDescending(q => q.CreatedDate),
                "processeddate" => sortDirection == "asc"
                    ? allQueue.OrderBy(q => q.ProcessedDate ?? DateTime.MaxValue)
                    : allQueue.OrderByDescending(q => q.ProcessedDate ?? DateTime.MinValue),
                "createdby" => sortDirection == "asc"
                    ? allQueue.OrderBy(q => q.CreatedBy)
                    : allQueue.OrderByDescending(q => q.CreatedBy),
                _ => allQueue.OrderByDescending(q => q.CreatedDate)
            };

            // Get total count before pagination
            var totalCount = sortedQueue.Count();

            // Apply pagination
            var paginatedQueue = sortedQueue
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            // Return data with pagination metadata
            return new JsonResult(new
            {
                items = paginatedQueue,
                totalCount,
                page,
                pageSize,
                totalPages = (int)Math.Ceiling(totalCount / (double)pageSize)
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving queue");
            return StatusCode(500, new { success = false, message = "Error retrieving queue" });
        }
    }

    public async Task<IActionResult> OnPostUpdatePriorityAsync([FromBody] UpdatePriorityRequest request, CancellationToken cancellationToken)
    {
        var token = HttpContext.Session.GetString("JwtToken");
        if (string.IsNullOrEmpty(token))
        {
            return new JsonResult(new { success = false, message = "Session expired" }) { StatusCode = 401 };
        }

        try
        {
            using var client = CreateApiClient(token);
            var content = new StringContent(
                JsonSerializer.Serialize(new { Priority = request.Priority }, JsonOptions),
                System.Text.Encoding.UTF8,
                "application/json");

            var response = await client.PutAsync($"api/mission-queue/{request.QueueId}/priority", content, cancellationToken);
            var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                return StatusCode((int)response.StatusCode, new { success = false, message = "Failed to update priority" });
            }

            return new JsonResult(new { success = true, message = "Priority updated successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating priority for queue item {QueueId}", request.QueueId);
            return StatusCode(500, new { success = false, message = "Error updating priority" });
        }
    }

    public async Task<IActionResult> OnPostUpdateSmartPriorityAsync([FromBody] UpdateSmartPriorityRequest request, CancellationToken cancellationToken)
    {
        var token = HttpContext.Session.GetString("JwtToken");
        if (string.IsNullOrEmpty(token))
        {
            return new JsonResult(new { success = false, message = "Session expired" }) { StatusCode = 401 };
        }

        try
        {
            using var client = CreateApiClient(token);
            var content = new StringContent(
                JsonSerializer.Serialize(new { PriorityLevel = request.PriorityLevel }, JsonOptions),
                System.Text.Encoding.UTF8,
                "application/json");

            var response = await client.PutAsync($"api/mission-queue/{request.QueueId}/priority/smart", content, cancellationToken);
            var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                return StatusCode((int)response.StatusCode, new { success = false, message = "Failed to update priority" });
            }

            // Parse the response to get the assigned priority
            var apiResponse = JsonSerializer.Deserialize<SmartPriorityApiResponse>(responseContent, JsonOptions);

            return new JsonResult(new
            {
                success = true,
                assignedPriority = apiResponse?.AssignedPriority ?? 5,
                message = apiResponse?.Message ?? "Priority updated successfully"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating smart priority for queue item {QueueId}", request.QueueId);
            return StatusCode(500, new { success = false, message = "Error updating priority" });
        }
    }

    public async Task<IActionResult> OnDeleteRemoveAsync([FromQuery] int queueId, CancellationToken cancellationToken)
    {
        var token = HttpContext.Session.GetString("JwtToken");
        if (string.IsNullOrEmpty(token))
        {
            return new JsonResult(new { success = false, message = "Session expired" }) { StatusCode = 401 };
        }

        try
        {
            using var client = CreateApiClient(token);
            var response = await client.DeleteAsync($"api/mission-queue/{queueId}", cancellationToken);
            var content = await response.Content.ReadAsStringAsync(cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                return StatusCode((int)response.StatusCode, new { success = false, message = "Failed to remove mission from queue" });
            }

            return new JsonResult(new { success = true, message = "Mission removed from queue successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing queue item {QueueId}", queueId);
            return StatusCode(500, new { success = false, message = "Error removing mission from queue" });
        }
    }

    public async Task<IActionResult> OnPostProcessNextAsync(CancellationToken cancellationToken)
    {
        var token = HttpContext.Session.GetString("JwtToken");
        if (string.IsNullOrEmpty(token))
        {
            return new JsonResult(new { success = false, message = "Session expired" }) { StatusCode = 401 };
        }

        try
        {
            using var client = CreateApiClient(token);
            var response = await client.PostAsync("api/mission-queue/process-next", null, cancellationToken);
            var content = await response.Content.ReadAsStringAsync(cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                return StatusCode((int)response.StatusCode, new { success = false, message = "Failed to process next mission" });
            }

            var result = JsonSerializer.Deserialize<ApiResponse>(content, JsonOptions);
            return new JsonResult(new { success = true, message = result?.Message ?? "Processing initiated" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing next mission");
            return StatusCode(500, new { success = false, message = "Error processing next mission" });
        }
    }

    public async Task<IActionResult> OnDeleteClearCompletedAsync(CancellationToken cancellationToken)
    {
        var token = HttpContext.Session.GetString("JwtToken");
        if (string.IsNullOrEmpty(token))
        {
            return new JsonResult(new { success = false, message = "Session expired" }) { StatusCode = 401 };
        }

        try
        {
            using var client = CreateApiClient(token);
            var response = await client.DeleteAsync("api/mission-queue/clear-completed", cancellationToken);
            var content = await response.Content.ReadAsStringAsync(cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                return StatusCode((int)response.StatusCode, new { success = false, message = "Failed to clear completed missions" });
            }

            var result = JsonSerializer.Deserialize<ApiResponse>(content, JsonOptions);
            return new JsonResult(new { success = true, message = result?.Message ?? "Completed missions cleared" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error clearing completed missions");
            return StatusCode(500, new { success = false, message = "Error clearing completed missions" });
        }
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

// DTOs
public class QueueStatsDto
{
    public int QueuedCount { get; set; }
    public int ProcessingCount { get; set; }
    public int AvailableSlots { get; set; }
    public int TotalSlots { get; set; }
}

public class QueueItemDto
{
    public int Id { get; set; }
    public int? WorkflowId { get; set; }  // Nullable for custom missions
    public string WorkflowCode { get; set; } = string.Empty;
    public string WorkflowName { get; set; } = string.Empty;
    public string MissionCode { get; set; } = string.Empty;
    public string TemplateCode { get; set; } = string.Empty;
    public int Priority { get; set; }
    public string RequestId { get; set; } = string.Empty;
    public int Status { get; set; }
    public DateTime CreatedDate { get; set; }
    public DateTime? ProcessedDate { get; set; }
    public DateTime? CompletedDate { get; set; }
    public string? ErrorMessage { get; set; }
    public string CreatedBy { get; set; } = string.Empty;
    public int TriggerSource { get; set; }
    public string? TriggerSourceName { get; set; }
}

public class UpdatePriorityRequest
{
    public int QueueId { get; set; }
    public int Priority { get; set; }
}

public class UpdateSmartPriorityRequest
{
    public int QueueId { get; set; }
    public string PriorityLevel { get; set; } = string.Empty;
}

public class SmartPriorityApiResponse
{
    public int AssignedPriority { get; set; }
    public string Message { get; set; } = string.Empty;
}

public class ApiResponse
{
    public string? Message { get; set; }
}
