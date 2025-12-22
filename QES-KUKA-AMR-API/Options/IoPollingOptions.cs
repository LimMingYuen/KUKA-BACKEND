namespace QES_KUKA_AMR_API.Options;

/// <summary>
/// Configuration options for IO Controller polling service.
/// </summary>
public class IoPollingOptions
{
    public const string SectionName = "IoPolling";

    /// <summary>
    /// Whether the IO polling service is enabled.
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Default polling interval in milliseconds.
    /// Individual devices can override this.
    /// </summary>
    public int DefaultPollingIntervalMs { get; set; } = 1000;

    /// <summary>
    /// Connection timeout for Modbus TCP connections in milliseconds.
    /// </summary>
    public int ConnectionTimeoutMs { get; set; } = 3000;

    /// <summary>
    /// Whether to use demand-based polling (only poll when SignalR clients are connected).
    /// </summary>
    public bool DemandBasedPolling { get; set; } = true;

    /// <summary>
    /// Maximum number of concurrent device polling operations.
    /// </summary>
    public int MaxConcurrentPolls { get; set; } = 10;
}
