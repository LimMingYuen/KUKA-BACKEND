using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using QES_KUKA_AMR_API.Models.Analytics;

namespace QES_KUKA_AMR_API.Services.Analytics;

public interface IWorkflowAnalyticsService
{
    Task<List<WorkflowExecutionRecord>> GetWorkflowExecutionsAsync(
        string robotId,
        DateTime periodStartUtc,
        DateTime periodEndUtc,
        CancellationToken cancellationToken = default);
}

public class WorkflowAnalyticsService : IWorkflowAnalyticsService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<WorkflowAnalyticsService> _logger;
    private readonly JsonSerializerOptions _jsonOptions;

    public WorkflowAnalyticsService(
        HttpClient httpClient,
        ILogger<WorkflowAnalyticsService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;

        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };
    }

    public async Task<List<WorkflowExecutionRecord>> GetWorkflowExecutionsAsync(
        string robotId,
        DateTime periodStartUtc,
        DateTime periodEndUtc,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation(
                "Fetching workflow executions for robot {RobotId} from {Start} to {End}",
                robotId, periodStartUtc, periodEndUtc);

            // Query JobQuery API
            var request = new
            {
                RobotId = robotId,
                Limit = 1000
            };

            var content = new StringContent(
                JsonSerializer.Serialize(request, _jsonOptions),
                Encoding.UTF8,
                "application/json");

            using var response = await _httpClient.PostAsync("api/missions/jobs/query", content, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning(
                    "Failed to fetch workflow executions. Status: {StatusCode}",
                    response.StatusCode);
                return new List<WorkflowExecutionRecord>();
            }

            var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);

            var jobResponse = JsonSerializer.Deserialize<JobQueryResponseDto>(
                responseContent, _jsonOptions);

            if (jobResponse?.Data == null)
            {
                _logger.LogWarning("No workflow data returned from API");
                return new List<WorkflowExecutionRecord>();
            }

            var workflows = jobResponse.Data
                .Where(job => !string.IsNullOrEmpty(job.RobotId) &&
                              job.RobotId.Equals(robotId, StringComparison.OrdinalIgnoreCase))
                .Where(job => IsWithinPeriod(job.CompleteTime, periodStartUtc, periodEndUtc))
                .Select(job => new WorkflowExecutionRecord
                {
                    JobCode = job.JobCode,
                    RobotId = job.RobotId ?? string.Empty,
                    Status = job.Status,
                    WorkflowName = job.WorkflowName,
                    CompleteTime = job.CompleteTime,
                    SpendTime = job.SpendTime ?? 0
                })
                .OrderByDescending(w => w.CompleteTime)
                .ToList();

            _logger.LogInformation(
                "Retrieved {Count} workflow executions for robot {RobotId}",
                workflows.Count, robotId);

            return workflows;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Error retrieving workflow executions for robot {RobotId}",
                robotId);
            return new List<WorkflowExecutionRecord>();
        }
    }

    private static bool IsWithinPeriod(string? completeTime, DateTime start, DateTime end)
    {
        if (string.IsNullOrEmpty(completeTime))
            return false;

        if (DateTime.TryParse(completeTime, out var completed))
        {
            completed = completed.ToUniversalTime();
            return completed >= start && completed <= end;
        }

        return false;
    }
}
