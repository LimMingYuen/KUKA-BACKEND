using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace QES_KUKA_AMR_API.Data.Entities;

[Table("UserTemplatePermissions")]
public class UserTemplatePermission
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    [Required]
    public int UserId { get; set; }

    [Required]
    public int SavedCustomMissionId { get; set; }

    [Required]
    public bool CanAccess { get; set; } = true;

    [Required]
    public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;

    // Navigation properties
    [ForeignKey(nameof(UserId))]
    public User? User { get; set; }

    [ForeignKey(nameof(SavedCustomMissionId))]
    public SavedCustomMission? SavedCustomMission { get; set; }
}
