using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace QES_KUKA_AMR_API.Data.Entities;

/// <summary>
/// Represents a saved robot monitoring map configuration.
/// Users can create multiple map configurations with different background images,
/// coordinate settings, and display preferences.
/// </summary>
[Table("RobotMonitoringMaps")]
public class RobotMonitoringMap
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    /// <summary>
    /// Display name for the map configuration
    /// </summary>
    [Required]
    [MaxLength(256)]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Optional description of the map configuration
    /// </summary>
    [MaxLength(1000)]
    public string? Description { get; set; }

    /// <summary>
    /// Filter for QrCodes/MapZones by map code
    /// </summary>
    [MaxLength(128)]
    public string? MapCode { get; set; }

    /// <summary>
    /// Filter for QrCodes/MapZones by floor number
    /// </summary>
    [MaxLength(16)]
    public string? FloorNumber { get; set; }

    /// <summary>
    /// Relative path to the background image file (stored in wwwroot/uploads/maps/)
    /// </summary>
    [MaxLength(512)]
    public string? BackgroundImagePath { get; set; }

    /// <summary>
    /// Original filename of the uploaded background image
    /// </summary>
    [MaxLength(256)]
    public string? BackgroundImageOriginalName { get; set; }

    /// <summary>
    /// Width of the background image in pixels
    /// </summary>
    public int? ImageWidth { get; set; }

    /// <summary>
    /// Height of the background image in pixels
    /// </summary>
    public int? ImageHeight { get; set; }

    /// <summary>
    /// JSON serialized CoordinateSettings (scaleX, scaleY, offsetX, offsetY, rotation)
    /// </summary>
    [Column(TypeName = "nvarchar(max)")]
    public string? CoordinateSettingsJson { get; set; }

    /// <summary>
    /// JSON serialized DisplaySettings (showNodes, showZones, showLabels, nodeSize, robotSize)
    /// </summary>
    [Column(TypeName = "nvarchar(max)")]
    public string? DisplaySettingsJson { get; set; }

    /// <summary>
    /// JSON serialized custom nodes placed by user (id, label, x, y, color)
    /// </summary>
    [Column(TypeName = "nvarchar(max)")]
    public string? CustomNodesJson { get; set; }

    /// <summary>
    /// JSON serialized custom zones drawn by user (id, name, color, opacity, points)
    /// </summary>
    [Column(TypeName = "nvarchar(max)")]
    public string? CustomZonesJson { get; set; }

    /// <summary>
    /// JSON serialized custom lines connecting nodes (id, fromNodeId, toNodeId, color, weight)
    /// </summary>
    [Column(TypeName = "nvarchar(max)")]
    public string? CustomLinesJson { get; set; }

    /// <summary>
    /// Refresh interval for robot position updates in milliseconds
    /// </summary>
    public int RefreshIntervalMs { get; set; } = 5000;

    /// <summary>
    /// Whether this is the default map configuration to load
    /// </summary>
    public bool IsDefault { get; set; }

    /// <summary>
    /// Username of the user who created this configuration
    /// </summary>
    [Required]
    [MaxLength(100)]
    public string CreatedBy { get; set; } = string.Empty;

    /// <summary>
    /// UTC timestamp when the configuration was created
    /// </summary>
    [Required]
    public DateTime CreatedUtc { get; set; }

    /// <summary>
    /// Username of the user who last updated this configuration
    /// </summary>
    [MaxLength(100)]
    public string? LastUpdatedBy { get; set; }

    /// <summary>
    /// UTC timestamp when the configuration was last updated
    /// </summary>
    public DateTime? LastUpdatedUtc { get; set; }
}
