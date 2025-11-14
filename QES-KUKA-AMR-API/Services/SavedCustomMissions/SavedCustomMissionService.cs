using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using QES_KUKA_AMR_API.Data;
using QES_KUKA_AMR_API.Data.Entities;
using QES_KUKA_AMR_API.Models.Missions;

namespace QES_KUKA_AMR_API.Services.SavedCustomMissions;

public interface ISavedCustomMissionService
{
    Task<List<SavedCustomMission>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<SavedCustomMission?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<SavedCustomMission> CreateAsync(SavedCustomMission mission, string createdBy, CancellationToken cancellationToken = default);
    Task<SavedCustomMission?> UpdateAsync(int id, SavedCustomMission mission, CancellationToken cancellationToken = default);
    Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default);
    Task<TriggerResult> TriggerAsync(int id, string triggeredBy, MissionTriggerSource triggerSource = MissionTriggerSource.Manual, CancellationToken cancellationToken = default);
}

public class SavedCustomMissionService : ISavedCustomMissionService
{
    private readonly ApplicationDbContext _dbContext;
    private readonly IQueueService _queueService;
    private readonly ILogger<SavedCustomMissionService> _logger;

    public SavedCustomMissionService(
        ApplicationDbContext dbContext,
        IQueueService queueService,
        ILogger<SavedCustomMissionService> logger)
    {
        _dbContext = dbContext;
        _queueService = queueService;
        _logger = logger;
    }

    public async Task<List<SavedCustomMission>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _dbContext.SavedCustomMissions
            .Include(m => m.Schedules)
            .AsNoTracking()
            .OrderByDescending(m => m.CreatedUtc)
            .ToListAsync(cancellationToken);
    }

    public async Task<SavedCustomMission?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        return await _dbContext.SavedCustomMissions
            .Include(m => m.Schedules)
            .AsNoTracking()
            .FirstOrDefaultAsync(m => m.Id == id, cancellationToken);
    }

    private static readonly JsonSerializerOptions MissionStepSerializerOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public async Task<SavedCustomMission> CreateAsync(
        SavedCustomMission mission,
        string createdBy,
        CancellationToken cancellationToken = default)
    {
        // Check for duplicate mission name by same user
        var exists = await _dbContext.SavedCustomMissions
            .AnyAsync(m => m.MissionName == mission.MissionName && m.CreatedBy == createdBy, cancellationToken);

        if (exists)
        {
            _logger.LogWarning("Cannot create saved mission. Mission name '{MissionName}' already exists for user {CreatedBy}",
                mission.MissionName, createdBy);
            throw new SavedCustomMissionConflictException(
                $"A saved mission with name '{mission.MissionName}' already exists.");
        }

        await ValidateAreaStepsAsync(mission.MissionStepsJson, cancellationToken);

        mission.CreatedBy = createdBy;
        mission.CreatedUtc = DateTime.UtcNow;
        mission.UpdatedUtc = mission.CreatedUtc;
        mission.IsDeleted = false;

        _dbContext.SavedCustomMissions.Add(mission);
        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Saved custom mission '{MissionName}' created by {CreatedBy} with ID {Id}",
            mission.MissionName, createdBy, mission.Id);

        return mission;
    }

    public async Task<SavedCustomMission?> UpdateAsync(
        int id,
        SavedCustomMission mission,
        CancellationToken cancellationToken = default)
    {
        var existing = await _dbContext.SavedCustomMissions
            .FirstOrDefaultAsync(m => m.Id == id, cancellationToken);

        if (existing is null)
        {
            return null;
        }

        // Check for duplicate name (excluding current record)
        var duplicateExists = await _dbContext.SavedCustomMissions
            .AnyAsync(m => m.Id != id
                        && m.MissionName == mission.MissionName
                        && m.CreatedBy == existing.CreatedBy, cancellationToken);

        if (duplicateExists)
        {
            _logger.LogWarning("Cannot update saved mission {Id}. Mission name '{MissionName}' already exists",
                id, mission.MissionName);
            throw new SavedCustomMissionConflictException(
                $"A saved mission with name '{mission.MissionName}' already exists.");
        }

        await ValidateAreaStepsAsync(mission.MissionStepsJson, cancellationToken);

        existing.MissionName = mission.MissionName;
        existing.Description = mission.Description;
        existing.MissionType = mission.MissionType;
        existing.RobotType = mission.RobotType;
        existing.Priority = mission.Priority;
        existing.RobotModels = mission.RobotModels;
        existing.RobotIds = mission.RobotIds;
        existing.ContainerModelCode = mission.ContainerModelCode;
        existing.ContainerCode = mission.ContainerCode;
        existing.IdleNode = mission.IdleNode;
        existing.MissionStepsJson = mission.MissionStepsJson;
        existing.UpdatedUtc = DateTime.UtcNow;

        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Saved custom mission {Id} ('{MissionName}') updated", id, existing.MissionName);

        return existing;
    }

    public async Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var entity = await _dbContext.SavedCustomMissions
            .FirstOrDefaultAsync(m => m.Id == id, cancellationToken);

        if (entity is null)
        {
            return false;
        }

        // Soft delete
        entity.IsDeleted = true;
        entity.UpdatedUtc = DateTime.UtcNow;
        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Saved custom mission {Id} ('{MissionName}') soft deleted", id, entity.MissionName);

        return true;
    }

    /// <summary>
    /// Triggers a saved custom mission by creating a new MissionQueue entry with fresh IDs
    /// </summary>
    public async Task<TriggerResult> TriggerAsync(
        int id,
        string triggeredBy,
        MissionTriggerSource triggerSource = MissionTriggerSource.Manual,
        CancellationToken cancellationToken = default)
    {
        var savedMission = await GetByIdAsync(id, cancellationToken);
        if (savedMission is null)
        {
            _logger.LogWarning("Cannot trigger saved mission {Id} - not found", id);
            throw new SavedCustomMissionNotFoundException($"Saved mission with ID {id} not found.");
        }

        await ValidateAreaStepsAsync(savedMission.MissionStepsJson, cancellationToken);

        // Generate fresh IDs using WorkflowTrigger pattern
        var timestamp = DateTime.UtcNow.ToString("yyyyMMddHHmmss");
        var requestId = $"request{timestamp}";
        var missionCode = $"mission{timestamp}";

        // Parse mission steps from JSON
        List<MissionDataItem>? missionData = null;
        if (!string.IsNullOrWhiteSpace(savedMission.MissionStepsJson))
        {
            try
            {
                missionData = JsonSerializer.Deserialize<List<MissionDataItem>>(
                    savedMission.MissionStepsJson,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            }
            catch (JsonException ex)
            {
                _logger.LogError(ex, "Failed to deserialize mission steps JSON for saved mission {Id}", id);
                throw new InvalidOperationException("Invalid mission steps data", ex);
            }
        }

        // Parse comma-separated robot models and IDs
        List<string>? robotModels = null;
        List<string>? robotIds = null;

        if (!string.IsNullOrWhiteSpace(savedMission.RobotModels))
        {
            robotModels = savedMission.RobotModels.Split(',')
                .Select(s => s.Trim())
                .Where(s => !string.IsNullOrEmpty(s))
                .ToList();
        }

        if (!string.IsNullOrWhiteSpace(savedMission.RobotIds))
        {
            robotIds = savedMission.RobotIds.Split(',')
                .Select(s => s.Trim())
                .Where(s => !string.IsNullOrEmpty(s))
                .ToList();
        }

        // Build enqueue request
        var enqueueRequest = new EnqueueRequest
        {
            // Use SavedMission.Id as WorkflowId to prevent concurrent triggers of same saved mission
            WorkflowId = id,
            WorkflowCode = null,
            WorkflowName = savedMission.MissionName,
            TemplateCode = null,

            // Mission source tracking
            SavedMissionId = id,
            TriggerSource = triggerSource,

            // Common fields with fresh IDs
            MissionCode = missionCode,
            RequestId = requestId,
            Priority = savedMission.Priority,
            CreatedBy = triggeredBy,

            // Custom mission fields from saved mission
            MissionType = savedMission.MissionType,
            RobotType = savedMission.RobotType,
            RobotModels = robotModels,
            RobotIds = robotIds,
            ContainerModelCode = savedMission.ContainerModelCode,
            ContainerCode = savedMission.ContainerCode,
            ViewBoardType = null, // Always null
            IdleNode = savedMission.IdleNode,
            LockRobotAfterFinish = false, // Always false
            UnlockRobotId = null,
            UnlockMissionCode = null,
            MissionData = missionData
        };

        // Enqueue the mission
        var queueResult = await _queueService.EnqueueMissionAsync(enqueueRequest, cancellationToken);

        if (!queueResult.Success)
        {
            _logger.LogError("Failed to enqueue mission from saved custom mission {Id}", id);
            throw new InvalidOperationException("Failed to enqueue mission");
        }

        _logger.LogInformation("Triggered saved custom mission {Id} ('{MissionName}'). " +
            "Generated MissionCode: {MissionCode}, RequestId: {RequestId}, QueueId: {QueueId}",
            id, savedMission.MissionName, missionCode, requestId, queueResult.QueueId);

        return new TriggerResult
        {
            Success = true,
            MissionCode = missionCode,
            RequestId = requestId,
            QueueId = queueResult.QueueId,
            ExecuteImmediately = queueResult.ExecuteImmediately,
            Message = queueResult.Message
        };
    }

    private async Task ValidateAreaStepsAsync(string? missionStepsJson, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(missionStepsJson))
        {
            return;
        }

        List<MissionDataItem>? missionSteps;
        try
        {
            missionSteps = JsonSerializer.Deserialize<List<MissionDataItem>>(missionStepsJson, MissionStepSerializerOptions);
        }
        catch (JsonException ex)
        {
            _logger.LogWarning(ex, "Invalid mission steps JSON payload.");
            throw new SavedCustomMissionValidationException("Mission steps payload is invalid JSON.");
        }

        if (missionSteps is null || missionSteps.Count == 0)
        {
            return;
        }

        var activeAreas = await _dbContext.Areas
            .Where(area => area.IsActive)
            .Select(area => area.ActualValue)
            .ToListAsync(cancellationToken);

        if (activeAreas.Count == 0)
        {
            return;
        }

        var areaReferences = missionSteps
            .Select(step => GetAreaActualValue(step, activeAreas))
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (areaReferences.Count == 0)
        {
            return;
        }

        var invalidValues = areaReferences
            .Where(value => !activeAreas.Any(active => string.Equals(active, value, StringComparison.OrdinalIgnoreCase)))
            .ToList();

        if (invalidValues.Count > 0)
        {
            var joined = string.Join(", ", invalidValues);
            throw new SavedCustomMissionValidationException($"The mission references unknown or inactive areas: {joined}. Please update the mission steps.");
        }
    }

    private static string? GetAreaActualValue(MissionDataItem step, IReadOnlyCollection<string> activeAreas)
    {
        if (string.Equals(step.Type, "AREA", StringComparison.OrdinalIgnoreCase))
        {
            return step.Position?.Trim();
        }

        if (string.IsNullOrWhiteSpace(step.Position)
            && !string.IsNullOrWhiteSpace(step.Type)
            && activeAreas.Any(active => string.Equals(active, step.Type.Trim(), StringComparison.OrdinalIgnoreCase)))
        {
            return step.Type.Trim();
        }

        return null;
    }
}

public class TriggerResult
{
    public bool Success { get; set; }
    public string MissionCode { get; set; } = string.Empty;
    public string RequestId { get; set; } = string.Empty;
    public int? QueueId { get; set; }
    public bool ExecuteImmediately { get; set; }
    public string Message { get; set; } = string.Empty;
}

public class SavedCustomMissionConflictException : Exception
{
    public SavedCustomMissionConflictException(string message) : base(message)
    {
    }
}

public class SavedCustomMissionNotFoundException : Exception
{
    public SavedCustomMissionNotFoundException(string message) : base(message)
    {
    }
}

public class SavedCustomMissionValidationException : Exception
{
    public SavedCustomMissionValidationException(string message) : base(message)
    {
    }
}
