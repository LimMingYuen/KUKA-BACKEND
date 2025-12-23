namespace QES_KUKA_AMR_API.Services.ErrorNotification;

/// <summary>
/// Service interface for sending error notification emails.
/// </summary>
public interface IErrorNotificationService
{
    /// <summary>
    /// Sends notification email when a mission submit operation fails.
    /// </summary>
    /// <param name="context">Error context with details about the failure</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task NotifyMissionSubmitErrorAsync(
        MissionErrorContext context,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Sends notification email when a job query operation fails.
    /// </summary>
    /// <param name="context">Error context with details about the failure</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task NotifyJobQueryErrorAsync(
        JobQueryErrorContext context,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Sends notification email when a robot query operation fails.
    /// </summary>
    /// <param name="context">Error context with details about the failure</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task NotifyRobotQueryErrorAsync(
        RobotQueryErrorContext context,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Context information for mission submit errors.
/// </summary>
public class MissionErrorContext
{
    /// <summary>
    /// Mission code that failed to submit
    /// </summary>
    public string MissionCode { get; set; } = string.Empty;

    /// <summary>
    /// Template code used for the mission (if applicable)
    /// </summary>
    public string? TemplateCode { get; set; }

    /// <summary>
    /// URL of the external API endpoint
    /// </summary>
    public string RequestUrl { get; set; } = string.Empty;

    /// <summary>
    /// JSON serialized request body
    /// </summary>
    public string RequestBody { get; set; } = string.Empty;

    /// <summary>
    /// Response body from the external API (if available)
    /// </summary>
    public string? ResponseBody { get; set; }

    /// <summary>
    /// HTTP status code returned (if available)
    /// </summary>
    public int? HttpStatusCode { get; set; }

    /// <summary>
    /// Error message describing the failure
    /// </summary>
    public string ErrorMessage { get; set; } = string.Empty;

    /// <summary>
    /// Exception stack trace (if available)
    /// </summary>
    public string? StackTrace { get; set; }

    /// <summary>
    /// Type of error (e.g., HttpRequestException, TimeoutException)
    /// </summary>
    public string ErrorType { get; set; } = string.Empty;

    /// <summary>
    /// UTC timestamp when the error occurred
    /// </summary>
    public DateTime OccurredUtc { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Context information for job query errors.
/// </summary>
public class JobQueryErrorContext
{
    /// <summary>
    /// Job code being queried (used for rate limiting)
    /// </summary>
    public string? JobCode { get; set; }

    /// <summary>
    /// URL of the external API endpoint
    /// </summary>
    public string RequestUrl { get; set; } = string.Empty;

    /// <summary>
    /// JSON serialized request body
    /// </summary>
    public string RequestBody { get; set; } = string.Empty;

    /// <summary>
    /// Response body from the external API (if available)
    /// </summary>
    public string? ResponseBody { get; set; }

    /// <summary>
    /// HTTP status code returned (if available)
    /// </summary>
    public int? HttpStatusCode { get; set; }

    /// <summary>
    /// Error message describing the failure
    /// </summary>
    public string ErrorMessage { get; set; } = string.Empty;

    /// <summary>
    /// Exception stack trace (if available)
    /// </summary>
    public string? StackTrace { get; set; }

    /// <summary>
    /// Type of error (e.g., HttpRequestException, TimeoutException)
    /// </summary>
    public string ErrorType { get; set; } = string.Empty;

    /// <summary>
    /// UTC timestamp when the error occurred
    /// </summary>
    public DateTime OccurredUtc { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Context information for robot query errors.
/// </summary>
public class RobotQueryErrorContext
{
    /// <summary>
    /// Robot ID being queried (if available)
    /// </summary>
    public string? RobotId { get; set; }

    /// <summary>
    /// Map code being queried (if available)
    /// </summary>
    public string? MapCode { get; set; }

    /// <summary>
    /// URL of the external API endpoint
    /// </summary>
    public string RequestUrl { get; set; } = string.Empty;

    /// <summary>
    /// JSON serialized request body
    /// </summary>
    public string RequestBody { get; set; } = string.Empty;

    /// <summary>
    /// Response body from the external API (if available)
    /// </summary>
    public string? ResponseBody { get; set; }

    /// <summary>
    /// HTTP status code returned (if available)
    /// </summary>
    public int? HttpStatusCode { get; set; }

    /// <summary>
    /// Error message describing the failure
    /// </summary>
    public string ErrorMessage { get; set; } = string.Empty;

    /// <summary>
    /// Exception stack trace (if available)
    /// </summary>
    public string? StackTrace { get; set; }

    /// <summary>
    /// Type of error (e.g., HttpRequestException, TimeoutException)
    /// </summary>
    public string ErrorType { get; set; } = string.Empty;

    /// <summary>
    /// UTC timestamp when the error occurred
    /// </summary>
    public DateTime OccurredUtc { get; set; } = DateTime.UtcNow;
}
