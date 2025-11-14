using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace QES_KUKA_AMR_API.Data.Entities;

[Table("Areas")]
public class Area
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    [Required]
    [MaxLength(128)]
    public string DisplayName { get; set; } = string.Empty;

    [Required]
    [MaxLength(128)]
    public string ActualValue { get; set; } = string.Empty;

    [MaxLength(512)]
    public string? Description { get; set; }

    [Required]
    public bool IsActive { get; set; } = true;

    [Required]
    public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;

    [Required]
    public DateTime UpdatedUtc { get; set; } = DateTime.UtcNow;
}
