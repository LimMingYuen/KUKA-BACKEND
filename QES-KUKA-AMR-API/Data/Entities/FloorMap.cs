using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace QES_KUKA_AMR_API.Data.Entities;

/// <summary>
/// Represents floor/map metadata from imported map files
/// </summary>
[Table("FloorMaps")]
public class FloorMap
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    /// <summary>
    /// Map code identifier
    /// </summary>
    [Required]
    [MaxLength(128)]
    public string MapCode { get; set; } = string.Empty;

    /// <summary>
    /// Floor number
    /// </summary>
    [Required]
    [MaxLength(16)]
    public string FloorNumber { get; set; } = string.Empty;

    /// <summary>
    /// Display name for the floor
    /// </summary>
    [Required]
    [MaxLength(64)]
    public string FloorName { get; set; } = string.Empty;

    /// <summary>
    /// Floor level (e.g., 1, 2, 3)
    /// </summary>
    public int FloorLevel { get; set; }

    /// <summary>
    /// Floor length in meters
    /// </summary>
    public double FloorLength { get; set; }

    /// <summary>
    /// Floor width in meters
    /// </summary>
    public double FloorWidth { get; set; }

    /// <summary>
    /// Map version string
    /// </summary>
    [MaxLength(32)]
    public string? FloorMapVersion { get; set; }

    /// <summary>
    /// Laser map ID reference
    /// </summary>
    public int? LaserMapId { get; set; }

    /// <summary>
    /// Number of nodes on this floor
    /// </summary>
    public int NodeCount { get; set; }

    /// <summary>
    /// Number of edges on this floor
    /// </summary>
    public int EdgeCount { get; set; }

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
