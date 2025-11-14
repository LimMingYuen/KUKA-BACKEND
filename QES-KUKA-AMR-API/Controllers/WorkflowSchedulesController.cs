using Microsoft.AspNetCore.Mvc;
using QES_KUKA_AMR_API.Data.Entities;
using QES_KUKA_AMR_API.Models;
using QES_KUKA_AMR_API.Models.Workflows;
using QES_KUKA_AMR_API.Services.Workflows;

namespace QES_KUKA_AMR_API.Controllers;

[ApiController]
[Route("api/workflows/{workflowId:int}/schedules")]
public class WorkflowSchedulesController : ControllerBase
{
    private readonly IWorkflowScheduleService _scheduleService;
    private readonly ILogger<WorkflowSchedulesController> _logger;

    public WorkflowSchedulesController(
        IWorkflowScheduleService scheduleService,
        ILogger<WorkflowSchedulesController> logger)
    {
        _scheduleService = scheduleService;
        _logger = logger;
    }

    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<List<WorkflowScheduleDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetSchedulesAsync(int workflowId, CancellationToken cancellationToken)
    {
        var schedules = await _scheduleService.GetSchedulesAsync(workflowId, cancellationToken);
        var dtos = schedules.Select(MapToDto).ToList();
        return Ok(Success(dtos));
    }

    [HttpGet("{scheduleId:int}")]
    [ProducesResponseType(typeof(ApiResponse<WorkflowScheduleDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetScheduleAsync(int workflowId, int scheduleId, CancellationToken cancellationToken)
    {
        var schedule = await _scheduleService.GetScheduleAsync(workflowId, scheduleId, cancellationToken);
        if (schedule is null)
        {
            return NotFound(NotFoundProblem(scheduleId));
        }

        return Ok(Success(MapToDto(schedule)));
    }

    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<WorkflowScheduleDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> CreateScheduleAsync(int workflowId, [FromBody] WorkflowScheduleRequest request, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        try
        {
            var schedule = await _scheduleService.CreateAsync(workflowId, request, cancellationToken);
            var dto = MapToDto(schedule);
            var location = Url.Action(nameof(GetScheduleAsync), values: new { workflowId, scheduleId = dto.Id });
            return Created(location ?? string.Empty, Success(dto));
        }
        catch (WorkflowNotFoundException ex)
        {
            _logger.LogWarning(ex, "Attempted to create schedule for missing workflow {WorkflowId}", workflowId);
            return NotFound(NotFoundWorkflowProblem(workflowId));
        }
        catch (WorkflowScheduleValidationException ex)
        {
            ModelState.AddModelError("schedule", ex.Message);
            return ValidationProblem(ModelState);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error while creating schedule for workflow {WorkflowId}", workflowId);
            return Problem(
                statusCode: StatusCodes.Status500InternalServerError,
                title: "Failed to create schedule.",
                detail: ex.Message);
        }
    }

    [HttpPut("{scheduleId:int}")]
    [ProducesResponseType(typeof(ApiResponse<WorkflowScheduleDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateScheduleAsync(int workflowId, int scheduleId, [FromBody] WorkflowScheduleRequest request, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        try
        {
            var schedule = await _scheduleService.UpdateAsync(workflowId, scheduleId, request, cancellationToken);
            if (schedule is null)
            {
                return NotFound(NotFoundProblem(scheduleId));
            }

            return Ok(Success(MapToDto(schedule)));
        }
        catch (WorkflowScheduleValidationException ex)
        {
            ModelState.AddModelError("schedule", ex.Message);
            return ValidationProblem(ModelState);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error while updating schedule {ScheduleId} for workflow {WorkflowId}", scheduleId, workflowId);
            return Problem(
                statusCode: StatusCodes.Status500InternalServerError,
                title: "Failed to update schedule.",
                detail: ex.Message);
        }
    }

    [HttpDelete("{scheduleId:int}")]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteScheduleAsync(int workflowId, int scheduleId, CancellationToken cancellationToken)
    {
        var deleted = await _scheduleService.DeleteAsync(workflowId, scheduleId, cancellationToken);
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
    [ProducesResponseType(typeof(ApiResponse<WorkflowTriggerResult>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RunNowAsync(int workflowId, int scheduleId, CancellationToken cancellationToken)
    {
        try
        {
            var triggeredBy = User.Identity?.Name
                ?? User.FindFirst("username")?.Value
                ?? User.FindFirst("sub")?.Value
                ?? "Scheduler";

            var result = await _scheduleService.RunNowAsync(workflowId, scheduleId, triggeredBy, cancellationToken);
            result.Success = true;
            result.Message = "Schedule enqueued successfully.";
            return Ok(Success(result));
        }
        catch (WorkflowNotFoundException ex)
        {
            _logger.LogWarning(ex, "Attempted to run schedule {ScheduleId} for workflow {WorkflowId} but it was not found", scheduleId, workflowId);
            return NotFound(NotFoundProblem(scheduleId));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error enqueuing run-now for schedule {ScheduleId} workflow {WorkflowId}", scheduleId, workflowId);
            return Problem(
                statusCode: StatusCodes.Status500InternalServerError,
                title: "Failed to enqueue schedule run.",
                detail: ex.Message);
        }
    }

    [HttpGet("logs")]
    [ProducesResponseType(typeof(ApiResponse<List<WorkflowScheduleLogDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetLogsAsync(
        int workflowId,
        [FromQuery] int? scheduleId,
        [FromQuery] int take = 50,
        CancellationToken cancellationToken = default)
    {
        take = Math.Clamp(take, 1, 200);
        var logs = await _scheduleService.GetLogsAsync(workflowId, scheduleId, take, cancellationToken);
        var dtos = logs.Select(MapLogToDto).ToList();
        return Ok(Success(dtos));
    }

    private static WorkflowScheduleDto MapToDto(WorkflowSchedule schedule) => new()
    {
        Id = schedule.Id,
        WorkflowId = schedule.WorkflowId,
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

    private static WorkflowScheduleLogDto MapLogToDto(WorkflowScheduleLog log) => new()
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

    private static ProblemDetails NotFoundWorkflowProblem(int workflowId) => new()
    {
        Title = "Workflow not found.",
        Detail = $"Workflow with id '{workflowId}' was not found.",
        Status = StatusCodes.Status404NotFound
    };
}
