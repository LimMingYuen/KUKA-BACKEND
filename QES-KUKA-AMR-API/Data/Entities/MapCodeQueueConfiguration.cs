using System.ComponentModel.DataAnnotations;

namespace QES_KUKA_AMR_API.Data.Entities;

/// <summary>
/// Per-MapCode queue configuration and optimization parameters
/// </summary>
public class MapCodeQueueConfiguration
{
    [Key]
    public int Id { get; set; }

    /// <summary>
    /// MapCode identifier (unique)
    /// </summary>
    [Required]
    [MaxLength(128)]
    public string MapCode { get; set; } = string.Empty;

    /// <summary>
    /// Enable/disable queue for this MapCode
    /// </summary>
    public bool EnableQueue { get; set; } = true;

    /// <summary>
    /// Default priority for jobs on this map
    /// </summary>
    public int DefaultPriority { get; set; } = 5;

    /// <summary>
    /// Maximum number of consecutive opportunistic jobs allowed (default: 1)
    /// Set to 0 to disable opportunistic jobs
    /// </summary>
    public int MaxConsecutiveOpportunisticJobs { get; set; } = 1;

    /// <summary>
    /// Enable cross-map job optimization
    /// </summary>
    public bool EnableCrossMapOptimization { get; set; } = true;

    /// <summary>
    /// Maximum concurrent robots allowed on this map (traffic limit)
    /// </summary>
    public int MaxConcurrentRobotsOnMap { get; set; } = 10;

    /// <summary>
    /// How often to process queue (in seconds)
    /// </summary>
    public int QueueProcessingIntervalSeconds { get; set; } = 5;

    /// <summary>
    /// Total jobs processed from this map's queue (statistics)
    /// </summary>
    public int TotalJobsProcessed { get; set; }

    /// <summary>
    /// Total opportunistic jobs chained (statistics)
    /// </summary>
    public int OpportunisticJobsChained { get; set; }

    /// <summary>
    /// Average job wait time in seconds (statistics)
    /// </summary>
    public double AverageJobWaitTimeSeconds { get; set; }

    /// <summary>
    /// Average distance of opportunistic jobs in meters (statistics)
    /// </summary>
    public double AverageOpportunisticJobDistanceMeters { get; set; }

    /// <summary>
    /// When this configuration was created
    /// </summary>
    [Required]
    public DateTime CreatedUtc { get; set; }

    /// <summary>
    /// When this configuration was last updated
    /// </summary>
    [Required]
    public DateTime UpdatedUtc { get; set; }
}
