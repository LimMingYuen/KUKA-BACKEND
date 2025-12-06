using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace QES_KUKA_AMR_API.Data.Entities;

[Table("RefreshTokens")]
public class RefreshToken
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    [Required]
    [MaxLength(256)]
    public string Token { get; set; } = string.Empty;

    [Required]
    public int UserId { get; set; }

    [ForeignKey(nameof(UserId))]
    public User? User { get; set; }

    [Required]
    public DateTime ExpiresUtc { get; set; }

    [Required]
    public DateTime CreatedUtc { get; set; }

    /// <summary>
    /// When the token was revoked (null if still valid)
    /// </summary>
    public DateTime? RevokedUtc { get; set; }

    /// <summary>
    /// The token that replaced this one (for token rotation)
    /// </summary>
    [MaxLength(256)]
    public string? ReplacedByToken { get; set; }

    /// <summary>
    /// Reason for revocation (e.g., "Replaced", "Logout", "Security")
    /// </summary>
    [MaxLength(100)]
    public string? RevokedReason { get; set; }

    /// <summary>
    /// Check if the token is expired
    /// </summary>
    [NotMapped]
    public bool IsExpired => DateTime.UtcNow >= ExpiresUtc;

    /// <summary>
    /// Check if the token has been revoked
    /// </summary>
    [NotMapped]
    public bool IsRevoked => RevokedUtc != null;

    /// <summary>
    /// Check if the token is still active (not expired and not revoked)
    /// </summary>
    [NotMapped]
    public bool IsActive => !IsExpired && !IsRevoked;
}
