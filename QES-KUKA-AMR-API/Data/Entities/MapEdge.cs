using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace QES_KUKA_AMR_API.Data.Entities;

/// <summary>
/// Represents an edge (path) between two nodes on the warehouse map
/// </summary>
[Table("MapEdges")]
public class MapEdge
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    /// <summary>
    /// Starting node label (references QrCode.NodeLabel)
    /// </summary>
    [Required]
    [MaxLength(64)]
    public string BeginNodeLabel { get; set; } = string.Empty;

    /// <summary>
    /// Ending node label (references QrCode.NodeLabel)
    /// </summary>
    [Required]
    [MaxLength(64)]
    public string EndNodeLabel { get; set; } = string.Empty;

    /// <summary>
    /// Map code this edge belongs to
    /// </summary>
    [Required]
    [MaxLength(128)]
    public string MapCode { get; set; } = string.Empty;

    /// <summary>
    /// Floor number this edge is on
    /// </summary>
    [Required]
    [MaxLength(16)]
    public string FloorNumber { get; set; } = string.Empty;

    /// <summary>
    /// Length of the edge in meters
    /// </summary>
    public double EdgeLength { get; set; }

    /// <summary>
    /// Type of edge (1=standard, etc.)
    /// </summary>
    public int EdgeType { get; set; }

    /// <summary>
    /// Weight/cost for path planning
    /// </summary>
    public double EdgeWeight { get; set; }

    /// <summary>
    /// Width of the edge path in meters
    /// </summary>
    public double EdgeWidth { get; set; }

    /// <summary>
    /// Maximum velocity allowed on this edge (m/s)
    /// </summary>
    public double MaxVelocity { get; set; }

    /// <summary>
    /// Maximum acceleration velocity (m/s²)
    /// </summary>
    public double MaxAccelerationVelocity { get; set; }

    /// <summary>
    /// Maximum deceleration velocity (m/s²)
    /// </summary>
    public double MaxDecelerationVelocity { get; set; }

    /// <summary>
    /// Orientation in degrees (0=N, 90=E, 180=S, 270=W)
    /// </summary>
    public int Orientation { get; set; }

    /// <summary>
    /// Turn radius for curved edges
    /// </summary>
    public double Radius { get; set; }

    /// <summary>
    /// Road type classification
    /// </summary>
    [MaxLength(32)]
    public string? RoadType { get; set; }

    /// <summary>
    /// Status: 1=enabled, 0=disabled
    /// </summary>
    public int Status { get; set; } = 1;

    /// <summary>
    /// Creation timestamp
    /// </summary>
    [Required]
    public DateTime CreateTime { get; set; }

    /// <summary>
    /// Last update timestamp
    /// </summary>
    [Required]
    public DateTime LastUpdateTime { get; set; }
}
