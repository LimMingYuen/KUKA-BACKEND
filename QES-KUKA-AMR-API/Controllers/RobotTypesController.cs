using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using QES_KUKA_AMR_API.Data.Entities;
using QES_KUKA_AMR_API.Models;
using QES_KUKA_AMR_API.Models.RobotTypes;
using QES_KUKA_AMR_API.Services.RobotTypes;

namespace QES_KUKA_AMR_API.Controllers;

[ApiController]
[Authorize]
[Route("api/v1/robot-types")]
public class RobotTypesController : ControllerBase
{
    private readonly IRobotTypeService _robotTypeService;
    private readonly ILogger<RobotTypesController> _logger;

    public RobotTypesController(IRobotTypeService robotTypeService, ILogger<RobotTypesController> logger)
    {
        _robotTypeService = robotTypeService;
        _logger = logger;
    }

    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<List<RobotTypeDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<List<RobotTypeDto>>>> GetRobotTypesAsync(CancellationToken cancellationToken)
    {
        var robotTypes = await _robotTypeService.GetAsync(cancellationToken);
        var dtos = robotTypes.Select(MapToDto).ToList();
        return Ok(Success(dtos));
    }

    [HttpGet("{id:int}", Name = nameof(GetRobotTypeByIdAsync))]
    [ProducesResponseType(typeof(ApiResponse<RobotTypeDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<RobotTypeDto>>> GetRobotTypeByIdAsync(int id, CancellationToken cancellationToken)
    {
        var robotType = await _robotTypeService.GetByIdAsync(id, cancellationToken);
        if (robotType is null)
        {
            return NotFound(NotFoundProblem(id));
        }

        return Ok(Success(MapToDto(robotType)));
    }

    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<RobotTypeDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ApiResponse<RobotTypeDto>>> CreateRobotTypeAsync(
        [FromBody] RobotTypeCreateRequest request,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        try
        {
            var entity = await _robotTypeService.CreateAsync(new RobotType
            {
                DisplayName = request.DisplayName,
                ActualValue = request.ActualValue,
                Description = request.Description,
                IsActive = request.IsActive
            }, cancellationToken);

            var dto = MapToDto(entity);
            var response = Success(dto);

            return CreatedAtRoute(nameof(GetRobotTypeByIdAsync), new { id = dto.Id }, response);
        }
        catch (RobotTypeConflictException ex)
        {
            _logger.LogWarning(ex, "Conflict while creating robot type with actual value {ActualValue}", request.ActualValue);
            return Conflict(new ProblemDetails
            {
                Title = "Robot type already exists.",
                Detail = ex.Message,
                Status = StatusCodes.Status409Conflict,
                Type = "https://httpstatuses.com/409"
            });
        }
    }

    [HttpPut("{id:int}")]
    [ProducesResponseType(typeof(ApiResponse<RobotTypeDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ApiResponse<RobotTypeDto>>> UpdateRobotTypeAsync(
        int id,
        [FromBody] RobotTypeUpdateRequest request,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        try
        {
            var updated = await _robotTypeService.UpdateAsync(id, new RobotType
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
        catch (RobotTypeConflictException ex)
        {
            _logger.LogWarning(ex, "Conflict while updating robot type {RobotTypeId}", id);
            return Conflict(new ProblemDetails
            {
                Title = "Robot type already exists.",
                Detail = ex.Message,
                Status = StatusCodes.Status409Conflict,
                Type = "https://httpstatuses.com/409"
            });
        }
    }

    [HttpDelete("{id:int}")]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<object>>> DeleteRobotTypeAsync(int id, CancellationToken cancellationToken)
    {
        var deleted = await _robotTypeService.DeleteAsync(id, cancellationToken);
        if (!deleted)
        {
            return NotFound(NotFoundProblem(id));
        }

        return Ok(new ApiResponse<object>
        {
            Success = true,
            Msg = "Robot type deleted.",
            Data = null
        });
    }

    [HttpPatch("{id:int}/toggle-status")]
    [ProducesResponseType(typeof(ApiResponse<RobotTypeDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<RobotTypeDto>>> ToggleRobotTypeStatusAsync(int id, CancellationToken cancellationToken)
    {
        var toggled = await _robotTypeService.ToggleStatusAsync(id, cancellationToken);
        if (toggled is null)
        {
            return NotFound(NotFoundProblem(id));
        }

        return Ok(Success(MapToDto(toggled)));
    }

    private static RobotTypeDto MapToDto(RobotType robotType) => new()
    {
        Id = robotType.Id,
        DisplayName = robotType.DisplayName,
        ActualValue = robotType.ActualValue,
        Description = robotType.Description,
        IsActive = robotType.IsActive,
        CreatedUtc = robotType.CreatedUtc,
        UpdatedUtc = robotType.UpdatedUtc
    };

    private static ApiResponse<T> Success<T>(T data) => new()
    {
        Success = true,
        Data = data
    };

    private static ProblemDetails NotFoundProblem(int id) => new()
    {
        Title = "Robot type not found.",
        Detail = $"Robot type with id '{id}' was not found.",
        Status = StatusCodes.Status404NotFound,
        Type = "https://httpstatuses.com/404"
    };
}
