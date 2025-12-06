using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using QES_KUKA_AMR_API.Models.Schedule;
using QES_KUKA_AMR_API.Services.Schedule;

namespace QES_KUKA_AMR_API.Controllers;

/// <summary>
/// Controller for managing workflow schedules
/// </summary>
[ApiController]
[Authorize]
[Route("api/v1/workflow-schedules")]
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

    /// <summary>
    /// Get all workflow schedules
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<IEnumerable<WorkflowScheduleDto>>> GetAll(CancellationToken cancellationToken)
    {
        var schedules = await _scheduleService.GetAllAsync(cancellationToken);
        return Ok(schedules);
    }

    /// <summary>
    /// Get a workflow schedule by ID
    /// </summary>
    [HttpGet("{id}")]
    public async Task<ActionResult<WorkflowScheduleDto>> GetById(int id, CancellationToken cancellationToken)
    {
        var schedule = await _scheduleService.GetByIdAsync(id, cancellationToken);

        if (schedule == null)
        {
            return NotFound(new { Message = $"Schedule with ID {id} not found." });
        }

        return Ok(schedule);
    }

    /// <summary>
    /// Get all schedules for a specific SavedCustomMission
    /// </summary>
    [HttpGet("by-mission/{missionId}")]
    public async Task<ActionResult<IEnumerable<WorkflowScheduleDto>>> GetByMissionId(
        int missionId,
        CancellationToken cancellationToken)
    {
        var schedules = await _scheduleService.GetByMissionIdAsync(missionId, cancellationToken);
        return Ok(schedules);
    }

    /// <summary>
    /// Create a new workflow schedule
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<WorkflowScheduleDto>> Create(
        [FromBody] CreateWorkflowScheduleRequest request,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        try
        {
            // TODO: Get actual user from JWT claims
            var createdBy = "system";

            var schedule = await _scheduleService.CreateAsync(request, createdBy, cancellationToken);

            _logger.LogInformation("Created workflow schedule {ScheduleId} '{ScheduleName}'",
                schedule.Id, schedule.ScheduleName);

            return CreatedAtAction(nameof(GetById), new { id = schedule.Id }, schedule);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { Message = ex.Message });
        }
    }

    /// <summary>
    /// Update an existing workflow schedule
    /// </summary>
    [HttpPut("{id}")]
    public async Task<ActionResult<WorkflowScheduleDto>> Update(
        int id,
        [FromBody] UpdateWorkflowScheduleRequest request,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        try
        {
            var schedule = await _scheduleService.UpdateAsync(id, request, cancellationToken);

            _logger.LogInformation("Updated workflow schedule {ScheduleId} '{ScheduleName}'",
                schedule.Id, schedule.ScheduleName);

            return Ok(schedule);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { Message = ex.Message });
        }
    }

    /// <summary>
    /// Delete a workflow schedule
    /// </summary>
    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        try
        {
            await _scheduleService.DeleteAsync(id, cancellationToken);

            _logger.LogInformation("Deleted workflow schedule {ScheduleId}", id);

            return NoContent();
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(new { Message = ex.Message });
        }
    }

    /// <summary>
    /// Enable or disable a workflow schedule
    /// </summary>
    [HttpPost("{id}/toggle")]
    public async Task<ActionResult<WorkflowScheduleDto>> Toggle(
        int id,
        [FromBody] ToggleScheduleRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var schedule = await _scheduleService.ToggleEnabledAsync(id, request.IsEnabled, cancellationToken);

            _logger.LogInformation("Toggled workflow schedule {ScheduleId} to {Enabled}",
                id, request.IsEnabled ? "enabled" : "disabled");

            return Ok(schedule);
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(new { Message = ex.Message });
        }
    }

    /// <summary>
    /// Manually trigger a schedule to run now
    /// </summary>
    [HttpPost("{id}/trigger")]
    public async Task<ActionResult<ScheduleTriggerResult>> TriggerNow(int id, CancellationToken cancellationToken)
    {
        // TODO: Get actual user from JWT claims
        var triggeredBy = "manual";

        var result = await _scheduleService.TriggerNowAsync(id, triggeredBy, cancellationToken);

        if (!result.Success)
        {
            return BadRequest(result);
        }

        _logger.LogInformation("Manually triggered workflow schedule {ScheduleId} -> Mission {MissionCode}",
            id, result.MissionCode);

        return Ok(result);
    }
}
