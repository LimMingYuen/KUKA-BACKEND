namespace QES_KUKA_AMR_API.Options;

public class RobotRealtimePollingOptions
{
    public const string SectionName = "RobotRealtimePolling";

    /// <summary>
    /// Polling interval in milliseconds. Default: 1000ms (1 second).
    /// </summary>
    public int PollingIntervalMs { get; set; } = 1000;

    /// <summary>
    /// If true, only broadcast positions that have changed significantly.
    /// Reduces network traffic but may affect smoothness. Default: false.
    /// </summary>
    public bool BroadcastOnlyChanges { get; set; } = false;

    /// <summary>
    /// Enable/disable the polling service. Default: true.
    /// </summary>
    public bool Enabled { get; set; } = true;
}
