using System.Net;
using System.Net.Mail;
using Microsoft.Extensions.Options;
using QES_KUKA_AMR_API.Options;

namespace QES_KUKA_AMR_API.Services.Email;

/// <summary>
/// SMTP-based email service implementation.
/// </summary>
public class SmtpEmailService : IEmailService
{
    private readonly SmtpOptions _options;
    private readonly ILogger<SmtpEmailService> _logger;

    public SmtpEmailService(
        IOptions<SmtpOptions> options,
        ILogger<SmtpEmailService> logger)
    {
        _options = options.Value;
        _logger = logger;
    }

    /// <inheritdoc />
    public bool IsEnabled => _options.Enabled;

    /// <inheritdoc />
    public async Task<bool> SendEmailAsync(
        string toEmail,
        string toName,
        string subject,
        string htmlBody,
        CancellationToken cancellationToken = default)
    {
        if (!_options.Enabled)
        {
            _logger.LogWarning("SMTP is disabled. Email not sent to {Email}", toEmail);
            return false;
        }

        if (string.IsNullOrWhiteSpace(_options.Host))
        {
            _logger.LogWarning("SMTP host not configured. Email not sent to {Email}", toEmail);
            return false;
        }

        try
        {
            using var client = CreateSmtpClient();
            using var message = CreateMailMessage(toEmail, toName, subject, htmlBody);

            await client.SendMailAsync(message, cancellationToken);
            _logger.LogInformation("Email sent successfully to {Email}: {Subject}", toEmail, subject);
            return true;
        }
        catch (SmtpException ex)
        {
            _logger.LogError(ex, "SMTP error sending email to {Email}: {Subject}", toEmail, subject);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send email to {Email}: {Subject}", toEmail, subject);
            return false;
        }
    }

    /// <inheritdoc />
    public async Task<bool> SendEmailToRecipientsAsync(
        IEnumerable<(string Email, string Name)> recipients,
        string subject,
        string htmlBody,
        CancellationToken cancellationToken = default)
    {
        if (!_options.Enabled)
        {
            _logger.LogWarning("SMTP is disabled. Emails not sent.");
            return false;
        }

        var recipientList = recipients.ToList();
        if (recipientList.Count == 0)
        {
            _logger.LogWarning("No recipients specified for email: {Subject}", subject);
            return false;
        }

        var successCount = 0;
        var failCount = 0;

        foreach (var (email, name) in recipientList)
        {
            var success = await SendEmailAsync(email, name, subject, htmlBody, cancellationToken);
            if (success)
                successCount++;
            else
                failCount++;
        }

        _logger.LogInformation(
            "Email batch sent: {SuccessCount} succeeded, {FailCount} failed for subject: {Subject}",
            successCount, failCount, subject);

        return failCount == 0;
    }

    /// <inheritdoc />
    public async Task<bool> TestConnectionAsync(CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_options.Host))
        {
            _logger.LogWarning("SMTP host not configured for connection test");
            return false;
        }

        try
        {
            using var client = CreateSmtpClient();

            // SmtpClient validates connection settings on first operation
            // We'll do a simple NOOP-like test by creating a client and checking settings
            _logger.LogInformation(
                "SMTP connection test: Host={Host}, Port={Port}, SSL={EnableSsl}",
                _options.Host, _options.Port, _options.EnableSsl);

            // For a more thorough test, we could send to the from address
            // But that requires a valid email. For now, just validate configuration.
            await Task.CompletedTask;

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "SMTP connection test failed");
            return false;
        }
    }

    private SmtpClient CreateSmtpClient()
    {
        var client = new SmtpClient(_options.Host, _options.Port)
        {
            EnableSsl = _options.EnableSsl,
            Timeout = _options.TimeoutMs,
            DeliveryMethod = SmtpDeliveryMethod.Network
        };

        if (!string.IsNullOrEmpty(_options.Username))
        {
            client.Credentials = new NetworkCredential(_options.Username, _options.Password);
        }

        return client;
    }

    private MailMessage CreateMailMessage(string toEmail, string toName, string subject, string htmlBody)
    {
        var fromAddress = new MailAddress(_options.FromAddress, _options.FromDisplayName);
        var toAddress = new MailAddress(toEmail, toName);

        return new MailMessage(fromAddress, toAddress)
        {
            Subject = subject,
            Body = htmlBody,
            IsBodyHtml = true
        };
    }
}
