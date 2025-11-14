using Microsoft.AspNetCore.Mvc;
using QES_KUKA_AMR_API.Data.Entities;
using QES_KUKA_AMR_API.Models;
using QES_KUKA_AMR_API.Models.SavedCustomMissions;
using QES_KUKA_AMR_API.Services.SavedCustomMissions;

namespace QES_KUKA_AMR_API.Controllers;

[ApiController]
[Route("api/saved-custom-missions/{missionId:int}/schedules")]
public class SavedMissionSchedulesController : ControllerBase
{
    private readonly ISavedMissionScheduleService _scheduleService;
    private readonly ILogger<SavedMissionSchedulesController> _logger;

    public SavedMissionSchedulesController(
        ISavedMissionScheduleService scheduleService,
        ILogger<SavedMissionSchedulesController> logger)
    {
        _scheduleService = scheduleService;
        _logger = logger;
    }

    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<List<SavedMissionScheduleDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetSchedulesAsync(int missionId, CancellationToken cancellationToken)
    {
        var schedules = await _scheduleService.GetSchedulesAsync(missionId, cancellationToken);
        var dtos = schedules.Select(MapToDto).ToList();
        return Ok(Success(dtos));
    }

    [HttpGet("{scheduleId:int}")]
    [ProducesResponseType(typeof(ApiResponse<SavedMissionScheduleDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetScheduleAsync(int missionId, int scheduleId, CancellationToken cancellationToken)
    {
        var schedule = await _scheduleService.GetScheduleAsync(missionId, scheduleId, cancellationToken);
        if (schedule is null)
        {
            return NotFound(NotFoundProblem(scheduleId));
        }

        return Ok(Success(MapToDto(schedule)));
    }

    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<SavedMissionScheduleDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> CreateScheduleAsync(int missionId, [FromBody] SavedMissionScheduleRequest request, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        try
        {
            var schedule = await _scheduleService.CreateAsync(missionId, request, cancellationToken);
            var dto = MapToDto(schedule);
            var location = Url.Action(nameof(GetScheduleAsync), values: new { missionId, scheduleId = dto.Id });
            return Created(location ?? string.Empty, Success(dto));
        }
        catch (SavedCustomMissionNotFoundException ex)
        {
            _logger.LogWarning(ex, "Attempted to create schedule for missing mission {MissionId}", missionId);
            return NotFound(NotFoundMissionProblem(missionId));
        }
        catch (SavedMissionScheduleValidationException ex)
        {
            ModelState.AddModelError("schedule", ex.Message);
            return ValidationProblem(ModelState);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error while creating schedule for mission {MissionId}", missionId);
            return Problem(
                statusCode: StatusCodes.Status500InternalServerError,
                title: "Failed to create schedule.",
                detail: ex.Message);
        }
    }

    [HttpPut("{scheduleId:int}")]
    [ProducesResponseType(typeof(ApiResponse<SavedMissionScheduleDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateScheduleAsync(int missionId, int scheduleId, [FromBody] SavedMissionScheduleRequest request, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        try
        {
            var schedule = await _scheduleService.UpdateAsync(missionId, scheduleId, request, cancellationToken);
            if (schedule is null)
            {
                return NotFound(NotFoundProblem(scheduleId));
            }

            return Ok(Success(MapToDto(schedule)));
        }
        catch (SavedMissionScheduleValidationException ex)
        {
            ModelState.AddModelError("schedule", ex.Message);
            return ValidationProblem(ModelState);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error while updating schedule {ScheduleId} for mission {MissionId}", scheduleId, missionId);
            return Problem(
                statusCode: StatusCodes.Status500InternalServerError,
                title: "Failed to update schedule.",
                detail: ex.Message);
        }
    }

    [HttpDelete("{scheduleId:int}")]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteScheduleAsync(int missionId, int scheduleId, CancellationToken cancellationToken)
    {
        var deleted = await _scheduleService.DeleteAsync(missionId, scheduleId, cancellationToken);
        if (!deleted)
        {
            return NotFound(NotFoundProblem(scheduleId));
        }

        return Ok(new ApiResponse<object>
        {
            Success = true,
            Msg = "Schedule deleted."
        });
    }

    [HttpPost("{scheduleId:int}/run-now")]
    [ProducesResponseType(typeof(ApiResponse<TriggerResult>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RunNowAsync(int missionId, int scheduleId, CancellationToken cancellationToken)
    {
        try
        {
            var triggeredBy = User.Identity?.Name
                ?? User.FindFirst("username")?.Value
                ?? User.FindFirst("sub")?.Value
                ?? "Scheduler";

            var result = await _scheduleService.RunNowAsync(missionId, scheduleId, triggeredBy, cancellationToken);
            result.Success = true;
            result.Message = "Schedule enqueued successfully.";
            return Ok(Success(result));
        }
        catch (SavedCustomMissionNotFoundException ex)
        {
            _logger.LogWarning(ex, "Attempted to run schedule {ScheduleId} for mission {MissionId} but it was not found", scheduleId, missionId);
            return NotFound(NotFoundProblem(scheduleId));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error enqueuing run-now for schedule {ScheduleId} mission {MissionId}", scheduleId, missionId);
            return Problem(
                statusCode: StatusCodes.Status500InternalServerError,
                title: "Failed to enqueue schedule run.",
                detail: ex.Message);
        }
    }

    [HttpGet("logs")]
    [ProducesResponseType(typeof(ApiResponse<List<SavedMissionScheduleLogDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetLogsAsync(
        int missionId,
        [FromQuery] int? scheduleId,
        [FromQuery] int take = 50,
        CancellationToken cancellationToken = default)
    {
        take = Math.Clamp(take, 1, 200);
        var logs = await _scheduleService.GetLogsAsync(missionId, scheduleId, take, cancellationToken);
        var dtos = logs.Select(MapLogToDto).ToList();
        return Ok(Success(dtos));
    }

    private static SavedMissionScheduleDto MapToDto(SavedMissionSchedule schedule) => new()
    {
        Id = schedule.Id,
        SavedMissionId = schedule.SavedMissionId,
        TriggerType = schedule.TriggerType,
        CronExpression = schedule.CronExpression,
        OneTimeRunUtc = schedule.OneTimeRunUtc,
        TimezoneId = schedule.TimezoneId,
        IsEnabled = schedule.IsEnabled,
        CreatedUtc = schedule.CreatedUtc,
        UpdatedUtc = schedule.UpdatedUtc,
        LastRunUtc = schedule.LastRunUtc,
        LastStatus = schedule.LastStatus,
        LastError = schedule.LastError,
        NextRunUtc = schedule.NextRunUtc
    };

    private static SavedMissionScheduleLogDto MapLogToDto(SavedMissionScheduleLog log) => new()
    {
        Id = log.Id,
        ScheduleId = log.ScheduleId,
        ScheduledForUtc = log.ScheduledForUtc,
        EnqueuedUtc = log.EnqueuedUtc,
        QueueId = log.QueueId,
        ResultStatus = log.ResultStatus,
        Error = log.Error,
        CreatedUtc = log.CreatedUtc
    };

    private static ApiResponse<T> Success<T>(T data) => new()
    {
        Success = true,
        Data = data
    };

    private static ProblemDetails NotFoundProblem(int scheduleId) => new()
    {
        Title = "Schedule not found.",
        Detail = $"Schedule with id '{scheduleId}' was not found.",
        Status = StatusCodes.Status404NotFound
    };

    private static ProblemDetails NotFoundMissionProblem(int missionId) => new()
    {
        Title = "Saved mission not found.",
        Detail = $"Saved mission with id '{missionId}' was not found.",
        Status = StatusCodes.Status404NotFound
    };
}
