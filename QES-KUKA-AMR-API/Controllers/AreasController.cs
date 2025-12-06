using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using QES_KUKA_AMR_API.Data.Entities;
using QES_KUKA_AMR_API.Models;
using QES_KUKA_AMR_API.Models.Areas;
using QES_KUKA_AMR_API.Services.Areas;

namespace QES_KUKA_AMR_API.Controllers;

[ApiController]
[Authorize]
[Route("api/v1/areas")]
public class AreasController : ControllerBase
{
    private readonly IAreaService _areaService;
    private readonly ILogger<AreasController> _logger;

    public AreasController(IAreaService areaService, ILogger<AreasController> logger)
    {
        _areaService = areaService;
        _logger = logger;
    }

    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<List<AreaDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<List<AreaDto>>>> GetAreasAsync(CancellationToken cancellationToken)
    {
        var areas = await _areaService.GetAsync(cancellationToken);
        var dtos = areas.Select(MapToDto).ToList();
        return Ok(Success(dtos));
    }

    [HttpGet("{id:int}", Name = nameof(GetAreaByIdAsync))]
    [ProducesResponseType(typeof(ApiResponse<AreaDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<AreaDto>>> GetAreaByIdAsync(int id, CancellationToken cancellationToken)
    {
        var area = await _areaService.GetByIdAsync(id, cancellationToken);
        if (area is null)
        {
            return NotFound(NotFoundProblem(id));
        }

        return Ok(Success(MapToDto(area)));
    }

    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<AreaDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ApiResponse<AreaDto>>> CreateAreaAsync(
        [FromBody] AreaCreateRequest request,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        try
        {
            var entity = await _areaService.CreateAsync(new Area
            {
                DisplayName = request.DisplayName,
                ActualValue = request.ActualValue,
                Description = request.Description,
                IsActive = request.IsActive
            }, cancellationToken);

            var dto = MapToDto(entity);
            return CreatedAtRoute(nameof(GetAreaByIdAsync), new { id = dto.Id }, Success(dto));
        }
        catch (AreaConflictException ex)
        {
            _logger.LogWarning(ex, "Conflict while creating area with actual value {ActualValue}", request.ActualValue);
            return Conflict(new ProblemDetails
            {
                Title = "Area already exists.",
                Detail = ex.Message,
                Status = StatusCodes.Status409Conflict,
                Type = "https://httpstatuses.com/409"
            });
        }
    }

    [HttpPut("{id:int}")]
    [ProducesResponseType(typeof(ApiResponse<AreaDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ApiResponse<AreaDto>>> UpdateAreaAsync(
        int id,
        [FromBody] AreaUpdateRequest request,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        try
        {
            var updated = await _areaService.UpdateAsync(id, new Area
            {
                DisplayName = request.DisplayName,
                ActualValue = request.ActualValue,
                Description = request.Description,
                IsActive = request.IsActive
            }, cancellationToken);

            if (updated is null)
            {
                return NotFound(NotFoundProblem(id));
            }

            return Ok(Success(MapToDto(updated)));
        }
        catch (AreaConflictException ex)
        {
            _logger.LogWarning(ex, "Conflict while updating area {AreaId}", id);
            return Conflict(new ProblemDetails
            {
                Title = "Area already exists.",
                Detail = ex.Message,
                Status = StatusCodes.Status409Conflict,
                Type = "https://httpstatuses.com/409"
            });
        }
    }

    [HttpDelete("{id:int}")]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<object>>> DeleteAreaAsync(int id, CancellationToken cancellationToken)
    {
        var deleted = await _areaService.DeleteAsync(id, cancellationToken);
        if (!deleted)
        {
            return NotFound(NotFoundProblem(id));
        }

        return Ok(new ApiResponse<object>
        {
            Success = true,
            Msg = "Area deleted.",
            Data = null
        });
    }

    private static AreaDto MapToDto(Area area) => new()
    {
        Id = area.Id,
        DisplayName = area.DisplayName,
        ActualValue = area.ActualValue,
        Description = area.Description,
        IsActive = area.IsActive,
        CreatedUtc = area.CreatedUtc,
        UpdatedUtc = area.UpdatedUtc
    };

    private static ApiResponse<T> Success<T>(T data) => new()
    {
        Success = true,
        Data = data
    };

    private static ProblemDetails NotFoundProblem(int id) => new()
    {
        Title = "Area not found.",
        Detail = $"Area with id '{id}' was not found.",
        Status = StatusCodes.Status404NotFound,
        Type = "https://httpstatuses.com/404"
    };
}
