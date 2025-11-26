using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace QES_KUKA_AMR_API.Data.Entities;

[Table("RolePermissions")]
public class RolePermission
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    [Required]
    public int RoleId { get; set; }

    [Required]
    public int PageId { get; set; }

    [Required]
    public bool CanAccess { get; set; } = true;

    [Required]
    public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;

    // Navigation properties
    [ForeignKey(nameof(RoleId))]
    public Role? Role { get; set; }

    [ForeignKey(nameof(PageId))]
    public Page? Page { get; set; }
}
