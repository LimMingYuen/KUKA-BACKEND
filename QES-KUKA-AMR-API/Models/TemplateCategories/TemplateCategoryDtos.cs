using System.ComponentModel.DataAnnotations;

namespace QES_KUKA_AMR_API.Models.TemplateCategories;

/// <summary>
/// DTO for returning template category data
/// </summary>
public class TemplateCategoryDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int DisplayOrder { get; set; }
    public DateTime CreatedUtc { get; set; }
    public DateTime UpdatedUtc { get; set; }

    /// <summary>
    /// Number of templates in this category
    /// </summary>
    public int TemplateCount { get; set; }
}

/// <summary>
/// Request to create a new template category
/// </summary>
public class TemplateCategoryCreateRequest
{
    [Required]
    [MaxLength(128)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(512)]
    public string? Description { get; set; }

    public int DisplayOrder { get; set; } = 0;
}

/// <summary>
/// Request to update an existing template category
/// </summary>
public class TemplateCategoryUpdateRequest
{
    [Required]
    [MaxLength(128)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(512)]
    public string? Description { get; set; }

    public int DisplayOrder { get; set; }
}

/// <summary>
/// Request to assign a template to a category
/// </summary>
public class AssignTemplateToCategoryRequest
{
    /// <summary>
    /// Category ID to assign. Null means "Uncategorized".
    /// </summary>
    public int? CategoryId { get; set; }
}
