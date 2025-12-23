namespace QES_KUKA_AMR_API.Services.Email;

/// <summary>
/// Service interface for sending emails via SMTP.
/// </summary>
public interface IEmailService
{
    /// <summary>
    /// Sends an email to a single recipient.
    /// </summary>
    /// <param name="toEmail">Recipient email address</param>
    /// <param name="toName">Recipient display name</param>
    /// <param name="subject">Email subject</param>
    /// <param name="htmlBody">HTML body content</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if email was sent successfully, false otherwise</returns>
    Task<bool> SendEmailAsync(
        string toEmail,
        string toName,
        string subject,
        string htmlBody,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Sends an email to multiple recipients.
    /// </summary>
    /// <param name="recipients">List of (email, name) tuples</param>
    /// <param name="subject">Email subject</param>
    /// <param name="htmlBody">HTML body content</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if all emails were sent successfully, false if any failed</returns>
    Task<bool> SendEmailToRecipientsAsync(
        IEnumerable<(string Email, string Name)> recipients,
        string subject,
        string htmlBody,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Tests the SMTP connection with current configuration.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if connection test passed, false otherwise</returns>
    Task<bool> TestConnectionAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if the email service is enabled.
    /// </summary>
    bool IsEnabled { get; }
}
