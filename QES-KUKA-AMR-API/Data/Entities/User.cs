using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json;

namespace QES_KUKA_AMR_API.Data.Entities;

[Table("Users")]
public class User
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    [Required]
    [MaxLength(100)]
    public string Username { get; set; } = string.Empty;

    [Required]
    [MaxLength(255)]
    public string PasswordHash { get; set; } = string.Empty;

    [MaxLength(100)]
    public string? Nickname { get; set; }

    [Required]
    public bool IsSuperAdmin { get; set; } = false;

    /// <summary>
    /// JSON array of role names stored in database
    /// </summary>
    public string? RolesJson { get; set; }

    /// <summary>
    /// Deserialized roles for application use (not mapped to database)
    /// </summary>
    [NotMapped]
    public List<string> Roles
    {
        get
        {
            if (string.IsNullOrWhiteSpace(RolesJson))
            {
                return new List<string>();
            }

            try
            {
                return JsonSerializer.Deserialize<List<string>>(RolesJson) ?? new List<string>();
            }
            catch
            {
                return new List<string>();
            }
        }
        set
        {
            RolesJson = value == null || value.Count == 0
                ? null
                : JsonSerializer.Serialize(value);
        }
    }

    [Required]
    public DateTime CreateTime { get; set; } = DateTime.UtcNow;

    [MaxLength(100)]
    public string? CreateBy { get; set; }

    [MaxLength(100)]
    public string? CreateApp { get; set; }

    [Required]
    public DateTime LastUpdateTime { get; set; } = DateTime.UtcNow;

    [MaxLength(100)]
    public string? LastUpdateBy { get; set; }

    [MaxLength(100)]
    public string? LastUpdateApp { get; set; }
}
