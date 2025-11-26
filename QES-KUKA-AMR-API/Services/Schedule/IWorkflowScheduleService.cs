using QES_KUKA_AMR_API.Data.Entities;
using QES_KUKA_AMR_API.Models.Schedule;

namespace QES_KUKA_AMR_API.Services.Schedule;

/// <summary>
/// Service for managing workflow schedules
/// </summary>
public interface IWorkflowScheduleService
{
    /// <summary>
    /// Get all workflow schedules
    /// </summary>
    Task<IEnumerable<WorkflowScheduleDto>> GetAllAsync(CancellationToken ct = default);

    /// <summary>
    /// Get a workflow schedule by ID
    /// </summary>
    Task<WorkflowScheduleDto?> GetByIdAsync(int id, CancellationToken ct = default);

    /// <summary>
    /// Get all schedules for a specific SavedCustomMission
    /// </summary>
    Task<IEnumerable<WorkflowScheduleDto>> GetByMissionIdAsync(int missionId, CancellationToken ct = default);

    /// <summary>
    /// Create a new workflow schedule
    /// </summary>
    Task<WorkflowScheduleDto> CreateAsync(CreateWorkflowScheduleRequest request, string createdBy, CancellationToken ct = default);

    /// <summary>
    /// Update an existing workflow schedule
    /// </summary>
    Task<WorkflowScheduleDto> UpdateAsync(int id, UpdateWorkflowScheduleRequest request, CancellationToken ct = default);

    /// <summary>
    /// Delete a workflow schedule
    /// </summary>
    Task DeleteAsync(int id, CancellationToken ct = default);

    /// <summary>
    /// Toggle the enabled state of a schedule
    /// </summary>
    Task<WorkflowScheduleDto> ToggleEnabledAsync(int id, bool enabled, CancellationToken ct = default);

    /// <summary>
    /// Manually trigger a schedule to run now
    /// </summary>
    Task<ScheduleTriggerResult> TriggerNowAsync(int id, string triggeredBy, CancellationToken ct = default);

    /// <summary>
    /// Get all schedules that are due to run (used by scheduler hosted service)
    /// </summary>
    Task<IEnumerable<WorkflowSchedule>> GetDueSchedulesAsync(DateTime now, CancellationToken ct = default);

    /// <summary>
    /// Update a schedule after execution (used by scheduler hosted service)
    /// </summary>
    Task UpdateAfterExecutionAsync(int id, bool success, string? errorMessage, CancellationToken ct = default);
}
