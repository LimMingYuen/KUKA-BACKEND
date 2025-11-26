using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace QES_KUKA_AMR_API.Data.Entities;

[Table("Pages")]
public class Page
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    [Required]
    [MaxLength(255)]
    public string PagePath { get; set; } = string.Empty;

    [Required]
    [MaxLength(100)]
    public string PageName { get; set; } = string.Empty;

    [MaxLength(50)]
    public string? PageIcon { get; set; }

    [Required]
    public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;
}
