namespace QES_KUKA_AMR_API.Options;

public class QueueSchedulerOptions
{
    public const string SectionName = "QueueScheduler";

    /// <summary>
    /// Enable/disable the queue scheduler
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// How often to process queues (in seconds)
    /// </summary>
    public int ProcessingIntervalSeconds { get; set; } = 5;

    /// <summary>
    /// How often to check for job completion (in seconds)
    /// </summary>
    public int CompletionCheckIntervalSeconds { get; set; } = 2;

    /// <summary>
    /// Maximum number of jobs to process per MapCode per cycle
    /// </summary>
    public int MaxJobsPerMapCodePerCycle { get; set; } = 5;

    /// <summary>
    /// Maximum retry attempts for failed job submissions
    /// </summary>
    public int MaxRetryAttempts { get; set; } = 3;

    /// <summary>
    /// Delay between retry attempts (in seconds)
    /// </summary>
    public int RetryDelaySeconds { get; set; } = 10;

    /// <summary>
    /// Enable opportunistic job evaluation after job completion
    /// </summary>
    public bool EnableOpportunisticJobEvaluation { get; set; } = true;
}
