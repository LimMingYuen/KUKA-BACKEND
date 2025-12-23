using System.Collections.Concurrent;
using Microsoft.Extensions.Options;
using QES_KUKA_AMR_API.Options;

namespace QES_KUKA_AMR_API.Services.ErrorNotification;

/// <summary>
/// Interface for rate limiting error notifications.
/// </summary>
public interface IErrorNotificationRateLimiter
{
    /// <summary>
    /// Checks if a notification should be sent for the given error key.
    /// Returns true if enough time has passed since the last notification.
    /// </summary>
    /// <param name="errorKey">Unique key identifying the error (e.g., "MissionError:MSN-001")</param>
    /// <returns>True if notification should be sent, false if rate limited</returns>
    bool ShouldSendNotification(string errorKey);

    /// <summary>
    /// Records that a notification was sent for the given error key.
    /// </summary>
    /// <param name="errorKey">Unique key identifying the error</param>
    void RecordNotification(string errorKey);
}

/// <summary>
/// Singleton service that rate limits error notifications to prevent email flooding.
/// Uses in-memory tracking with automatic cleanup of old entries.
/// </summary>
public class ErrorNotificationRateLimiter : IErrorNotificationRateLimiter, IDisposable
{
    private readonly ConcurrentDictionary<string, DateTime> _lastNotificationTimes = new();
    private readonly TimeSpan _rateLimitInterval;
    private readonly TimeSpan _cleanupInterval;
    private readonly ILogger<ErrorNotificationRateLimiter> _logger;
    private readonly Timer _cleanupTimer;
    private bool _disposed;

    public ErrorNotificationRateLimiter(
        IOptions<ErrorNotificationOptions> options,
        ILogger<ErrorNotificationRateLimiter> logger)
    {
        _logger = logger;
        _rateLimitInterval = TimeSpan.FromSeconds(options.Value.RateLimitIntervalSeconds);
        _cleanupInterval = TimeSpan.FromMinutes(options.Value.CleanupIntervalMinutes);

        _logger.LogInformation(
            "ErrorNotificationRateLimiter initialized with {IntervalSeconds}s rate limit interval",
            options.Value.RateLimitIntervalSeconds);

        // Start periodic cleanup timer
        _cleanupTimer = new Timer(
            CleanupOldEntries,
            null,
            _cleanupInterval,
            _cleanupInterval);
    }

    /// <inheritdoc />
    public bool ShouldSendNotification(string errorKey)
    {
        if (string.IsNullOrWhiteSpace(errorKey))
        {
            return true; // Allow notification if no key provided
        }

        var now = DateTime.UtcNow;

        if (_lastNotificationTimes.TryGetValue(errorKey, out var lastTime))
        {
            var elapsed = now - lastTime;
            if (elapsed < _rateLimitInterval)
            {
                _logger.LogDebug(
                    "Rate limited: {ErrorKey} - last notification {ElapsedSeconds}s ago (limit: {LimitSeconds}s)",
                    errorKey, elapsed.TotalSeconds, _rateLimitInterval.TotalSeconds);
                return false;
            }
        }

        return true;
    }

    /// <inheritdoc />
    public void RecordNotification(string errorKey)
    {
        if (string.IsNullOrWhiteSpace(errorKey))
        {
            return;
        }

        var now = DateTime.UtcNow;
        _lastNotificationTimes.AddOrUpdate(errorKey, now, (_, _) => now);

        _logger.LogDebug("Recorded notification for {ErrorKey} at {Time}", errorKey, now);
    }

    /// <summary>
    /// Removes entries older than 1 hour to prevent memory leaks.
    /// </summary>
    private void CleanupOldEntries(object? state)
    {
        var cutoff = DateTime.UtcNow.AddHours(-1);
        var removedCount = 0;

        foreach (var kvp in _lastNotificationTimes)
        {
            if (kvp.Value < cutoff)
            {
                if (_lastNotificationTimes.TryRemove(kvp.Key, out _))
                {
                    removedCount++;
                }
            }
        }

        if (removedCount > 0)
        {
            _logger.LogDebug(
                "Rate limiter cleanup: removed {Count} old entries, {Remaining} remaining",
                removedCount, _lastNotificationTimes.Count);
        }
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _cleanupTimer.Dispose();
        _disposed = true;
    }
}
