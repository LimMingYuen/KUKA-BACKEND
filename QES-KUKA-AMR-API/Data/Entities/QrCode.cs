using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace QES_KUKA_AMR_API.Data.Entities;

[Table("QrCodes")]
public class QrCode
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    /// <summary>
    /// The QR code ID from the external AMR system API
    /// </summary>
    public int? ExternalQrCodeId { get; set; }

    [Required]
    public DateTime CreateTime { get; set; }

    [Required]
    [MaxLength(128)]
    public string CreateBy { get; set; } = string.Empty;

    [Required]
    [MaxLength(128)]
    public string CreateApp { get; set; } = string.Empty;

    [Required]
    public DateTime LastUpdateTime { get; set; }

    [Required]
    [MaxLength(128)]
    public string LastUpdateBy { get; set; } = string.Empty;

    [Required]
    [MaxLength(128)]
    public string LastUpdateApp { get; set; } = string.Empty;

    [Required]
    [MaxLength(64)]
    public string NodeLabel { get; set; } = string.Empty;

    [Required]
    public int Reliability { get; set; }

    [Required]
    [MaxLength(128)]
    public string MapCode { get; set; } = string.Empty;

    [Required]
    [MaxLength(16)]
    public string FloorNumber { get; set; } = string.Empty;

    [Required]
    public int NodeNumber { get; set; }

    [Required]
    public int ReportTimes { get; set; }

    /// <summary>
    /// X coordinate position on the map
    /// </summary>
    public double? XCoordinate { get; set; }

    /// <summary>
    /// Y coordinate position on the map
    /// </summary>
    public double? YCoordinate { get; set; }

    /// <summary>
    /// Unique identifier for the node from the map system
    /// </summary>
    [MaxLength(128)]
    public string? NodeUuid { get; set; }

    /// <summary>
    /// Node type (1=standard, etc.)
    /// </summary>
    public int? NodeType { get; set; }

    /// <summary>
    /// Allowed transit orientations (e.g., "0,180")
    /// </summary>
    [MaxLength(64)]
    public string? TransitOrientations { get; set; }

    /// <summary>
    /// Distance accuracy for navigation (meters)
    /// </summary>
    public double? DistanceAccuracy { get; set; }

    /// <summary>
    /// Goal distance accuracy (meters)
    /// </summary>
    public double? GoalDistanceAccuracy { get; set; }

    /// <summary>
    /// Angular accuracy (degrees)
    /// </summary>
    public int? AngularAccuracy { get; set; }

    /// <summary>
    /// Goal angular accuracy (degrees)
    /// </summary>
    public int? GoalAngularAccuracy { get; set; }

    /// <summary>
    /// Special configuration JSON string
    /// </summary>
    [Column(TypeName = "nvarchar(max)")]
    public string? SpecialConfig { get; set; }

    /// <summary>
    /// Function list JSON string (charging, container handling, etc.)
    /// </summary>
    [Column(TypeName = "nvarchar(max)")]
    public string? FunctionListJson { get; set; }
}
