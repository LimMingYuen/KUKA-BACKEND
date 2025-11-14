using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using QES_KUKA_AMR_API.Data;
using QES_KUKA_AMR_API.Data.Entities;
using QES_KUKA_AMR_API.Models.Missions;
using QES_KUKA_AMR_API.Options;

namespace QES_KUKA_AMR_API.Services;

public interface IQueueService
{
    Task InitializeAsync(CancellationToken cancellationToken = default);
    Task<bool> CanExecuteNowAsync(CancellationToken cancellationToken = default);
    Task<QueueResult> EnqueueMissionAsync(EnqueueRequest request, CancellationToken cancellationToken = default);
    Task<bool> ProcessNextAsync(CancellationToken cancellationToken = default);
    Task OnMissionCompletedAsync(string missionCode, bool success, string? errorMessage = null, CancellationToken cancellationToken = default);
    Task<List<MissionQueue>> GetQueuedMissionsAsync(CancellationToken cancellationToken = default);
    Task<QueueStats> GetQueueStatsAsync(CancellationToken cancellationToken = default);
    Task<bool> UpdatePriorityAsync(int queueId, int newPriority, CancellationToken cancellationToken = default);
    Task<int> GetSmartPriorityAsync(string priorityLevel, CancellationToken cancellationToken = default);
    Task<bool> RemoveQueuedMissionAsync(int queueId, CancellationToken cancellationToken = default);
    Task<bool> CancelProcessingMissionAsync(int queueId, CancellationToken cancellationToken = default);
    Task<ResumeManualWaypointResult> ResumeManualWaypointAsync(string missionCode, CancellationToken cancellationToken = default);
}

public class QueueService : IQueueService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<QueueService> _logger;
    private readonly MissionQueueSettings _settings;

    // CRITICAL: Static semaphores shared across ALL instances
    // _globalSemaphore: Controls global concurrency limit (e.g., max 30 concurrent missions)
    // _dbLock: Serializes queue state modifications to prevent race conditions
    // QueueService is now Singleton, so these are shared across entire app lifetime
    private static SemaphoreSlim? _globalSemaphore;
    private static readonly SemaphoreSlim _dbLock = new(1, 1);
    private static readonly SemaphoreSlim _initLock = new(1, 1);
    private static bool _isInitialized = false;

    public QueueService(
        IServiceProvider serviceProvider,
        ILogger<QueueService> logger,
        IOptions<MissionQueueSettings> settings)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
        _settings = settings.Value;

        // Initialize static semaphore only once (InitializeAsync will sync with database)
        if (_globalSemaphore == null)
        {
            _globalSemaphore = new SemaphoreSlim(_settings.GlobalConcurrencyLimit, _settings.GlobalConcurrencyLimit);
        }
    }

    /// <summary>
    /// CRITICAL: Initialize semaphore state by syncing with database.
    /// This prevents SemaphoreFullException when app restarts with Processing missions in DB.
    /// MUST be called once during app startup before processing any missions.
    /// </summary>
    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        // Check if already initialized (early return without lock for performance)
        if (_isInitialized)
        {
            _logger.LogInformation("QueueService already initialized. Skipping.");
            return;
        }

        // Use semaphore to ensure only one thread initializes
        await _initLock.WaitAsync(cancellationToken);
        try
        {
            // Double-check after acquiring lock
            if (_isInitialized)
            {
                _logger.LogInformation("QueueService already initialized by another thread. Skipping.");
                return;
            }

            _logger.LogInformation("=== QueueService.InitializeAsync ===");
            _logger.LogInformation("Initializing semaphore state from database...");

            // Create scope for database access
            using var scope = _serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            // Count missions that are currently in Processing status
            var processingCount = await context.MissionQueues
                .CountAsync(m => m.Status == QueueStatus.Processing, cancellationToken);

            _logger.LogInformation("Found {ProcessingCount} missions in Processing status from previous app instance",
                processingCount);

            // Acquire semaphore slots for each Processing mission to sync in-memory state with database
            // This ensures that when these missions complete, they can properly release their slots
            if (processingCount > 0)
            {
                if (processingCount > _settings.GlobalConcurrencyLimit)
                {
                    _logger.LogError("⚠️ CRITICAL: Database has {ProcessingCount} Processing missions, " +
                        "but limit is {Limit}! This indicates data corruption. " +
                        "Marking excess missions as Failed to recover.",
                        processingCount, _settings.GlobalConcurrencyLimit);

                    // Delete excess missions to recover from invalid state
                    var excessMissions = await context.MissionQueues
                        .Where(m => m.Status == QueueStatus.Processing)
                        .OrderBy(m => m.ProcessedDate)
                        .Skip(_settings.GlobalConcurrencyLimit)
                        .ToListAsync(cancellationToken);

                    foreach (var mission in excessMissions)
                    {
                        // Create MissionHistory record for the mission before deleting it
                        var missionHistory = new MissionHistory
                        {
                            MissionCode = mission.MissionCode,
                            RequestId = mission.RequestId,
                            WorkflowId = mission.WorkflowId,
                            WorkflowName = mission.WorkflowName ?? mission.MissionCode,
                            Status = "Failed",
                            MissionType = mission.WorkflowId.HasValue ? "Workflow" : "Custom",
                            CreatedDate = mission.CreatedDate,
                            CompletedDate = DateTime.UtcNow,
                            ErrorMessage = "Marked as Failed during app restart due to exceeding concurrency limit",
                            CreatedBy = mission.CreatedBy
                        };
                        context.MissionHistories.Add(missionHistory);

                        // Remove the mission from the queue
                        context.MissionQueues.Remove(mission);
                        _logger.LogWarning("Deleting mission {MissionCode} (was orphaned from previous instance)",
                            mission.MissionCode);
                    }

                    await context.SaveChangesAsync(cancellationToken);
                    processingCount = _settings.GlobalConcurrencyLimit;
                }

                // Acquire semaphore slots for legitimate Processing missions
                for (int i = 0; i < processingCount; i++)
                {
                    await _globalSemaphore.WaitAsync(cancellationToken);
                }

                _logger.LogInformation("✓ Acquired {Count} semaphore slots to match database state. " +
                    "Semaphore: {Available}/{Limit} available",
                    processingCount, _globalSemaphore.CurrentCount, _settings.GlobalConcurrencyLimit);
            }
            else
            {
                _logger.LogInformation("✓ No Processing missions found. Semaphore starts at full capacity: {Limit}/{Max}",
                    _settings.GlobalConcurrencyLimit, _settings.GlobalConcurrencyLimit);
            }

            // Mark as initialized
            _isInitialized = true;
            _logger.LogInformation("=== QueueService initialized successfully ===");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "✗ FATAL: Failed to initialize QueueService. App may experience semaphore issues!");
            throw;
        }
        finally
        {
            _initLock.Release();
        }
    }

    public async Task<bool> CanExecuteNowAsync(CancellationToken cancellationToken = default)
    {
        using var scope = _serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var processingCount = await context.MissionQueues
            .CountAsync(m => m.Status == QueueStatus.Processing, cancellationToken);

        var available = _settings.GlobalConcurrencyLimit - processingCount;
        _logger.LogInformation("Current processing: {ProcessingCount}/{Limit}, Available: {Available}",
            processingCount, _settings.GlobalConcurrencyLimit, available);

        return available > 0;
    }

    public async Task<QueueResult> EnqueueMissionAsync(EnqueueRequest request, CancellationToken cancellationToken = default)
    {
        await _dbLock.WaitAsync(cancellationToken);
        try
        {
            using var scope = _serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            _logger.LogInformation("=== QueueService.EnqueueMissionAsync ===");
            _logger.LogInformation("Request: WorkflowId={WorkflowId}, MissionCode={MissionCode}, Priority={Priority}",
                request.WorkflowId, request.MissionCode, request.Priority);

            // Check if mission already exists
            var existingMission = await context.MissionQueues
                .FirstOrDefaultAsync(m => m.MissionCode == request.MissionCode, cancellationToken);

            if (existingMission != null)
            {
                _logger.LogWarning("Mission {MissionCode} already exists in queue with status {Status}",
                    request.MissionCode, existingMission.Status);
                return new QueueResult
                {
                    Success = false,
                    ExecuteImmediately = false,
                    Message = $"Mission {request.MissionCode} already exists in queue",
                    QueuePosition = null
                };
            }

            // Check if we can execute immediately
            var processingCount = await context.MissionQueues
                .CountAsync(m => m.Status == QueueStatus.Processing, cancellationToken);

            // Check if this workflow is already Processing (prevent same workflow from running twice)
            // Skip this check for custom missions (WorkflowId == null)
            var workflowAlreadyProcessing = false;
            if (request.WorkflowId.HasValue)
            {
                workflowAlreadyProcessing = await context.MissionQueues
                    .AnyAsync(m => m.Status == QueueStatus.Processing
                               && m.WorkflowId == request.WorkflowId, cancellationToken);

                if (workflowAlreadyProcessing)
                {
                    _logger.LogInformation("Workflow {WorkflowId} ({WorkflowName}) is already Processing. New mission will be queued.",
                        request.WorkflowId, request.WorkflowName);
                }
            }

            var canExecuteNow = processingCount < _settings.GlobalConcurrencyLimit && !workflowAlreadyProcessing;

            // CRITICAL: Acquire semaphore BEFORE saving to database to ensure atomicity
            // This prevents orphaned Processing records without acquired semaphore slots
            bool semaphoreAcquired = false;
            if (canExecuteNow)
            {
                await _globalSemaphore.WaitAsync(cancellationToken);
                semaphoreAcquired = true;
                _logger.LogInformation("✓ Semaphore acquired for mission {MissionCode} (Slot {Current}/{Max})",
                    request.MissionCode, processingCount + 1, _settings.GlobalConcurrencyLimit);
            }

            try
            {
                var queueItem = new MissionQueue
                {
                    WorkflowId = request.WorkflowId,
                    WorkflowCode = request.WorkflowCode,
                    WorkflowName = request.WorkflowName,
                    SavedMissionId = request.SavedMissionId,
                    TriggerSource = request.TriggerSource,
                    MissionCode = request.MissionCode,
                    TemplateCode = request.TemplateCode,
                    Priority = request.Priority ?? _settings.DefaultPriority,
                    RequestId = request.RequestId,
                    Status = canExecuteNow ? QueueStatus.Processing : QueueStatus.Queued,
                    CreatedDate = DateTime.UtcNow,
                    ProcessedDate = canExecuteNow ? DateTime.UtcNow : null,
                    CreatedBy = request.CreatedBy,

                    // Custom mission fields
                    RobotModels = request.RobotModels != null && request.RobotModels.Any()
                        ? JsonSerializer.Serialize(request.RobotModels)
                        : null,
                    RobotIds = request.RobotIds != null && request.RobotIds.Any()
                        ? JsonSerializer.Serialize(request.RobotIds)
                        : null,
                    RobotType = request.RobotType,
                    MissionType = request.MissionType,
                    ViewBoardType = request.ViewBoardType,
                    ContainerModelCode = request.ContainerModelCode,
                    ContainerCode = request.ContainerCode,
                    LockRobotAfterFinish = request.LockRobotAfterFinish,
                    UnlockRobotId = request.UnlockRobotId,
                    UnlockMissionCode = request.UnlockMissionCode,
                    IdleNode = request.IdleNode,
                    MissionDataJson = request.MissionData != null && request.MissionData.Any()
                        ? JsonSerializer.Serialize(request.MissionData)
                        : null
                };

                context.MissionQueues.Add(queueItem);
                await context.SaveChangesAsync(cancellationToken);

                if (canExecuteNow)
                {
                    _logger.LogInformation("✓ Mission {MissionCode} saved with Processing status (Slot {Current}/{Max})",
                        request.MissionCode, processingCount + 1, _settings.GlobalConcurrencyLimit);

                    return new QueueResult
                    {
                        Success = true,
                        ExecuteImmediately = true,
                        Message = "Mission will execute immediately",
                        QueuePosition = null,
                        QueueId = queueItem.Id
                    };
                }
                else
                {
                    var queuePosition = await GetQueuePositionAsync(context, queueItem.Id, cancellationToken);
                    _logger.LogInformation("✓ Mission {MissionCode} queued at position {Position} (Priority: {Priority})",
                        request.MissionCode, queuePosition, queueItem.Priority);

                    return new QueueResult
                    {
                        Success = true,
                        ExecuteImmediately = false,
                        Message = $"Mission queued at position {queuePosition}",
                        QueuePosition = queuePosition,
                        QueueId = queueItem.Id
                    };
                }
            }
            catch (Exception saveEx)
            {
                // If we acquired semaphore but failed to save, release it back
                if (semaphoreAcquired)
                {
                    _globalSemaphore.Release();
                    _logger.LogError(saveEx, "✗ Failed to save mission {MissionCode} to database. Semaphore slot released.",
                        request.MissionCode);
                }
                else
                {
                    _logger.LogError(saveEx, "✗ Error enqueueing mission {MissionCode}", request.MissionCode);
                }

                return new QueueResult
                {
                    Success = false,
                    ExecuteImmediately = false,
                    Message = $"Error enqueueing mission: {saveEx.Message}",
                    QueuePosition = null
                };
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in EnqueueMissionAsync for {MissionCode}", request.MissionCode);
            return new QueueResult
            {
                Success = false,
                ExecuteImmediately = false,
                Message = $"Error enqueueing mission: {ex.Message}",
                QueuePosition = null
            };
        }
        finally
        {
            _dbLock.Release();
        }
    }

    public async Task<bool> ProcessNextAsync(CancellationToken cancellationToken = default)
    {
        await _dbLock.WaitAsync(cancellationToken);
        bool semaphoreAcquired = false;
        try
        {
            using var scope = _serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            _logger.LogInformation("=== QueueService.ProcessNextAsync ===");

            // Check if we have available slots
            var processingCount = await context.MissionQueues
                .CountAsync(m => m.Status == QueueStatus.Processing, cancellationToken);

            if (processingCount >= _settings.GlobalConcurrencyLimit)
            {
                _logger.LogInformation("No available slots ({Current}/{Max}). Skipping.",
                    processingCount, _settings.GlobalConcurrencyLimit);
                return false;
            }

            // Get IDs of workflows that are already Processing
            var processingWorkflowIds = await context.MissionQueues
                .Where(m => m.Status == QueueStatus.Processing)
                .Select(m => m.WorkflowId)
                .Distinct()
                .ToListAsync(cancellationToken);

            // Get next queued mission that is NOT from a workflow already Processing
            var nextMission = await context.MissionQueues
                .Where(m => m.Status == QueueStatus.Queued
                         && !processingWorkflowIds.Contains(m.WorkflowId))
                .OrderBy(m => m.Priority)
                .ThenBy(m => m.CreatedDate)
                .FirstOrDefaultAsync(cancellationToken);

            if (nextMission == null)
            {
                _logger.LogInformation("No queued missions available (all workflows already processing or queue empty)");
                return false;
            }

            // CRITICAL: Acquire semaphore BEFORE updating status to prevent orphaned Processing records
            await _globalSemaphore.WaitAsync(cancellationToken);
            semaphoreAcquired = true;
            _logger.LogInformation("✓ Semaphore acquired for queued mission {MissionCode} (Slot {Current}/{Max})",
                nextMission.MissionCode, processingCount + 1, _settings.GlobalConcurrencyLimit);

            try
            {
                // Update status to Processing
                var oldStatus = nextMission.Status;
                nextMission.Status = QueueStatus.Processing;
                nextMission.ProcessedDate = DateTime.UtcNow;
                await context.SaveChangesAsync(cancellationToken);

                _logger.LogInformation("✓ Mission {MissionCode} status changed: {OldStatus} → Processing " +
                    "(Priority: {Priority}, Slot: {Current}/{Max}, CurrentSemaphoreCount={SemCount})",
                    nextMission.MissionCode, oldStatus, nextMission.Priority, processingCount + 1,
                    _settings.GlobalConcurrencyLimit, _globalSemaphore.CurrentCount);

                return true;
            }
            catch (Exception saveEx)
            {
                // If we acquired semaphore but failed to save, release it back
                if (semaphoreAcquired)
                {
                    _globalSemaphore.Release();
                    semaphoreAcquired = false;
                    _logger.LogError(saveEx, "✗ Failed to save mission {MissionCode} status to Processing. Semaphore slot released.",
                        nextMission.MissionCode);
                }
                throw;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing next queued mission. SemaphoreAcquired={Acquired}", semaphoreAcquired);
            return false;
        }
        finally
        {
            _dbLock.Release();
        }
    }

    public async Task OnMissionCompletedAsync(string missionCode, bool success, string? errorMessage = null, CancellationToken cancellationToken = default)
    {
        await _dbLock.WaitAsync(cancellationToken);
        try
        {
            using var scope = _serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            _logger.LogInformation("=== QueueService.OnMissionCompletedAsync ===");
            _logger.LogInformation("MissionCode={MissionCode}, Success={Success}, Error={Error}, Timestamp={Timestamp}",
                missionCode, success, errorMessage, DateTime.UtcNow);

            var queueItem = await context.MissionQueues
                .FirstOrDefaultAsync(m => m.MissionCode == missionCode, cancellationToken);

            if (queueItem == null)
            {
                _logger.LogWarning("⚠️ MISSION NOT IN QUEUE - Mission {MissionCode} not found in queue. " +
                    "This might be normal if mission was manually removed or never enqueued. " +
                    "Completion request ignored. Success={Success}, Error={Error}",
                    missionCode, success, errorMessage);
                return;
            }

            // CRITICAL: Check current status to prevent duplicate semaphore releases
            if (queueItem.Status != QueueStatus.Processing)
            {
                _logger.LogWarning("⚠️ DUPLICATE COMPLETION ATTEMPT - Mission {MissionCode} is already in {CurrentStatus} status " +
                    "(CompletedDate={CompletedDate}, ProcessedDate={ProcessedDate}). " +
                    "Completion request ignored to prevent semaphore over-release. " +
                    "RequestedOutcome={RequestedOutcome}, RequestedSuccess={Success}, RequestedError={Error}",
                    missionCode, queueItem.Status, queueItem.CompletedDate, queueItem.ProcessedDate,
                    success ? "Completed" : "Failed", success, errorMessage);
                return;
            }

            // Only reach here if status is Processing - safe to delete and release
            _logger.LogInformation("✓ Mission {MissionCode} is in Processing status. Proceeding with completion. " +
                "ProcessedDate={ProcessedDate}, Success={Success}",
                missionCode, queueItem.ProcessedDate, success);

            // Create MissionHistory record before deleting the mission
            // CRITICAL FIX: Copy all tracking fields for robot utilization analytics
            var missionHistory = new MissionHistory
            {
                MissionCode = queueItem.MissionCode,
                RequestId = queueItem.RequestId,
                WorkflowId = queueItem.WorkflowId,
                WorkflowName = queueItem.WorkflowName ?? queueItem.MissionCode,
                SavedMissionId = queueItem.SavedMissionId,
                TriggerSource = queueItem.TriggerSource,
                Status = success ? "Completed" : "Failed",
                MissionType = queueItem.WorkflowId.HasValue ? "Workflow" : "Custom",
                CreatedDate = queueItem.CreatedDate,
                ProcessedDate = queueItem.ProcessedDate,
                SubmittedToAmrDate = queueItem.SubmittedToAmrDate,
                CompletedDate = DateTime.UtcNow,
                AssignedRobotId = queueItem.AssignedRobotId,
                ErrorMessage = errorMessage,
                CreatedBy = queueItem.CreatedBy
            };
            context.MissionHistories.Add(missionHistory);

            // Delete the mission from the queue so it won't appear in UI anymore
            context.MissionQueues.Remove(queueItem);

            await context.SaveChangesAsync(cancellationToken);

            // Check if queue is now empty and reset identity if needed
            var remainingQueueCount = await context.MissionQueues.CountAsync(cancellationToken);
            if (remainingQueueCount == 0)
            {
                try
                {
                    // Reset identity seed to start from 1 on next insert
                    await context.Database.ExecuteSqlRawAsync(
                        "DBCC CHECKIDENT ('MissionQueues', NORESEED);",
                        cancellationToken);
                    await context.Database.ExecuteSqlRawAsync(
                        "DBCC CHECKIDENT ('MissionQueues', RESEED, 0);",
                        cancellationToken);
                    _logger.LogInformation("✓ Queue is empty after mission completion - reset identity seed to 0 (next ID will be 1)");
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Could not reset identity seed after mission completion");
                }
            }

            // Release semaphore slot ONLY after successful database update
            _globalSemaphore.Release();

            _logger.LogInformation("✓ Mission {MissionCode} deleted from queue and recorded in history. " +
                "Semaphore slot released. CurrentSemaphoreCount={CurrentCount}/{MaxCount}",
                missionCode, _globalSemaphore.CurrentCount, _settings.GlobalConcurrencyLimit);

            // Try to process next queued mission
            _ = Task.Run(async () =>
            {
                try
                {
                    await ProcessNextAsync(cancellationToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error auto-processing next mission after completion");
                }
            }, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling mission completion for {MissionCode}", missionCode);
        }
        finally
        {
            _dbLock.Release();
        }
    }

    public async Task<List<MissionQueue>> GetQueuedMissionsAsync(CancellationToken cancellationToken = default)
    {
        using var scope = _serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        return await context.MissionQueues
            .Where(m => m.Status == QueueStatus.Queued)
            .OrderBy(m => m.Priority)
            .ThenBy(m => m.CreatedDate)
            .ToListAsync(cancellationToken);
    }

    public async Task<QueueStats> GetQueueStatsAsync(CancellationToken cancellationToken = default)
    {
        using var scope = _serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var stats = await context.MissionQueues
            .GroupBy(m => m.Status)
            .Select(g => new { Status = g.Key, Count = g.Count() })
            .ToListAsync(cancellationToken);

        var queuedCount = stats.FirstOrDefault(s => s.Status == QueueStatus.Queued)?.Count ?? 0;
        var processingCount = stats.FirstOrDefault(s => s.Status == QueueStatus.Processing)?.Count ?? 0;
        // Since completed, failed and cancelled missions are now deleted, we set their counts to 0
        var completedCount = 0;
        var failedCount = 0;
        var cancelledCount = 0;

        return new QueueStats
        {
            QueuedCount = queuedCount,
            ProcessingCount = processingCount,
            CompletedCount = completedCount,
            FailedCount = failedCount,
            CancelledCount = cancelledCount,
            AvailableSlots = _settings.GlobalConcurrencyLimit - processingCount,
            TotalSlots = _settings.GlobalConcurrencyLimit
        };
    }

    public async Task<bool> UpdatePriorityAsync(int queueId, int newPriority, CancellationToken cancellationToken = default)
    {
        // Validate priority range
        if (newPriority < 0 || newPriority > 10)
        {
            _logger.LogWarning("Invalid priority {Priority} for queue item {QueueId}. Must be between 0 and 10",
                newPriority, queueId);
            return false;
        }

        await _dbLock.WaitAsync(cancellationToken);
        try
        {
            using var scope = _serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            var queueItem = await context.MissionQueues
                .FirstOrDefaultAsync(m => m.Id == queueId, cancellationToken);

            if (queueItem == null)
            {
                _logger.LogWarning("Queue item {QueueId} not found", queueId);
                return false;
            }

            if (queueItem.Status != QueueStatus.Queued)
            {
                _logger.LogWarning("Cannot update priority for queue item {QueueId} with status {Status}",
                    queueId, queueItem.Status);
                return false;
            }

            var oldPriority = queueItem.Priority;
            queueItem.Priority = newPriority;
            await context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("✓ Queue item {QueueId} priority updated from {OldPriority} to {NewPriority}",
                queueId, oldPriority, newPriority);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating priority for queue item {QueueId}", queueId);
            return false;
        }
        finally
        {
            _dbLock.Release();
        }
    }

    public async Task<int> GetSmartPriorityAsync(string priorityLevel, CancellationToken cancellationToken = default)
    {
        await _dbLock.WaitAsync(cancellationToken);
        try
        {
            using var scope = _serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            // Get all queued missions
            var queuedPriorities = await context.MissionQueues
                .Where(m => m.Status == QueueStatus.Queued)
                .Select(m => m.Priority)
                .ToListAsync(cancellationToken);

            int assignedPriority;

            switch (priorityLevel.ToLower())
            {
                case "high":
                    // Try to find lowest available priority in range 0-3
                    assignedPriority = 0;
                    for (int p = 0; p <= 3; p++)
                    {
                        if (!queuedPriorities.Contains(p))
                        {
                            assignedPriority = p;
                            break;
                        }
                        assignedPriority = p; // If all taken, use the highest in range (3)
                    }
                    _logger.LogInformation("Smart priority 'high' assigned: {Priority}", assignedPriority);
                    break;

                case "medium":
                    // Always use priority 5
                    assignedPriority = 5;
                    _logger.LogInformation("Smart priority 'medium' assigned: {Priority}", assignedPriority);
                    break;

                case "low":
                    // Try to find highest available priority in range 8-10
                    assignedPriority = 10;
                    for (int p = 10; p >= 8; p--)
                    {
                        if (!queuedPriorities.Contains(p))
                        {
                            assignedPriority = p;
                            break;
                        }
                        assignedPriority = p; // If all taken, use the lowest in range (8)
                    }
                    _logger.LogInformation("Smart priority 'low' assigned: {Priority}", assignedPriority);
                    break;

                default:
                    _logger.LogWarning("Invalid priority level: {PriorityLevel}. Defaulting to medium (5)", priorityLevel);
                    assignedPriority = 5;
                    break;
            }

            return assignedPriority;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calculating smart priority for level {PriorityLevel}", priorityLevel);
            return 5; // Default to medium priority on error
        }
        finally
        {
            _dbLock.Release();
        }
    }

    public async Task<bool> RemoveQueuedMissionAsync(int queueId, CancellationToken cancellationToken = default)
    {
        await _dbLock.WaitAsync(cancellationToken);
        try
        {
            using var scope = _serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            var queueItem = await context.MissionQueues
                .FirstOrDefaultAsync(m => m.Id == queueId, cancellationToken);

            if (queueItem == null)
            {
                _logger.LogWarning("Queue item {QueueId} not found", queueId);
                return false;
            }

            if (queueItem.Status != QueueStatus.Queued)
            {
                _logger.LogWarning("Cannot remove queue item {QueueId} with status {Status}",
                    queueId, queueItem.Status);
                return false;
            }

            context.MissionQueues.Remove(queueItem);
            await context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("✓ Queue item {QueueId} (Mission: {MissionCode}) removed from queue",
                queueId, queueItem.MissionCode);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing queue item {QueueId}", queueId);
            return false;
        }
        finally
        {
            _dbLock.Release();
        }
    }

    public async Task<bool> CancelProcessingMissionAsync(int queueId, CancellationToken cancellationToken = default)
    {
        await _dbLock.WaitAsync(cancellationToken);
        try
        {
            using var scope = _serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            _logger.LogInformation("=== QueueService.CancelProcessingMissionAsync ===");
            _logger.LogInformation("QueueId={QueueId}, Timestamp={Timestamp}", queueId, DateTime.UtcNow);

            var queueItem = await context.MissionQueues
                .FirstOrDefaultAsync(m => m.Id == queueId, cancellationToken);

            if (queueItem == null)
            {
                _logger.LogWarning("⚠️ Queue item {QueueId} not found", queueId);
                return false;
            }

            // CRITICAL: Only cancel missions that are actually Processing
            if (queueItem.Status != QueueStatus.Processing)
            {
                _logger.LogWarning("⚠️ Cannot cancel queue item {QueueId} (Mission: {MissionCode}) with status {Status}. " +
                    "Only Processing missions can be cancelled.",
                    queueId, queueItem.MissionCode, queueItem.Status);
                return false;
            }

            // Create MissionHistory record for cancelled mission before deleting
            var missionHistory = new MissionHistory
            {
                MissionCode = queueItem.MissionCode,
                RequestId = queueItem.RequestId,
                WorkflowId = queueItem.WorkflowId,
                WorkflowName = queueItem.WorkflowName ?? queueItem.MissionCode,
                Status = "Cancelled",
                MissionType = queueItem.WorkflowId.HasValue ? "Workflow" : "Custom",
                CreatedDate = queueItem.CreatedDate,
                CompletedDate = DateTime.UtcNow,
                ErrorMessage = "Mission cancelled by user",
                CreatedBy = queueItem.CreatedBy
            };
            context.MissionHistories.Add(missionHistory);

            // Delete the mission from the queue so it won't appear in UI anymore
            context.MissionQueues.Remove(queueItem);

            await context.SaveChangesAsync(cancellationToken);

            // Release semaphore slot ONLY after successful database update
            _globalSemaphore.Release();

            _logger.LogInformation("✓ Mission {MissionCode} deleted from queue and recorded in history. " +
                "Semaphore slot released. CurrentSemaphoreCount={CurrentCount}/{MaxCount}",
                queueItem.MissionCode, _globalSemaphore.CurrentCount, _settings.GlobalConcurrencyLimit);

            // Try to process next queued mission
            _ = Task.Run(async () =>
            {
                try
                {
                    await ProcessNextAsync(cancellationToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error auto-processing next mission after cancellation");
                }
            }, cancellationToken);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error cancelling queue item {QueueId}", queueId);
            return false;
        }
        finally
        {
            _dbLock.Release();
        }
    }

    private async Task<int> GetQueuePositionAsync(ApplicationDbContext context, int queueId, CancellationToken cancellationToken = default)
    {
        var queueItem = await context.MissionQueues
            .FirstOrDefaultAsync(m => m.Id == queueId, cancellationToken);

        if (queueItem == null || queueItem.Status != QueueStatus.Queued)
        {
            return 0;
        }

        var position = await context.MissionQueues
            .Where(m => m.Status == QueueStatus.Queued)
            .Where(m => m.Priority > queueItem.Priority ||
                       (m.Priority == queueItem.Priority && m.CreatedDate < queueItem.CreatedDate))
            .CountAsync(cancellationToken);

        return position + 1;
    }

    /// <summary>
    /// Resumes a mission that is paused at a manual waypoint by sending operation feedback
    /// </summary>
    public async Task<ResumeManualWaypointResult> ResumeManualWaypointAsync(
        string missionCode,
        CancellationToken cancellationToken = default)
    {
        using var scope = _serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var httpClientFactory = scope.ServiceProvider.GetRequiredService<IHttpClientFactory>();
        var missionOptions = scope.ServiceProvider.GetRequiredService<IOptions<MissionServiceOptions>>().Value;

        await _dbLock.WaitAsync(cancellationToken);
        try
        {
            // Find mission
            var mission = await context.MissionQueues
                .FirstOrDefaultAsync(m => m.MissionCode == missionCode, cancellationToken);

            if (mission == null)
            {
                _logger.LogWarning("Cannot resume mission {MissionCode} - not found", missionCode);
                return new ResumeManualWaypointResult
                {
                    Success = false,
                    Message = "Mission not found"
                };
            }

            _logger.LogDebug("ResumeManualWaypointAsync - Mission {MissionCode} state before resume: IsWaitingForManualResume={IsWaiting}, CurrentManualWaypointPosition={Position}, AssignedRobotId={RobotId}",
                missionCode,
                mission.IsWaitingForManualResume,
                mission.CurrentManualWaypointPosition ?? "null",
                mission.AssignedRobotId ?? "null");

            if (!mission.IsWaitingForManualResume)
            {
                _logger.LogWarning("Cannot resume mission {MissionCode} - not waiting for manual resume", missionCode);
                return new ResumeManualWaypointResult
                {
                    Success = false,
                    Message = "Mission is not waiting for manual resume"
                };
            }

            if (string.IsNullOrWhiteSpace(mission.CurrentManualWaypointPosition))
            {
                _logger.LogError("Mission {MissionCode} is waiting but has no CurrentManualWaypointPosition", missionCode);
                return new ResumeManualWaypointResult
                {
                    Success = false,
                    Message = "No manual waypoint position recorded"
                };
            }

            // Generate new requestId for operation feedback
            var timestamp = DateTime.UtcNow.ToString("yyyyMMddHHmmss");
            var requestId = $"request{timestamp}";

            // Call operation-feedback API
            var feedbackRequest = new Models.Missions.OperationFeedbackRequest
            {
                RequestId = requestId,
                MissionCode = missionCode,
                ContainerCode = mission.ContainerCode ?? string.Empty,
                Position = mission.CurrentManualWaypointPosition
            };

            _logger.LogInformation("▶️ Sending operation feedback to resume mission {MissionCode} at position {Position}. RequestId={RequestId}",
                missionCode, mission.CurrentManualWaypointPosition, requestId);

            try
            {
                var httpClient = httpClientFactory.CreateClient();
                var response = await httpClient.PostAsJsonAsync(
                    missionOptions.OperationFeedbackUrl,
                    feedbackRequest,
                    cancellationToken);

                _logger.LogInformation("ResumeManualWaypointAsync - Operation feedback response status for mission {MissionCode}: {StatusCode}",
                    missionCode,
                    response.StatusCode);

                var feedbackResponse = await response.Content.ReadFromJsonAsync<Models.Missions.OperationFeedbackResponse>(
                    cancellationToken: cancellationToken);

                _logger.LogInformation("ResumeManualWaypointAsync - Operation feedback response body for mission {MissionCode}: Success={Success}, Message={Message}",
                    missionCode,
                    feedbackResponse?.Success,
                    feedbackResponse?.Message ?? "null");

                if (feedbackResponse == null || !feedbackResponse.Success)
                {
                    _logger.LogError("Operation feedback failed for mission {MissionCode}. Response: {Response}",
                        missionCode, feedbackResponse?.Message ?? "No response");

                    return new ResumeManualWaypointResult
                    {
                        Success = false,
                        Message = $"Operation feedback failed: {feedbackResponse?.Message ?? "Unknown error"}"
                    };
                }

                _logger.LogInformation("✓ Operation feedback successful for mission {MissionCode}", missionCode);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calling operation feedback API for mission {MissionCode}", missionCode);
                return new ResumeManualWaypointResult
                {
                    Success = false,
                    Message = $"Error calling operation feedback API: {ex.Message}"
                };
            }

            // Add current waypoint to visited list
            List<string> visitedWaypoints = new();
            if (!string.IsNullOrWhiteSpace(mission.VisitedManualWaypointsJson))
            {
                try
                {
                    visitedWaypoints = JsonSerializer.Deserialize<List<string>>(mission.VisitedManualWaypointsJson) ?? new List<string>();
                }
                catch (JsonException ex)
                {
                    _logger.LogWarning(ex, "Failed to parse VisitedManualWaypointsJson for mission {MissionCode}", missionCode);
                }
            }

            if (!string.IsNullOrWhiteSpace(mission.CurrentManualWaypointPosition) &&
                !visitedWaypoints.Contains(mission.CurrentManualWaypointPosition, StringComparer.OrdinalIgnoreCase))
            {
                visitedWaypoints.Add(mission.CurrentManualWaypointPosition);
                mission.VisitedManualWaypointsJson = JsonSerializer.Serialize(visitedWaypoints);
                _logger.LogInformation("✓ Marked waypoint '{Waypoint}' as visited for mission {MissionCode}. Total visited: {Count}",
                    mission.CurrentManualWaypointPosition, missionCode, visitedWaypoints.Count);
            }

            // Close active manual pause window if recorded
            var activePause = await context.RobotManualPauses
                .Where(p => p.MissionCode == mission.MissionCode && p.PauseEndUtc == null)
                .OrderByDescending(p => p.PauseStartUtc)
                .FirstOrDefaultAsync(cancellationToken);

            if (activePause != null)
            {
                var pauseClosedAtUtc = DateTime.UtcNow;
                activePause.PauseEndUtc = pauseClosedAtUtc;
                activePause.UpdatedUtc = pauseClosedAtUtc;
                _logger.LogInformation("✓ Closed manual pause for mission {MissionCode}. Start={Start}, End={End}",
                    missionCode, activePause.PauseStartUtc, activePause.PauseEndUtc);
            }

            // Clear waiting flags
            mission.IsWaitingForManualResume = false;
            mission.CurrentManualWaypointPosition = null;
            await context.SaveChangesAsync(cancellationToken);

            _logger.LogDebug("ResumeManualWaypointAsync - Mission {MissionCode} state after resume: IsWaitingForManualResume={IsWaiting}, CurrentManualWaypointPosition={Position}",
                missionCode,
                mission.IsWaitingForManualResume,
                mission.CurrentManualWaypointPosition ?? "null");

            _logger.LogInformation("✓ Mission {MissionCode} resumed successfully. Robot will continue mission.", missionCode);

            return new ResumeManualWaypointResult
            {
                Success = true,
                Message = "Mission resumed successfully",
                RequestId = requestId
            };
        }
        finally
        {
            _dbLock.Release();
        }
    }
}

// Request/Response Models
public class EnqueueRequest
{
    // Template-based mission fields (nullable for custom missions)
    public int? WorkflowId { get; set; }
    public string? WorkflowCode { get; set; }
    public string? WorkflowName { get; set; }
    public string? TemplateCode { get; set; }

    // Mission source tracking
    public int? SavedMissionId { get; set; }
    public MissionTriggerSource TriggerSource { get; set; } = MissionTriggerSource.Manual;

    // Common fields (required for all missions)
    public string MissionCode { get; set; } = string.Empty;
    public int? Priority { get; set; }
    public string RequestId { get; set; } = string.Empty;
    public string CreatedBy { get; set; } = string.Empty;

    // Custom mission fields (nullable, used when missionData is provided)
    public List<string>? RobotModels { get; set; }
    public List<string>? RobotIds { get; set; }
    public string? RobotType { get; set; }
    public string? MissionType { get; set; }
    public string? ViewBoardType { get; set; }
    public string? ContainerModelCode { get; set; }
    public string? ContainerCode { get; set; }
    public bool LockRobotAfterFinish { get; set; }
    public string? UnlockRobotId { get; set; }
    public string? UnlockMissionCode { get; set; }
    public string? IdleNode { get; set; }
    public List<MissionDataItem>? MissionData { get; set; }
}

public class QueueResult
{
    public bool Success { get; set; }
    public bool ExecuteImmediately { get; set; }
    public string Message { get; set; } = string.Empty;
    public int? QueuePosition { get; set; }
    public int? QueueId { get; set; }
}

public class QueueStats
{
    public int QueuedCount { get; set; }
    public int ProcessingCount { get; set; }
    public int CompletedCount { get; set; }
    public int FailedCount { get; set; }
    public int CancelledCount { get; set; }
    public int AvailableSlots { get; set; }
    public int TotalSlots { get; set; }
}

public class ResumeManualWaypointResult
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public string? RequestId { get; set; }
}
