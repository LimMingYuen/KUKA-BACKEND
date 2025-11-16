using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace QES_KUKA_AMR_API.Data.Entities;

/// <summary>
/// Stores the zone classification result for each workflow based on node code matching.
/// Each workflow can only be mapped to one zone at a time.
/// </summary>
[Table("WorkflowZoneMappings")]
public class WorkflowZoneMapping
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    /// <summary>
    /// The external workflow ID (unique - each workflow maps to one zone)
    /// </summary>
    [Required]
    public int ExternalWorkflowId { get; set; }

    /// <summary>
    /// The workflow code from WorkflowDiagram table
    /// </summary>
    [Required]
    [MaxLength(64)]
    public string WorkflowCode { get; set; } = string.Empty;

    /// <summary>
    /// The workflow name from WorkflowDiagram table
    /// </summary>
    [Required]
    [MaxLength(256)]
    public string WorkflowName { get; set; } = string.Empty;

    /// <summary>
    /// The zone name from MapZone table
    /// </summary>
    [Required]
    [MaxLength(256)]
    public string ZoneName { get; set; } = string.Empty;

    /// <summary>
    /// The zone code from MapZone table
    /// </summary>
    [Required]
    [MaxLength(128)]
    public string ZoneCode { get; set; } = string.Empty;

    /// <summary>
    /// The map code from MapZone table
    /// </summary>
    [MaxLength(128)]
    public string MapCode { get; set; } = string.Empty;

    /// <summary>
    /// Number of nodes that matched between workflow and zone
    /// </summary>
    public int MatchedNodesCount { get; set; }

    /// <summary>
    /// When this classification was created
    /// </summary>
    [Column(TypeName = "datetime2")]
    public DateTime CreatedUtc { get; set; }

    /// <summary>
    /// When this classification was last updated (re-classified)
    /// </summary>
    [Column(TypeName = "datetime2")]
    public DateTime UpdatedUtc { get; set; }
}
