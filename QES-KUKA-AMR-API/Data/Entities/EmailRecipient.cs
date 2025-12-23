using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace QES_KUKA_AMR_API.Data.Entities;

/// <summary>
/// Represents an email recipient for system notifications.
/// </summary>
[Table("EmailRecipients")]
public class EmailRecipient
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    /// <summary>
    /// Email address of the recipient (must be unique)
    /// </summary>
    [Required]
    [MaxLength(255)]
    [EmailAddress]
    public string EmailAddress { get; set; } = string.Empty;

    /// <summary>
    /// Display name of the recipient
    /// </summary>
    [Required]
    [MaxLength(128)]
    public string DisplayName { get; set; } = string.Empty;

    /// <summary>
    /// Optional description or notes about this recipient
    /// </summary>
    [MaxLength(512)]
    public string? Description { get; set; }

    /// <summary>
    /// Comma-separated list of notification types this recipient should receive.
    /// Valid values: MissionError, JobQueryError, SystemAlert
    /// Example: "MissionError,JobQueryError"
    /// </summary>
    [Required]
    [MaxLength(512)]
    public string NotificationTypes { get; set; } = "MissionError,JobQueryError";

    /// <summary>
    /// Whether this recipient is active and should receive notifications
    /// </summary>
    [Required]
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// UTC timestamp when this recipient was created
    /// </summary>
    [Required]
    public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// UTC timestamp when this recipient was last updated
    /// </summary>
    [Required]
    public DateTime UpdatedUtc { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Username of the user who created this recipient
    /// </summary>
    [MaxLength(128)]
    public string? CreatedBy { get; set; }

    /// <summary>
    /// Username of the user who last updated this recipient
    /// </summary>
    [MaxLength(128)]
    public string? UpdatedBy { get; set; }
}
