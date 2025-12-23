namespace QES_KUKA_AMR_API.Options;

/// <summary>
/// Configuration options for SMTP email service.
/// </summary>
public class SmtpOptions
{
    public const string SectionName = "Smtp";

    /// <summary>
    /// SMTP server hostname (e.g., smtp.gmail.com, smtp.office365.com)
    /// </summary>
    public string Host { get; set; } = string.Empty;

    /// <summary>
    /// SMTP server port (25, 465 for SSL, 587 for TLS)
    /// </summary>
    public int Port { get; set; } = 587;

    /// <summary>
    /// SMTP authentication username
    /// </summary>
    public string Username { get; set; } = string.Empty;

    /// <summary>
    /// SMTP authentication password
    /// </summary>
    public string Password { get; set; } = string.Empty;

    /// <summary>
    /// Sender email address
    /// </summary>
    public string FromAddress { get; set; } = string.Empty;

    /// <summary>
    /// Sender display name shown in email clients
    /// </summary>
    public string FromDisplayName { get; set; } = "KUKA AMR System";

    /// <summary>
    /// Enable SSL/TLS encryption
    /// </summary>
    public bool EnableSsl { get; set; } = true;

    /// <summary>
    /// Connection timeout in milliseconds
    /// </summary>
    public int TimeoutMs { get; set; } = 30000;

    /// <summary>
    /// Enable or disable email notifications globally
    /// </summary>
    public bool Enabled { get; set; } = true;
}
