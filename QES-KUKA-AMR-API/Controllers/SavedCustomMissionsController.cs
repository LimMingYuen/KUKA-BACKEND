using Microsoft.AspNetCore.Mvc;
using QES_KUKA_AMR_API.Data.Entities;
using QES_KUKA_AMR_API.Models;
using QES_KUKA_AMR_API.Models.SavedCustomMissions;
using QES_KUKA_AMR_API.Services.SavedCustomMissions;

namespace QES_KUKA_AMR_API.Controllers;

[ApiController]
[Route("api/saved-custom-missions")]
public class SavedCustomMissionsController : ControllerBase
{
    private readonly ISavedCustomMissionService _savedCustomMissionService;
    private readonly ILogger<SavedCustomMissionsController> _logger;

    public SavedCustomMissionsController(
        ISavedCustomMissionService savedCustomMissionService,
        ILogger<SavedCustomMissionsController> logger)
    {
        _savedCustomMissionService = savedCustomMissionService;
        _logger = logger;
    }

    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<List<SavedCustomMissionDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<List<SavedCustomMissionDto>>>> GetAllAsync(
        CancellationToken cancellationToken)
    {
        var missions = await _savedCustomMissionService.GetAllAsync(cancellationToken);
        var dtos = missions.Select(MapToDto).ToList();

        return Ok(Success(dtos));
    }

    [HttpGet("{id:int}", Name = nameof(GetByIdAsync))]
    [ProducesResponseType(typeof(ApiResponse<SavedCustomMissionDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<SavedCustomMissionDto>>> GetByIdAsync(
        int id,
        CancellationToken cancellationToken)
    {
        var mission = await _savedCustomMissionService.GetByIdAsync(id, cancellationToken);
        if (mission is null)
        {
            return NotFound(NotFoundProblem(id));
        }

        return Ok(Success(MapToDto(mission)));
    }

    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<SavedCustomMissionDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ApiResponse<SavedCustomMissionDto>>> CreateAsync(
        [FromBody] SavedCustomMissionCreateRequest request,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        // Get username from JWT token claims
        var createdBy = User.Identity?.Name
            ?? User.FindFirst("username")?.Value
            ?? User.FindFirst("sub")?.Value
            ?? "System";

        try
        {
            var entity = await _savedCustomMissionService.CreateAsync(new SavedCustomMission
            {
                MissionName = request.MissionName,
                Description = request.Description,
                MissionType = request.MissionType,
                RobotType = request.RobotType,
                Priority = request.Priority,
                RobotModels = request.RobotModels,
                RobotIds = request.RobotIds,
                ContainerModelCode = request.ContainerModelCode,
                ContainerCode = request.ContainerCode,
                IdleNode = request.IdleNode,
                MissionStepsJson = request.MissionStepsJson
            }, createdBy, cancellationToken);

            var dto = MapToDto(entity);
            var response = Success(dto);
            return CreatedAtRoute(nameof(GetByIdAsync), new { id = dto.Id }, response);
        }
        catch (SavedCustomMissionConflictException ex)
        {
            _logger.LogWarning(ex, "Conflict while creating saved custom mission '{MissionName}'", request.MissionName);
            return Conflict(new ProblemDetails
            {
                Title = "Saved mission already exists.",
                Detail = ex.Message,
                Status = StatusCodes.Status409Conflict,
                Type = "https://httpstatuses.com/409"
            });
        }
        catch (SavedCustomMissionValidationException ex)
        {
            _logger.LogWarning(ex, "Validation error while creating saved custom mission '{MissionName}'", request.MissionName);
            return BadRequest(new ProblemDetails
            {
                Title = "Invalid mission data.",
                Detail = ex.Message,
                Status = StatusCodes.Status400BadRequest,
                Type = "https://httpstatuses.com/400"
            });
        }
    }

    [HttpPut("{id:int}")]
    [ProducesResponseType(typeof(ApiResponse<SavedCustomMissionDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ApiResponse<SavedCustomMissionDto>>> UpdateAsync(
        int id,
        [FromBody] SavedCustomMissionUpdateRequest request,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        try
        {
            var updated = await _savedCustomMissionService.UpdateAsync(id, new SavedCustomMission
            {
                MissionName = request.MissionName,
                Description = request.Description,
                MissionType = request.MissionType,
                RobotType = request.RobotType,
                Priority = request.Priority,
                RobotModels = request.RobotModels,
                RobotIds = request.RobotIds,
                ContainerModelCode = request.ContainerModelCode,
                ContainerCode = request.ContainerCode,
                IdleNode = request.IdleNode,
                MissionStepsJson = request.MissionStepsJson
            }, cancellationToken);

            if (updated is null)
            {
                return NotFound(NotFoundProblem(id));
            }

            return Ok(Success(MapToDto(updated)));
        }
        catch (SavedCustomMissionConflictException ex)
        {
            _logger.LogWarning(ex, "Conflict while updating saved custom mission {Id}", id);
            return Conflict(new ProblemDetails
            {
                Title = "Saved mission already exists.",
                Detail = ex.Message,
                Status = StatusCodes.Status409Conflict,
                Type = "https://httpstatuses.com/409"
            });
        }
        catch (SavedCustomMissionValidationException ex)
        {
            _logger.LogWarning(ex, "Validation error while updating saved custom mission {Id}", id);
            return BadRequest(new ProblemDetails
            {
                Title = "Invalid mission data.",
                Detail = ex.Message,
                Status = StatusCodes.Status400BadRequest,
                Type = "https://httpstatuses.com/400"
            });
        }
    }

    [HttpDelete("{id:int}")]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<object>>> DeleteAsync(
        int id,
        CancellationToken cancellationToken)
    {
        var deleted = await _savedCustomMissionService.DeleteAsync(id, cancellationToken);
        if (!deleted)
        {
            return NotFound(NotFoundProblem(id));
        }

        return Ok(new ApiResponse<object>
        {
            Success = true,
            Msg = "Saved custom mission deleted.",
            Data = null
        });
    }

    [HttpPost("{id:int}/trigger")]
    [ProducesResponseType(typeof(ApiResponse<TriggerMissionResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse<TriggerMissionResponse>>> TriggerAsync(
        int id,
        CancellationToken cancellationToken)
    {
        // Get username from JWT token claims
        var triggeredBy = User.Identity?.Name
            ?? User.FindFirst("username")?.Value
            ?? User.FindFirst("sub")?.Value
            ?? "System";

        try
        {
            var result = await _savedCustomMissionService.TriggerAsync(id, triggeredBy, cancellationToken);

            var response = new TriggerMissionResponse
            {
                MissionCode = result.MissionCode,
                RequestId = result.RequestId,
                Message = $"Mission '{result.MissionCode}' has been {(result.ExecuteImmediately ? "started" : "queued")} successfully. {result.Message}"
            };

            return Ok(Success(response));
        }
        catch (SavedCustomMissionNotFoundException ex)
        {
            _logger.LogWarning(ex, "Cannot trigger saved custom mission {Id} - not found", id);
            return NotFound(new ProblemDetails
            {
                Title = "Saved mission not found.",
                Detail = ex.Message,
                Status = StatusCodes.Status404NotFound,
                Type = "https://httpstatuses.com/404"
            });
        }
        catch (SavedCustomMissionValidationException ex)
        {
            _logger.LogWarning(ex, "Validation error while triggering saved custom mission {Id}", id);
            return BadRequest(new ProblemDetails
            {
                Title = "Invalid mission data.",
                Detail = ex.Message,
                Status = StatusCodes.Status400BadRequest,
                Type = "https://httpstatuses.com/400"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error triggering saved custom mission {Id}", id);
            return StatusCode(StatusCodes.Status500InternalServerError, new ProblemDetails
            {
                Title = "Error triggering mission.",
                Detail = ex.Message,
                Status = StatusCodes.Status500InternalServerError,
                Type = "https://httpstatuses.com/500"
            });
        }
    }

    private static SavedCustomMissionDto MapToDto(SavedCustomMission mission)
    {
        var schedules = mission.Schedules ?? new List<SavedMissionSchedule>();
        var activeSchedules = schedules.Where(s => s.IsEnabled).ToList();
        var nextRunUtc = activeSchedules
            .Where(s => s.NextRunUtc.HasValue)
            .Select(s => s.NextRunUtc!.Value)
            .OrderBy(dt => dt)
            .FirstOrDefault();

        var lastRun = schedules
            .Where(s => s.LastRunUtc.HasValue)
            .OrderByDescending(s => s.LastRunUtc)
            .FirstOrDefault();

        return new SavedCustomMissionDto
        {
            Id = mission.Id,
            MissionName = mission.MissionName,
            Description = mission.Description,
            MissionType = mission.MissionType,
            RobotType = mission.RobotType,
            Priority = mission.Priority,
            RobotModels = mission.RobotModels,
            RobotIds = mission.RobotIds,
            ContainerModelCode = mission.ContainerModelCode,
            ContainerCode = mission.ContainerCode,
            IdleNode = mission.IdleNode,
            MissionStepsJson = mission.MissionStepsJson,
            CreatedBy = mission.CreatedBy,
            CreatedUtc = mission.CreatedUtc,
            UpdatedUtc = mission.UpdatedUtc,
            ScheduleSummary = new SavedMissionScheduleSummaryDto
            {
                TotalSchedules = schedules.Count,
                ActiveSchedules = activeSchedules.Count,
                NextRunUtc = nextRunUtc == default ? null : nextRunUtc,
                LastStatus = lastRun?.LastStatus,
                LastRunUtc = lastRun?.LastRunUtc
            }
        };
    }

    private static ApiResponse<T> Success<T>(T data) => new()
    {
        Success = true,
        Data = data
    };

    private static ProblemDetails NotFoundProblem(int id) => new()
    {
        Title = "Saved custom mission not found.",
        Detail = $"Saved custom mission with id '{id}' was not found.",
        Status = StatusCodes.Status404NotFound,
        Type = "https://httpstatuses.com/404"
    };
}
