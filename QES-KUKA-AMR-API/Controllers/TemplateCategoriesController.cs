using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using QES_KUKA_AMR_API.Data.Entities;
using QES_KUKA_AMR_API.Models;
using QES_KUKA_AMR_API.Models.TemplateCategories;
using QES_KUKA_AMR_API.Services.TemplateCategories;

namespace QES_KUKA_AMR_API.Controllers;

/// <summary>
/// Controller for managing template categories used to organize saved custom missions.
/// </summary>
[ApiController]
[Authorize]
[Route("api/v1/template-categories")]
public class TemplateCategoriesController : ControllerBase
{
    private readonly ITemplateCategoryService _categoryService;
    private readonly ILogger<TemplateCategoriesController> _logger;

    public TemplateCategoriesController(
        ITemplateCategoryService categoryService,
        ILogger<TemplateCategoriesController> logger)
    {
        _categoryService = categoryService;
        _logger = logger;
    }

    /// <summary>
    /// Get all template categories with template counts
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<List<TemplateCategoryDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<List<TemplateCategoryDto>>>> GetAllAsync(
        CancellationToken cancellationToken)
    {
        var categories = await _categoryService.GetAllAsync(cancellationToken);
        var templateCounts = await _categoryService.GetTemplateCountsAsync(cancellationToken);

        var dtos = categories.Select(c => MapToDto(c, templateCounts.GetValueOrDefault(c.Id, 0))).ToList();

        return Ok(new ApiResponse<List<TemplateCategoryDto>>
        {
            Success = true,
            Data = dtos
        });
    }

    /// <summary>
    /// Get a template category by ID
    /// </summary>
    [HttpGet("{id:int}", Name = nameof(GetTemplateCategoryByIdAsync))]
    [ProducesResponseType(typeof(ApiResponse<TemplateCategoryDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<TemplateCategoryDto>>> GetTemplateCategoryByIdAsync(
        int id,
        CancellationToken cancellationToken)
    {
        var category = await _categoryService.GetByIdAsync(id, cancellationToken);
        if (category is null)
        {
            return NotFound(NotFoundProblem(id));
        }

        var templateCounts = await _categoryService.GetTemplateCountsAsync(cancellationToken);

        return Ok(new ApiResponse<TemplateCategoryDto>
        {
            Success = true,
            Data = MapToDto(category, templateCounts.GetValueOrDefault(category.Id, 0))
        });
    }

    /// <summary>
    /// Create a new template category
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<TemplateCategoryDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ApiResponse<TemplateCategoryDto>>> CreateAsync(
        [FromBody] TemplateCategoryCreateRequest request,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        try
        {
            var entity = await _categoryService.CreateAsync(new TemplateCategory
            {
                Name = request.Name,
                Description = request.Description,
                DisplayOrder = request.DisplayOrder
            }, cancellationToken);

            var dto = MapToDto(entity, 0);

            return CreatedAtRoute(
                nameof(GetTemplateCategoryByIdAsync),
                new { id = dto.Id },
                new ApiResponse<TemplateCategoryDto>
                {
                    Success = true,
                    Data = dto
                });
        }
        catch (TemplateCategoryConflictException ex)
        {
            _logger.LogWarning(ex, "Conflict while creating category '{Name}'", request.Name);
            return Conflict(new ProblemDetails
            {
                Title = "Category already exists.",
                Detail = ex.Message,
                Status = StatusCodes.Status409Conflict,
                Type = "https://httpstatuses.com/409"
            });
        }
    }

    /// <summary>
    /// Update an existing template category
    /// </summary>
    [HttpPut("{id:int}")]
    [ProducesResponseType(typeof(ApiResponse<TemplateCategoryDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ApiResponse<TemplateCategoryDto>>> UpdateAsync(
        int id,
        [FromBody] TemplateCategoryUpdateRequest request,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        try
        {
            var updated = await _categoryService.UpdateAsync(id, new TemplateCategory
            {
                Name = request.Name,
                Description = request.Description,
                DisplayOrder = request.DisplayOrder
            }, cancellationToken);

            if (updated is null)
            {
                return NotFound(NotFoundProblem(id));
            }

            var templateCounts = await _categoryService.GetTemplateCountsAsync(cancellationToken);

            return Ok(new ApiResponse<TemplateCategoryDto>
            {
                Success = true,
                Data = MapToDto(updated, templateCounts.GetValueOrDefault(id, 0))
            });
        }
        catch (TemplateCategoryConflictException ex)
        {
            _logger.LogWarning(ex, "Conflict while updating category {Id}", id);
            return Conflict(new ProblemDetails
            {
                Title = "Category already exists.",
                Detail = ex.Message,
                Status = StatusCodes.Status409Conflict,
                Type = "https://httpstatuses.com/409"
            });
        }
    }

    /// <summary>
    /// Delete a template category. Templates in this category will become Uncategorized.
    /// </summary>
    [HttpDelete("{id:int}")]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<object>>> DeleteAsync(
        int id,
        CancellationToken cancellationToken)
    {
        var deleted = await _categoryService.DeleteAsync(id, cancellationToken);
        if (!deleted)
        {
            return NotFound(NotFoundProblem(id));
        }

        return Ok(new ApiResponse<object>
        {
            Success = true,
            Msg = "Category deleted. Templates have been moved to Uncategorized."
        });
    }

    private static TemplateCategoryDto MapToDto(TemplateCategory category, int templateCount) => new()
    {
        Id = category.Id,
        Name = category.Name,
        Description = category.Description,
        DisplayOrder = category.DisplayOrder,
        CreatedUtc = category.CreatedUtc,
        UpdatedUtc = category.UpdatedUtc,
        TemplateCount = templateCount
    };

    private static ProblemDetails NotFoundProblem(int id) => new()
    {
        Title = "Category not found.",
        Detail = $"Template category with id '{id}' was not found.",
        Status = StatusCodes.Status404NotFound,
        Type = "https://httpstatuses.com/404"
    };
}
