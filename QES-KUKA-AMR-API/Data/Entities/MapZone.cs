using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace QES_KUKA_AMR_API.Data.Entities;

[Table("MapZones")]
public class MapZone
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

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
    [MaxLength(256)]
    public string ZoneName { get; set; } = string.Empty;

    [Required]
    [MaxLength(256)]
    public string ZoneCode { get; set; } = string.Empty;

    [MaxLength(1000)]
    public string ZoneDescription { get; set; } = string.Empty;

    [MaxLength(64)]
    public string ZoneColor { get; set; } = string.Empty;

    [Required]
    [MaxLength(128)]
    public string MapCode { get; set; } = string.Empty;

    [Required]
    [MaxLength(16)]
    public string FloorNumber { get; set; } = string.Empty;

    public string Points { get; set; } = string.Empty;

    public string Nodes { get; set; } = string.Empty;

    public string Edges { get; set; } = string.Empty;

    public string CustomerUi { get; set; } = string.Empty;

    [Required]
    [MaxLength(16)]
    public string ZoneType { get; set; } = string.Empty;

    [Required]
    public int Status { get; set; }

    public DateTime? BeginTime { get; set; }

    public DateTime? EndTime { get; set; }

    // Store configs as JSON string
    public string? Configs { get; set; }
}
