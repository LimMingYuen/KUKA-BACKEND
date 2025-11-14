using Microsoft.AspNetCore.Mvc;
using QES_KUKA_AMR_API.Data.Entities;
using QES_KUKA_AMR_API.Models;
using QES_KUKA_AMR_API.Models.MissionTypes;
using QES_KUKA_AMR_API.Services.MissionTypes;

namespace QES_KUKA_AMR_API.Controllers;

[ApiController]
[Route("api/v1/mission-types")]
public class MissionTypesController : ControllerBase
{
    private readonly IMissionTypeService _missionTypeService;
    private readonly ILogger<MissionTypesController> _logger;

    public MissionTypesController(
        IMissionTypeService missionTypeService,
        ILogger<MissionTypesController> logger)
    {
        _missionTypeService = missionTypeService;
        _logger = logger;
    }

    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<List<MissionTypeDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<List<MissionTypeDto>>>> GetMissionTypesAsync(CancellationToken cancellationToken)
    {
        var missionTypes = await _missionTypeService.GetAsync(cancellationToken);
        var dtos = missionTypes.Select(MapToDto).ToList();

        return Ok(Success(dtos));
    }

    [HttpGet("{id:int}", Name = nameof(GetMissionTypeByIdAsync))]
    [ProducesResponseType(typeof(ApiResponse<MissionTypeDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<MissionTypeDto>>> GetMissionTypeByIdAsync(
        int id,
        CancellationToken cancellationToken)
    {
        var missionType = await _missionTypeService.GetByIdAsync(id, cancellationToken);
        if (missionType is null)
        {
            return NotFound(NotFoundProblem(id));
        }

        return Ok(Success(MapToDto(missionType)));
    }

    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<MissionTypeDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ApiResponse<MissionTypeDto>>> CreateMissionTypeAsync(
        [FromBody] MissionTypeCreateRequest request,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        try
        {
            var entity = await _missionTypeService.CreateAsync(new MissionType
            {
                DisplayName = request.DisplayName,
                ActualValue = request.ActualValue,
                Description = request.Description,
                IsActive = request.IsActive
            }, cancellationToken);

            var dto = MapToDto(entity);
            var response = Success(dto);
            return CreatedAtRoute(nameof(GetMissionTypeByIdAsync), new { id = dto.Id }, response);
        }
        catch (MissionTypeConflictException ex)
        {
            _logger.LogWarning(ex, "Conflict while creating mission type with actual value {ActualValue}", request.ActualValue);
            return Conflict(new ProblemDetails
            {
                Title = "Mission type already exists.",
                Detail = ex.Message,
                Status = StatusCodes.Status409Conflict,
                Type = "https://httpstatuses.com/409"
            });
        }
    }

    [HttpPut("{id:int}")]
    [ProducesResponseType(typeof(ApiResponse<MissionTypeDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ApiResponse<MissionTypeDto>>> UpdateMissionTypeAsync(
        int id,
        [FromBody] MissionTypeUpdateRequest request,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        try
        {
            var updated = await _missionTypeService.UpdateAsync(id, new MissionType
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
        catch (MissionTypeConflictException ex)
        {
            _logger.LogWarning(ex, "Conflict while updating mission type {MissionTypeId}", id);
            return Conflict(new ProblemDetails
            {
                Title = "Mission type already exists.",
                Detail = ex.Message,
                Status = StatusCodes.Status409Conflict,
                Type = "https://httpstatuses.com/409"
            });
        }
    }

    [HttpDelete("{id:int}")]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<object>>> DeleteMissionTypeAsync(
        int id,
        CancellationToken cancellationToken)
    {
        var deleted = await _missionTypeService.DeleteAsync(id, cancellationToken);
        if (!deleted)
        {
            return NotFound(NotFoundProblem(id));
        }

        return Ok(new ApiResponse<object>
        {
            Success = true,
            Msg = "Mission type deleted.",
            Data = null
        });
    }

    private static MissionTypeDto MapToDto(MissionType missionType) => new()
    {
        Id = missionType.Id,
        DisplayName = missionType.DisplayName,
        ActualValue = missionType.ActualValue,
        Description = missionType.Description,
        IsActive = missionType.IsActive,
        CreatedUtc = missionType.CreatedUtc,
        UpdatedUtc = missionType.UpdatedUtc
    };

    private static ApiResponse<T> Success<T>(T data) => new()
    {
        Success = true,
        Data = data
    };

    private static ProblemDetails NotFoundProblem(int id) => new()
    {
        Title = "Mission type not found.",
        Detail = $"Mission type with id '{id}' was not found.",
        Status = StatusCodes.Status404NotFound,
        Type = "https://httpstatuses.com/404"
    };
}
