using System.ComponentModel.DataAnnotations;

namespace QES_KUKA_AMR_API.Data.Entities;

/// <summary>
/// Tracks opportunistic job eligibility and chaining limits for robots
/// </summary>
public class RobotJobOpportunity
{
    [Key]
    public int Id { get; set; }

    /// <summary>
    /// Robot identifier
    /// </summary>
    [Required]
    [MaxLength(100)]
    public string RobotId { get; set; } = string.Empty;

    /// <summary>
    /// Current MapCode where robot is operating
    /// </summary>
    [Required]
    [MaxLength(128)]
    public string CurrentMapCode { get; set; } = string.Empty;

    /// <summary>
    /// Robot's current X coordinate
    /// </summary>
    [Required]
    public double CurrentXCoordinate { get; set; }

    /// <summary>
    /// Robot's current Y coordinate
    /// </summary>
    [Required]
    public double CurrentYCoordinate { get; set; }

    /// <summary>
    /// When position was last updated
    /// </summary>
    [Required]
    public DateTime PositionUpdatedUtc { get; set; }

    /// <summary>
    /// Current/completed queue item ID
    /// </summary>
    [Required]
    public int CurrentQueueItemId { get; set; }

    /// <summary>
    /// When the mission was completed
    /// </summary>
    [Required]
    public DateTime MissionCompletedUtc { get; set; }

    /// <summary>
    /// Number of consecutive jobs completed in current MapCode
    /// </summary>
    [Required]
    public int ConsecutiveJobsInMapCode { get; set; }

    /// <summary>
    /// Original/home MapCode where robot started
    /// </summary>
    [Required]
    [MaxLength(128)]
    public string OriginalMapCode { get; set; } = string.Empty;

    /// <summary>
    /// When robot entered the current MapCode
    /// </summary>
    [Required]
    public DateTime EnteredMapCodeUtc { get; set; }

    /// <summary>
    /// Should system check for next opportunistic job
    /// </summary>
    public bool OpportunityCheckPending { get; set; }

    /// <summary>
    /// When opportunity was last evaluated
    /// </summary>
    public DateTime? OpportunityEvaluatedUtc { get; set; }

    /// <summary>
    /// ID of selected opportunistic job (if any)
    /// </summary>
    public int? SelectedOpportunisticJobId { get; set; }

    /// <summary>
    /// Decision made during opportunity evaluation
    /// </summary>
    [Required]
    public OpportunityDecision Decision { get; set; } = OpportunityDecision.Pending;

    /// <summary>
    /// Human-readable reason for decision
    /// </summary>
    [MaxLength(500)]
    public string? DecisionReason { get; set; }
}
