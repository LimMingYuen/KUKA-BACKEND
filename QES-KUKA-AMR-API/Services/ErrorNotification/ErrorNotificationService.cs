using System.Net;
using Microsoft.EntityFrameworkCore;
using QES_KUKA_AMR_API.Data;
using QES_KUKA_AMR_API.Services.Email;

namespace QES_KUKA_AMR_API.Services.ErrorNotification;

/// <summary>
/// Service for sending error notification emails to configured recipients.
/// </summary>
public class ErrorNotificationService : IErrorNotificationService
{
    private readonly ApplicationDbContext _dbContext;
    private readonly IEmailService _emailService;
    private readonly IErrorNotificationRateLimiter _rateLimiter;
    private readonly ILogger<ErrorNotificationService> _logger;

    public ErrorNotificationService(
        ApplicationDbContext dbContext,
        IEmailService emailService,
        IErrorNotificationRateLimiter rateLimiter,
        ILogger<ErrorNotificationService> logger)
    {
        _dbContext = dbContext;
        _emailService = emailService;
        _rateLimiter = rateLimiter;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task NotifyMissionSubmitErrorAsync(
        MissionErrorContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (!_emailService.IsEnabled)
            {
                _logger.LogDebug("Email service disabled. Skipping mission error notification.");
                return;
            }

            // Rate limiting check
            var errorKey = $"MissionError:{context.MissionCode}";
            if (!_rateLimiter.ShouldSendNotification(errorKey))
            {
                _logger.LogDebug("Rate limited: Skipping duplicate notification for {ErrorKey}", errorKey);
                return;
            }

            var recipients = await GetActiveRecipientsAsync("MissionError", cancellationToken);
            if (recipients.Count == 0)
            {
                _logger.LogWarning("No active recipients for MissionError notifications");
                return;
            }

            var subject = $"[KUKA AMR ALERT] Mission Submit Error - {context.MissionCode}";
            var htmlBody = BuildMissionErrorEmailBody(context);

            await _emailService.SendEmailToRecipientsAsync(
                recipients.Select(r => (r.EmailAddress, r.DisplayName)),
                subject,
                htmlBody,
                cancellationToken);

            // Record notification after successful send
            _rateLimiter.RecordNotification(errorKey);

            _logger.LogInformation(
                "Mission error notification sent to {Count} recipients for mission {MissionCode}",
                recipients.Count, context.MissionCode);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Failed to send mission error notification for {MissionCode}",
                context.MissionCode);
        }
    }

    /// <inheritdoc />
    public async Task NotifyJobQueryErrorAsync(
        JobQueryErrorContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (!_emailService.IsEnabled)
            {
                _logger.LogDebug("Email service disabled. Skipping job query error notification.");
                return;
            }

            // Rate limiting check - use JobCode if available, otherwise use URL hash
            var errorKey = !string.IsNullOrWhiteSpace(context.JobCode)
                ? $"JobQueryError:{context.JobCode}"
                : $"JobQueryError:{context.RequestUrl.GetHashCode()}";

            if (!_rateLimiter.ShouldSendNotification(errorKey))
            {
                _logger.LogDebug("Rate limited: Skipping duplicate notification for {ErrorKey}", errorKey);
                return;
            }

            var recipients = await GetActiveRecipientsAsync("JobQueryError", cancellationToken);
            if (recipients.Count == 0)
            {
                _logger.LogWarning("No active recipients for JobQueryError notifications");
                return;
            }

            var subject = !string.IsNullOrWhiteSpace(context.JobCode)
                ? $"[KUKA AMR ALERT] Job Query Error - {context.JobCode}"
                : $"[KUKA AMR ALERT] Job Query Error - {context.OccurredUtc:yyyy-MM-dd HH:mm:ss} UTC";
            var htmlBody = BuildJobQueryErrorEmailBody(context);

            await _emailService.SendEmailToRecipientsAsync(
                recipients.Select(r => (r.EmailAddress, r.DisplayName)),
                subject,
                htmlBody,
                cancellationToken);

            // Record notification after successful send
            _rateLimiter.RecordNotification(errorKey);

            _logger.LogInformation(
                "Job query error notification sent to {Count} recipients for {ErrorKey}",
                recipients.Count, errorKey);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send job query error notification");
        }
    }

    /// <inheritdoc />
    public async Task NotifyRobotQueryErrorAsync(
        RobotQueryErrorContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (!_emailService.IsEnabled)
            {
                _logger.LogDebug("Email service disabled. Skipping robot query error notification.");
                return;
            }

            // Rate limiting check
            var errorKey = !string.IsNullOrWhiteSpace(context.RobotId)
                ? $"RobotQueryError:{context.RobotId}"
                : $"RobotQueryError:{context.RequestUrl.GetHashCode()}";

            if (!_rateLimiter.ShouldSendNotification(errorKey))
            {
                _logger.LogDebug("Rate limited: Skipping duplicate notification for {ErrorKey}", errorKey);
                return;
            }

            var recipients = await GetActiveRecipientsAsync("RobotQueryError", cancellationToken);
            if (recipients.Count == 0)
            {
                _logger.LogWarning("No active recipients for RobotQueryError notifications");
                return;
            }

            var subject = $"[KUKA AMR ALERT] Robot Query Error - {context.RobotId ?? "Unknown"} - {context.OccurredUtc:yyyy-MM-dd HH:mm:ss} UTC";
            var htmlBody = BuildRobotQueryErrorEmailBody(context);

            await _emailService.SendEmailToRecipientsAsync(
                recipients.Select(r => (r.EmailAddress, r.DisplayName)),
                subject,
                htmlBody,
                cancellationToken);

            // Record notification after successful send
            _rateLimiter.RecordNotification(errorKey);

            _logger.LogInformation(
                "Robot query error notification sent to {Count} recipients for robot {RobotId}",
                recipients.Count, context.RobotId ?? "Unknown");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Failed to send robot query error notification for robot {RobotId}",
                context.RobotId);
        }
    }

    private async Task<List<Data.Entities.EmailRecipient>> GetActiveRecipientsAsync(
        string notificationType,
        CancellationToken cancellationToken)
    {
        return await _dbContext.EmailRecipients
            .AsNoTracking()
            .Where(r => r.IsActive && r.NotificationTypes.Contains(notificationType))
            .ToListAsync(cancellationToken);
    }

    private static string BuildMissionErrorEmailBody(MissionErrorContext context)
    {
        var escapedErrorMessage = WebUtility.HtmlEncode(context.ErrorMessage);
        var escapedRequestBody = WebUtility.HtmlEncode(context.RequestBody);
        var escapedResponseBody = context.ResponseBody != null
            ? WebUtility.HtmlEncode(context.ResponseBody)
            : null;
        var escapedStackTrace = context.StackTrace != null
            ? WebUtility.HtmlEncode(context.StackTrace)
            : null;

        return $@"
<!DOCTYPE html>
<html>
<head>
    <style>
        body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; margin: 0; padding: 0; }}
        .header {{ background-color: #dc3545; color: white; padding: 20px; text-align: center; }}
        .header h1 {{ margin: 0; font-size: 24px; }}
        .content {{ padding: 20px; }}
        .section {{ margin-bottom: 20px; border: 1px solid #ddd; border-radius: 4px; overflow: hidden; }}
        .section-header {{ background-color: #f8f9fa; padding: 12px 15px; font-weight: bold; border-bottom: 1px solid #ddd; }}
        .section-body {{ padding: 15px; }}
        pre {{ background-color: #f4f4f4; padding: 12px; overflow-x: auto; font-size: 12px; margin: 0; white-space: pre-wrap; word-wrap: break-word; }}
        .info-table {{ width: 100%; border-collapse: collapse; }}
        .info-table td {{ padding: 8px 12px; border-bottom: 1px solid #eee; }}
        .info-table td:first-child {{ font-weight: bold; width: 150px; color: #666; }}
        .footer {{ background-color: #f8f9fa; padding: 15px; text-align: center; font-size: 12px; color: #666; }}
    </style>
</head>
<body>
    <div class='header'>
        <h1>Mission Submit Error Alert</h1>
    </div>
    <div class='content'>
        <div class='section'>
            <div class='section-header'>Error Summary</div>
            <div class='section-body'>
                <table class='info-table'>
                    <tr><td>Error Type:</td><td>{WebUtility.HtmlEncode(context.ErrorType)}</td></tr>
                    <tr><td>Mission Code:</td><td>{WebUtility.HtmlEncode(context.MissionCode)}</td></tr>
                    <tr><td>Template Code:</td><td>{WebUtility.HtmlEncode(context.TemplateCode ?? "N/A")}</td></tr>
                    <tr><td>HTTP Status:</td><td>{context.HttpStatusCode?.ToString() ?? "N/A"}</td></tr>
                    <tr><td>Occurred At:</td><td>{context.OccurredUtc:yyyy-MM-dd HH:mm:ss} UTC</td></tr>
                </table>
            </div>
        </div>

        <div class='section'>
            <div class='section-header'>Error Message</div>
            <div class='section-body'>
                <pre>{escapedErrorMessage}</pre>
            </div>
        </div>

        <div class='section'>
            <div class='section-header'>Request Details</div>
            <div class='section-body'>
                <p><strong>URL:</strong> {WebUtility.HtmlEncode(context.RequestUrl)}</p>
                <p><strong>Request Body:</strong></p>
                <pre>{escapedRequestBody}</pre>
            </div>
        </div>

        {(escapedResponseBody != null ? $@"
        <div class='section'>
            <div class='section-header'>Response Body</div>
            <div class='section-body'>
                <pre>{escapedResponseBody}</pre>
            </div>
        </div>" : "")}

        {(escapedStackTrace != null ? $@"
        <div class='section'>
            <div class='section-header'>Stack Trace</div>
            <div class='section-body'>
                <pre>{escapedStackTrace}</pre>
            </div>
        </div>" : "")}

        <div class='section'>
            <div class='section-header'>System Information</div>
            <div class='section-body'>
                <table class='info-table'>
                    <tr><td>Server:</td><td>{WebUtility.HtmlEncode(Environment.MachineName)}</td></tr>
                    <tr><td>Environment:</td><td>{WebUtility.HtmlEncode(Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Unknown")}</td></tr>
                    <tr><td>OS:</td><td>{WebUtility.HtmlEncode(Environment.OSVersion.ToString())}</td></tr>
                    <tr><td>.NET Version:</td><td>{WebUtility.HtmlEncode(Environment.Version.ToString())}</td></tr>
                </table>
            </div>
        </div>
    </div>
    <div class='footer'>
        This is an automated message from KUKA AMR System. Please do not reply to this email.
    </div>
</body>
</html>";
    }

    private static string BuildJobQueryErrorEmailBody(JobQueryErrorContext context)
    {
        var escapedErrorMessage = WebUtility.HtmlEncode(context.ErrorMessage);
        var escapedRequestBody = WebUtility.HtmlEncode(context.RequestBody);
        var escapedResponseBody = context.ResponseBody != null
            ? WebUtility.HtmlEncode(context.ResponseBody)
            : null;
        var escapedStackTrace = context.StackTrace != null
            ? WebUtility.HtmlEncode(context.StackTrace)
            : null;

        return $@"
<!DOCTYPE html>
<html>
<head>
    <style>
        body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; margin: 0; padding: 0; }}
        .header {{ background-color: #dc3545; color: white; padding: 20px; text-align: center; }}
        .header h1 {{ margin: 0; font-size: 24px; }}
        .content {{ padding: 20px; }}
        .section {{ margin-bottom: 20px; border: 1px solid #ddd; border-radius: 4px; overflow: hidden; }}
        .section-header {{ background-color: #f8f9fa; padding: 12px 15px; font-weight: bold; border-bottom: 1px solid #ddd; }}
        .section-body {{ padding: 15px; }}
        pre {{ background-color: #f4f4f4; padding: 12px; overflow-x: auto; font-size: 12px; margin: 0; white-space: pre-wrap; word-wrap: break-word; }}
        .info-table {{ width: 100%; border-collapse: collapse; }}
        .info-table td {{ padding: 8px 12px; border-bottom: 1px solid #eee; }}
        .info-table td:first-child {{ font-weight: bold; width: 150px; color: #666; }}
        .footer {{ background-color: #f8f9fa; padding: 15px; text-align: center; font-size: 12px; color: #666; }}
    </style>
</head>
<body>
    <div class='header'>
        <h1>Job Query Error Alert</h1>
    </div>
    <div class='content'>
        <div class='section'>
            <div class='section-header'>Error Summary</div>
            <div class='section-body'>
                <table class='info-table'>
                    <tr><td>Error Type:</td><td>{WebUtility.HtmlEncode(context.ErrorType)}</td></tr>
                    <tr><td>HTTP Status:</td><td>{context.HttpStatusCode?.ToString() ?? "N/A"}</td></tr>
                    <tr><td>Occurred At:</td><td>{context.OccurredUtc:yyyy-MM-dd HH:mm:ss} UTC</td></tr>
                </table>
            </div>
        </div>

        <div class='section'>
            <div class='section-header'>Error Message</div>
            <div class='section-body'>
                <pre>{escapedErrorMessage}</pre>
            </div>
        </div>

        <div class='section'>
            <div class='section-header'>Request Details</div>
            <div class='section-body'>
                <p><strong>URL:</strong> {WebUtility.HtmlEncode(context.RequestUrl)}</p>
                <p><strong>Request Body:</strong></p>
                <pre>{escapedRequestBody}</pre>
            </div>
        </div>

        {(escapedResponseBody != null ? $@"
        <div class='section'>
            <div class='section-header'>Response Body</div>
            <div class='section-body'>
                <pre>{escapedResponseBody}</pre>
            </div>
        </div>" : "")}

        {(escapedStackTrace != null ? $@"
        <div class='section'>
            <div class='section-header'>Stack Trace</div>
            <div class='section-body'>
                <pre>{escapedStackTrace}</pre>
            </div>
        </div>" : "")}

        <div class='section'>
            <div class='section-header'>System Information</div>
            <div class='section-body'>
                <table class='info-table'>
                    <tr><td>Server:</td><td>{WebUtility.HtmlEncode(Environment.MachineName)}</td></tr>
                    <tr><td>Environment:</td><td>{WebUtility.HtmlEncode(Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Unknown")}</td></tr>
                    <tr><td>OS:</td><td>{WebUtility.HtmlEncode(Environment.OSVersion.ToString())}</td></tr>
                    <tr><td>.NET Version:</td><td>{WebUtility.HtmlEncode(Environment.Version.ToString())}</td></tr>
                </table>
            </div>
        </div>
    </div>
    <div class='footer'>
        This is an automated message from KUKA AMR System. Please do not reply to this email.
    </div>
</body>
</html>";
    }

    private static string BuildRobotQueryErrorEmailBody(RobotQueryErrorContext context)
    {
        var escapedErrorMessage = WebUtility.HtmlEncode(context.ErrorMessage);
        var escapedRequestBody = WebUtility.HtmlEncode(context.RequestBody);
        var escapedResponseBody = context.ResponseBody != null
            ? WebUtility.HtmlEncode(context.ResponseBody)
            : null;
        var escapedStackTrace = context.StackTrace != null
            ? WebUtility.HtmlEncode(context.StackTrace)
            : null;

        return $@"
<!DOCTYPE html>
<html>
<head>
    <style>
        body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; margin: 0; padding: 0; }}
        .header {{ background-color: #dc3545; color: white; padding: 20px; text-align: center; }}
        .header h1 {{ margin: 0; font-size: 24px; }}
        .content {{ padding: 20px; }}
        .section {{ margin-bottom: 20px; border: 1px solid #ddd; border-radius: 4px; overflow: hidden; }}
        .section-header {{ background-color: #f8f9fa; padding: 12px 15px; font-weight: bold; border-bottom: 1px solid #ddd; }}
        .section-body {{ padding: 15px; }}
        pre {{ background-color: #f4f4f4; padding: 12px; overflow-x: auto; font-size: 12px; margin: 0; white-space: pre-wrap; word-wrap: break-word; }}
        .info-table {{ width: 100%; border-collapse: collapse; }}
        .info-table td {{ padding: 8px 12px; border-bottom: 1px solid #eee; }}
        .info-table td:first-child {{ font-weight: bold; width: 150px; color: #666; }}
        .footer {{ background-color: #f8f9fa; padding: 15px; text-align: center; font-size: 12px; color: #666; }}
    </style>
</head>
<body>
    <div class='header'>
        <h1>Robot Query Error Alert</h1>
    </div>
    <div class='content'>
        <div class='section'>
            <div class='section-header'>Error Summary</div>
            <div class='section-body'>
                <table class='info-table'>
                    <tr><td>Error Type:</td><td>{WebUtility.HtmlEncode(context.ErrorType)}</td></tr>
                    <tr><td>Robot ID:</td><td>{WebUtility.HtmlEncode(context.RobotId ?? "N/A")}</td></tr>
                    <tr><td>Map Code:</td><td>{WebUtility.HtmlEncode(context.MapCode ?? "N/A")}</td></tr>
                    <tr><td>HTTP Status:</td><td>{context.HttpStatusCode?.ToString() ?? "N/A"}</td></tr>
                    <tr><td>Occurred At:</td><td>{context.OccurredUtc:yyyy-MM-dd HH:mm:ss} UTC</td></tr>
                </table>
            </div>
        </div>

        <div class='section'>
            <div class='section-header'>Error Message</div>
            <div class='section-body'>
                <pre>{escapedErrorMessage}</pre>
            </div>
        </div>

        <div class='section'>
            <div class='section-header'>Request Details</div>
            <div class='section-body'>
                <p><strong>URL:</strong> {WebUtility.HtmlEncode(context.RequestUrl)}</p>
                <p><strong>Request Body:</strong></p>
                <pre>{escapedRequestBody}</pre>
            </div>
        </div>

        {(escapedResponseBody != null ? $@"
        <div class='section'>
            <div class='section-header'>Response Body</div>
            <div class='section-body'>
                <pre>{escapedResponseBody}</pre>
            </div>
        </div>" : "")}

        {(escapedStackTrace != null ? $@"
        <div class='section'>
            <div class='section-header'>Stack Trace</div>
            <div class='section-body'>
                <pre>{escapedStackTrace}</pre>
            </div>
        </div>" : "")}

        <div class='section'>
            <div class='section-header'>System Information</div>
            <div class='section-body'>
                <table class='info-table'>
                    <tr><td>Server:</td><td>{WebUtility.HtmlEncode(Environment.MachineName)}</td></tr>
                    <tr><td>Environment:</td><td>{WebUtility.HtmlEncode(Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Unknown")}</td></tr>
                    <tr><td>OS:</td><td>{WebUtility.HtmlEncode(Environment.OSVersion.ToString())}</td></tr>
                    <tr><td>.NET Version:</td><td>{WebUtility.HtmlEncode(Environment.Version.ToString())}</td></tr>
                </table>
            </div>
        </div>
    </div>
    <div class='footer'>
        This is an automated message from KUKA AMR System. Please do not reply to this email.
    </div>
</body>
</html>";
    }
}
