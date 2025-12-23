namespace QES_KUKA_AMR_API.Options;

/// <summary>
/// Configuration options for error notification rate limiting.
/// </summary>
public class ErrorNotificationOptions
{
    public const string SectionName = "ErrorNotification";

    /// <summary>
    /// Minimum interval in seconds between sending duplicate notifications for the same error.
    /// Default: 60 seconds (1 minute).
    /// </summary>
    public int RateLimitIntervalSeconds { get; set; } = 60;

    /// <summary>
    /// Interval in minutes for cleaning up old rate limit entries.
    /// Default: 5 minutes.
    /// </summary>
    public int CleanupIntervalMinutes { get; set; } = 5;
}
