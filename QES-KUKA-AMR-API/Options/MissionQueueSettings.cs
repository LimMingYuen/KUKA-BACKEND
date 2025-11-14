namespace QES_KUKA_AMR_API.Options;

public class MissionQueueSettings
{
    public int GlobalConcurrencyLimit { get; set; } = 30;

    public int AutoProcessIntervalSeconds { get; set; } = 5;

    public int JobStatusPollingIntervalSeconds { get; set; } = 10;

    public int RobotQueryPollingIntervalSeconds { get; set; } = 5;

    public int MissionSubmissionIntervalSeconds { get; set; } = 5;

    public int DefaultPriority { get; set; } = 5;
}
