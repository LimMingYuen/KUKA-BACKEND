using Microsoft.AspNetCore.Mvc;
using QES_KUKA_AMR_API.Data.Entities;
using QES_KUKA_AMR_API.Models;
using QES_KUKA_AMR_API.Models.Page;
using QES_KUKA_AMR_API.Services.Pages;

namespace QES_KUKA_AMR_API.Controllers;

[ApiController]
[Route("api/v1/pages")]
public class PagesController : ControllerBase
{
    private readonly IPageService _pageService;
    private readonly ILogger<PagesController> _logger;

    public PagesController(IPageService pageService, ILogger<PagesController> logger)
    {
        _pageService = pageService;
        _logger = logger;
    }

    /// <summary>
    /// Get all pages
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<List<PageDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<List<PageDto>>>> GetPagesAsync(CancellationToken cancellationToken)
    {
        var pages = await _pageService.GetAsync(cancellationToken);
        var dtos = pages.Select(MapToDto).ToList();
        return Ok(Success(dtos));
    }

    /// <summary>
    /// Get page by ID
    /// </summary>
    [HttpGet("{id:int}", Name = nameof(GetPageByIdAsync))]
    [ProducesResponseType(typeof(ApiResponse<PageDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<PageDto>>> GetPageByIdAsync(int id, CancellationToken cancellationToken)
    {
        var page = await _pageService.GetByIdAsync(id, cancellationToken);
        if (page is null)
        {
            return NotFound(NotFoundProblem(id));
        }

        return Ok(Success(MapToDto(page)));
    }

    /// <summary>
    /// Get page by path
    /// </summary>
    [HttpGet("path/{*pagePath}")]
    [ProducesResponseType(typeof(ApiResponse<PageDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<PageDto>>> GetPageByPathAsync(string pagePath, CancellationToken cancellationToken)
    {
        var page = await _pageService.GetByPathAsync(pagePath, cancellationToken);
        if (page is null)
        {
            return NotFound(new ProblemDetails
            {
                Title = "Page not found.",
                Detail = $"Page with path '{pagePath}' was not found.",
                Status = StatusCodes.Status404NotFound,
                Type = "https://httpstatuses.com/404"
            });
        }

        return Ok(Success(MapToDto(page)));
    }

    /// <summary>
    /// Create a new page
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<PageDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ApiResponse<PageDto>>> CreatePageAsync(
        [FromBody] PageCreateRequest request,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        try
        {
            var entity = await _pageService.CreateAsync(new Page
            {
                PagePath = request.PagePath,
                PageName = request.PageName,
                PageIcon = request.PageIcon
            }, cancellationToken);

            var dto = MapToDto(entity);
            return CreatedAtRoute(nameof(GetPageByIdAsync), new { id = dto.Id }, Success(dto));
        }
        catch (PageConflictException ex)
        {
            _logger.LogWarning(ex, "Conflict while creating page with path {PagePath}", request.PagePath);
            return Conflict(new ProblemDetails
            {
                Title = "Page already exists.",
                Detail = ex.Message,
                Status = StatusCodes.Status409Conflict,
                Type = "https://httpstatuses.com/409"
            });
        }
    }

    /// <summary>
    /// Update an existing page
    /// </summary>
    [HttpPut("{id:int}")]
    [ProducesResponseType(typeof(ApiResponse<PageDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ApiResponse<PageDto>>> UpdatePageAsync(
        int id,
        [FromBody] PageUpdateRequest request,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        try
        {
            var updated = await _pageService.UpdateAsync(id, new Page
            {
                PagePath = request.PagePath,
                PageName = request.PageName,
                PageIcon = request.PageIcon
            }, cancellationToken);

            if (updated is null)
            {
                return NotFound(NotFoundProblem(id));
            }

            return Ok(Success(MapToDto(updated)));
        }
        catch (PageConflictException ex)
        {
            _logger.LogWarning(ex, "Conflict while updating page {PageId}", id);
            return Conflict(new ProblemDetails
            {
                Title = "Page already exists.",
                Detail = ex.Message,
                Status = StatusCodes.Status409Conflict,
                Type = "https://httpstatuses.com/409"
            });
        }
    }

    /// <summary>
    /// Delete a page
    /// </summary>
    [HttpDelete("{id:int}")]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<object>>> DeletePageAsync(int id, CancellationToken cancellationToken)
    {
        var deleted = await _pageService.DeleteAsync(id, cancellationToken);
        if (!deleted)
        {
            return NotFound(NotFoundProblem(id));
        }

        return Ok(new ApiResponse<object>
        {
            Success = true,
            Msg = "Page deleted.",
            Data = null
        });
    }

    /// <summary>
    /// Sync pages from frontend application (auto-registration)
    /// </summary>
    [HttpPost("sync")]
    [ProducesResponseType(typeof(ApiResponse<PageSyncResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ApiResponse<PageSyncResponse>>> SyncPagesAsync(
        [FromBody] PageSyncRequest request,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        var pages = request.Pages.Select(p => new Page
        {
            PagePath = p.PagePath,
            PageName = p.PageName,
            PageIcon = p.PageIcon
        }).ToList();

        var (total, newCount, updatedCount, unchangedCount) = await _pageService.SyncPagesAsync(pages, cancellationToken);

        var response = new PageSyncResponse
        {
            TotalPages = total,
            NewPages = newCount,
            UpdatedPages = updatedCount,
            UnchangedPages = unchangedCount
        };

        return Ok(Success(response));
    }

    private static PageDto MapToDto(Page page) => new()
    {
        Id = page.Id,
        PagePath = page.PagePath,
        PageName = page.PageName,
        PageIcon = page.PageIcon,
        CreatedUtc = page.CreatedUtc
    };

    private static ApiResponse<T> Success<T>(T data) => new()
    {
        Success = true,
        Data = data
    };

    private static ProblemDetails NotFoundProblem(int id) => new()
    {
        Title = "Page not found.",
        Detail = $"Page with id '{id}' was not found.",
        Status = StatusCodes.Status404NotFound,
        Type = "https://httpstatuses.com/404"
    };
}
