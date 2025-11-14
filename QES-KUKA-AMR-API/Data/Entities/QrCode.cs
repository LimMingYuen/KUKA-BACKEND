using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace QES_KUKA_AMR_API.Data.Entities;

[Table("QrCodes")]
public class QrCode
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
}
