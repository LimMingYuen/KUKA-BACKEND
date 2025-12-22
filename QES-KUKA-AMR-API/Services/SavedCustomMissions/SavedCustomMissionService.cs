using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using QES_KUKA_AMR_API.Data;
using QES_KUKA_AMR_API.Data.Entities;
using QES_KUKA_AMR_API.Models.Missions;
using QES_KUKA_AMR_API.Options;
using QES_KUKA_AMR_API.Services.Queue;

namespace QES_KUKA_AMR_API.Services.SavedCustomMissions;

public interface ISavedCustomMissionService
{
    Task<List<SavedCustomMission>> GetAllAsync(bool includeInactive = false, CancellationToken cancellationToken = default);
    Task<SavedCustomMission?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<SavedCustomMission> CreateAsync(SavedCustomMission mission, string createdBy, CancellationToken cancellationToken = default);
    Task<SavedCustomMission?> UpdateAsync(int id, SavedCustomMission mission, CancellationToken cancellationToken = default);
    Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default);
    Task<TriggerResult> TriggerAsync(int id, string triggeredBy, CancellationToken cancellationToken = default);
    Task<SavedCustomMission?> ToggleStatusAsync(int id, CancellationToken cancellationToken = default);
    Task<SavedCustomMission?> AssignCategoryAsync(int templateId, int? categoryId, CancellationToken cancellationToken = default);
}

public class SavedCustomMissionService : ISavedCustomMissionService
{
    private readonly ApplicationDbContext _dbContext;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly MissionServiceOptions _missionOptions;
    private readonly ILogger<SavedCustomMissionService> _logger;
    private readonly IMissionQueueService _queueService;

    public SavedCustomMissionService(
        ApplicationDbContext dbContext,
        IHttpClientFactory httpClientFactory,
        IOptions<MissionServiceOptions> missionOptions,
        ILogger<SavedCustomMissionService> logger,
        IMissionQueueService queueService)
    {
        _dbContext = dbContext;
        _httpClientFactory = httpClientFactory;
        _missionOptions = missionOptions.Value;
        _logger = logger;
        _queueService = queueService;
    }

    public async Task<List<SavedCustomMission>> GetAllAsync(bool includeInactive = false, CancellationToken cancellationToken = default)
    {
        var query = _dbContext.SavedCustomMissions
            .Include(m => m.Category)
            .AsNoTracking();

        if (!includeInactive)
        {
            query = query.Where(m => m.IsActive);
        }

        return await query
            .OrderByDescending(m => m.CreatedUtc)
            .ToListAsync(cancellationToken);
    }

    public async Task<SavedCustomMission?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        return await _dbContext.SavedCustomMissions
            .Include(m => m.Category)
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
        mission.IsActive = true;  // Always create as active
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
        existing.OrgId = mission.OrgId;
        existing.ViewBoardType = mission.ViewBoardType;
        existing.TemplateCode = mission.TemplateCode;
        existing.LockRobotAfterFinish = mission.LockRobotAfterFinish;
        existing.UnlockRobotId = mission.UnlockRobotId;
        existing.UnlockMissionCode = mission.UnlockMissionCode;
        existing.MissionStepsJson = mission.MissionStepsJson;
        existing.IsActive = mission.IsActive;
        existing.ConcurrencyMode = mission.ConcurrencyMode;
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

        // Prevent deletion of active workflow templates
        if (entity.IsActive)
        {
            _logger.LogWarning("Cannot delete active workflow template {Id} ('{MissionName}'). Set to inactive first.",
                id, entity.MissionName);
            throw new SavedCustomMissionValidationException(
                "Cannot delete an active workflow template. Please set it to inactive first.");
        }

        // Soft delete
        entity.IsDeleted = true;
        entity.UpdatedUtc = DateTime.UtcNow;
        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Saved custom mission {Id} ('{MissionName}') soft deleted", id, entity.MissionName);

        return true;
    }

    public async Task<SavedCustomMission?> ToggleStatusAsync(int id, CancellationToken cancellationToken = default)
    {
        var entity = await _dbContext.SavedCustomMissions
            .FirstOrDefaultAsync(m => m.Id == id, cancellationToken);

        if (entity is null)
        {
            return null;
        }

        entity.IsActive = !entity.IsActive;
        entity.UpdatedUtc = DateTime.UtcNow;

        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Saved custom mission {Id} ('{MissionName}') status toggled to {IsActive}",
            id, entity.MissionName, entity.IsActive);

        return entity;
    }

    /// <summary>
    /// Triggers a saved custom mission by adding it to the mission queue.
    /// The QueueProcessorService will handle robot availability checking and submission to AMR.
    /// </summary>
    public async Task<TriggerResult> TriggerAsync(
        int id,
        string triggeredBy,
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

        // Build the submission request
        var submitRequest = new SubmitMissionRequest
        {
            OrgId = savedMission.OrgId ?? string.Empty,
            RequestId = requestId,
            MissionCode = missionCode,
            MissionType = savedMission.MissionType,
            ViewBoardType = savedMission.ViewBoardType ?? string.Empty,
            RobotModels = (IReadOnlyList<string>?)robotModels ?? Array.Empty<string>(),
            RobotIds = (IReadOnlyList<string>?)robotIds ?? Array.Empty<string>(),
            RobotType = savedMission.RobotType,
            Priority = savedMission.Priority,
            ContainerModelCode = savedMission.ContainerModelCode ?? string.Empty,
            ContainerCode = savedMission.ContainerCode ?? string.Empty,
            TemplateCode = savedMission.TemplateCode ?? string.Empty,
            LockRobotAfterFinish = savedMission.LockRobotAfterFinish,
            UnlockRobotId = savedMission.UnlockRobotId ?? string.Empty,
            UnlockMissionCode = savedMission.UnlockMissionCode ?? string.Empty,
            IdleNode = savedMission.IdleNode ?? string.Empty,
            MissionData = missionData
        };

        // Create queue item instead of direct submission
        // This ensures proper robot availability checking and queuing
        var queueItem = new MissionQueue
        {
            MissionCode = missionCode,
            RequestId = requestId,
            SavedMissionId = savedMission.Id,
            MissionName = savedMission.MissionName,
            MissionRequestJson = JsonSerializer.Serialize(submitRequest),
            Status = MissionQueueStatus.Queued,
            Priority = savedMission.Priority,
            CreatedBy = triggeredBy,
            RobotTypeFilter = savedMission.RobotType,
            PreferredRobotIds = savedMission.RobotIds
        };

        try
        {
            // Add to queue - QueueProcessorService will handle robot availability and submission
            var result = await _queueService.AddToQueueAsync(queueItem, cancellationToken);

            _logger.LogInformation(
                "Triggered saved custom mission {Id} ('{MissionName}') -> Added to queue with MissionCode: {MissionCode}, QueueId: {QueueId}, Position: {Position}",
                id, savedMission.MissionName, missionCode, result.Id, result.QueuePosition);

            return new TriggerResult
            {
                Success = true,
                MissionCode = missionCode,
                RequestId = requestId,
                QueueId = result.Id,
                ExecuteImmediately = false,
                Message = $"Mission queued at position {result.QueuePosition}. Will execute when robot is available."
            };
        }
        catch (ConcurrencyViolationException ex)
        {
            _logger.LogWarning(ex, "Concurrency violation while queuing mission {Id}", id);
            throw new SavedCustomMissionSubmissionException($"Cannot queue mission: {ex.Message}", ex);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error while adding mission to queue for saved mission {Id}", id);
            throw new SavedCustomMissionSubmissionException("Failed to add mission to queue.", ex);
        }
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

    /// <summary>
    /// Assigns a template to a category (or Uncategorized if categoryId is null)
    /// </summary>
    public async Task<SavedCustomMission?> AssignCategoryAsync(
        int templateId,
        int? categoryId,
        CancellationToken cancellationToken = default)
    {
        var template = await _dbContext.SavedCustomMissions
            .FirstOrDefaultAsync(m => m.Id == templateId, cancellationToken);

        if (template is null)
        {
            return null;
        }

        // Validate categoryId if provided
        if (categoryId.HasValue)
        {
            var categoryExists = await _dbContext.TemplateCategories
                .AnyAsync(c => c.Id == categoryId.Value, cancellationToken);

            if (!categoryExists)
            {
                throw new SavedCustomMissionValidationException($"Category with ID {categoryId.Value} does not exist.");
            }
        }

        template.CategoryId = categoryId;
        template.UpdatedUtc = DateTime.UtcNow;

        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Template {Id} ('{MissionName}') assigned to category {CategoryId}",
            templateId, template.MissionName, categoryId?.ToString() ?? "Uncategorized");

        // Reload with category
        return await _dbContext.SavedCustomMissions
            .Include(m => m.Category)
            .AsNoTracking()
            .FirstOrDefaultAsync(m => m.Id == templateId, cancellationToken);
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

public class SavedCustomMissionSubmissionException : Exception
{
    public SavedCustomMissionSubmissionException(string message) : base(message)
    {
    }

    public SavedCustomMissionSubmissionException(string message, Exception innerException) : base(message, innerException)
    {
    }
}
