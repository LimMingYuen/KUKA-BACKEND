using System.ComponentModel.DataAnnotations;

namespace QES_KUKA_AMR_API.Models.RobotTypes;

public class RobotTypeDto
{
    public int Id { get; set; }
    public string DisplayName { get; set; } = string.Empty;
    public string ActualValue { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedUtc { get; set; }
    public DateTime UpdatedUtc { get; set; }
}

public class RobotTypeCreateRequest
{
    [Required]
    [MaxLength(128)]
    public string DisplayName { get; set; } = string.Empty;

    [Required]
    [MaxLength(128)]
    public string ActualValue { get; set; } = string.Empty;

    [MaxLength(512)]
    public string? Description { get; set; }

    public bool IsActive { get; set; } = true;
}

public class RobotTypeUpdateRequest
{
    [Required]
    [MaxLength(128)]
    public string DisplayName { get; set; } = string.Empty;

    [Required]
    [MaxLength(128)]
    public string ActualValue { get; set; } = string.Empty;

    [MaxLength(512)]
    public string? Description { get; set; }

    public bool IsActive { get; set; }
}
