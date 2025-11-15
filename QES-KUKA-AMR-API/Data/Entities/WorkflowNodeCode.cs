using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace QES_KUKA_AMR_API.Data.Entities;

/// <summary>
/// Stores the mapping between workflow external IDs and their associated node codes
/// from the external AMR system API
/// </summary>
[Table("WorkflowNodeCodes")]
public class WorkflowNodeCode
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    /// <summary>
    /// The workflow ID from the external AMR system (corresponds to WorkflowDiagram.ExternalWorkflowId)
    /// </summary>
    [Required]
    public int ExternalWorkflowId { get; set; }

    /// <summary>
    /// The node code (QR code position identifier) associated with this workflow
    /// </summary>
    [Required]
    [MaxLength(128)]
    public string NodeCode { get; set; } = string.Empty;

    /// <summary>
    /// When this record was created
    /// </summary>
    [Column(TypeName = "datetime2")]
    public DateTime CreatedUtc { get; set; }

    /// <summary>
    /// When this record was last updated
    /// </summary>
    [Column(TypeName = "datetime2")]
    public DateTime UpdatedUtc { get; set; }
}
