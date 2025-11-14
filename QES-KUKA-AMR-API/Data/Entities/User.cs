using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json;

namespace QES_KUKA_AMR_API.Data.Entities;

[Table("Users")]
public class User
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.None)]
    public int Id { get; set; }

    public DateTime CreateTime { get; set; }
    [MaxLength(100)]
    public string? CreateBy { get; set; }
    [MaxLength(100)]
    public string? CreateApp { get; set; }

    public DateTime LastUpdateTime { get; set; }
    [MaxLength(100)]
    public string? LastUpdateBy { get; set; }
    [MaxLength(100)]
    public string? LastUpdateApp { get; set; }

    [MaxLength(100)]
    public string Username { get; set; } = string.Empty;
    [MaxLength(100)]
    public string Nickname { get; set; } = string.Empty;

    public int IsSuperAdmin { get; set; }

    [Column(TypeName = "nvarchar(max)")]
    public string RolesJson { get; set; } = "[]";

    [NotMapped]
    public List<Role> Roles
    {
        get
        {
            try
            {
                return string.IsNullOrWhiteSpace(RolesJson)
                    ? new List<Role>()
                    : JsonSerializer.Deserialize<List<Role>>(RolesJson) ?? new List<Role>();
            }
            catch
            {
                return new List<Role>();
            }
        }
        set => RolesJson = JsonSerializer.Serialize(value);
    }
}

[Table("Roles")]
public class Role
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.None)]
    public int Id { get; set; }

    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(100)]
    public string RoleCode { get; set; } = string.Empty;

    public bool? IsProtected { get; set; }
}

[Table("RolePermissions")]
public class RolePermission
{
    public int Id { get; set; }
    public int RoleId { get; set; }
    public int PageId { get; set; }
}