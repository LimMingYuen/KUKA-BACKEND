using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using QES_KUKA_AMR_API.Data;
using QES_KUKA_AMR_API.Data.Entities;
using QES_KUKA_AMR_API.Models.Missions;

namespace QES_KUKA_AMR_API.Services.Queue;

/// <summary>
/// Converts mission submission requests into queue items
/// </summary>
public interface IMissionEnqueueService
{
    /// <summary>
    /// Enqueues a mission submission request.
    /// Returns list of created queue items (multiple for multi-map workflows).
    /// </summary>
    Task<List<MissionQueueItem>> EnqueueMissionAsync(
        SubmitMissionRequest request,
        MissionTriggerSource triggerSource = MissionTriggerSource.Direct,
        CancellationToken cancellationToken = default);
}

public class MissionEnqueueService : IMissionEnqueueService
{
    private readonly ApplicationDbContext _dbContext;
    private readonly IWorkflowAnalysisService _workflowAnalysisService;
    private readonly ILogger<MissionEnqueueService> _logger;
    private readonly TimeProvider _timeProvider;

    public MissionEnqueueService(
        ApplicationDbContext dbContext,
        IWorkflowAnalysisService workflowAnalysisService,
        ILogger<MissionEnqueueService> logger,
        TimeProvider timeProvider)
    {
        _dbContext = dbContext;
        _workflowAnalysisService = workflowAnalysisService;
        _logger = logger;
        _timeProvider = timeProvider;
    }

    public async Task<List<MissionQueueItem>> EnqueueMissionAsync(
        SubmitMissionRequest request,
        MissionTriggerSource triggerSource = MissionTriggerSource.Direct,
        CancellationToken cancellationToken = default)
    {
        var now = _timeProvider.GetUtcNow().UtcDateTime;

        // Determine if this is custom mission (has missionData) or workflow-based (has templateCode)
        var hasCustomMissionData = request.MissionData != null && request.MissionData.Any();
        var hasTemplateCode = !string.IsNullOrWhiteSpace(request.TemplateCode);

        if (hasCustomMissionData)
        {
            // Custom mission with explicit mission data
            _logger.LogInformation(
                "Enqueuing custom mission: MissionCode={MissionCode}, Steps={StepCount}",
                request.MissionCode,
                request.MissionData!.Count
            );

            return await EnqueueCustomMissionAsync(request, triggerSource, now, cancellationToken);
        }
        else if (hasTemplateCode)
        {
            // Workflow-based mission (templateCode only)
            _logger.LogInformation(
                "Enqueuing workflow-based mission: MissionCode={MissionCode}, TemplateCode={TemplateCode}",
                request.MissionCode,
                request.TemplateCode
            );

            return await EnqueueWorkflowMissionAsync(request, triggerSource, now, cancellationToken);
        }
        else
        {
            throw new InvalidOperationException(
                "Mission request must have either MissionData or TemplateCode");
        }
    }

    /// <summary>
    /// Enqueues a custom mission with explicit mission data steps.
    /// Analyzes steps to detect multi-map workflows and creates linked queue items.
    /// </summary>
    private async Task<List<MissionQueueItem>> EnqueueCustomMissionAsync(
        SubmitMissionRequest request,
        MissionTriggerSource triggerSource,
        DateTime now,
        CancellationToken cancellationToken)
    {
        // Analyze mission steps to detect MapCode segments
        var missionDataList = request.MissionData!.ToList();
        var analysis = await _workflowAnalysisService.AnalyzeMissionStepsAsync(
            missionDataList,
            cancellationToken
        );

        // Validate that queue is enabled for all MapCodes in this mission
        var uniqueMapCodes = analysis.Segments.Select(s => s.MapCode).Distinct().ToList();
        var queueConfigs = await _dbContext.MapCodeQueueConfigurations
            .Where(c => uniqueMapCodes.Contains(c.MapCode) && c.EnableQueue)
            .Select(c => c.MapCode)
            .ToListAsync(cancellationToken);

        var missingMapCodes = uniqueMapCodes.Except(queueConfigs).ToList();
        if (missingMapCodes.Any())
        {
            throw new InvalidOperationException(
                $"Queue not enabled for MapCode(s): {string.Join(", ", missingMapCodes)}. Please configure MapCodeQueueConfiguration for these MapCodes.");
        }

        _logger.LogDebug(
            "Queue validation passed for {Count} MapCode(s): {MapCodes}",
            uniqueMapCodes.Count,
            string.Join(", ", uniqueMapCodes)
        );

        var queueItems = new List<MissionQueueItem>();
        MissionQueueItem? previousItem = null;

        foreach (var segment in analysis.Segments)
        {
            // Generate unique queue item code
            var queueItemCode = analysis.IsMultiMap
                ? $"{request.MissionCode}_SEG{segment.SegmentIndex + 1}"
                : request.MissionCode;

            // Serialize mission steps for this segment
            var segmentStepsJson = JsonSerializer.Serialize(segment.Steps);

            var queueItem = new MissionQueueItem
            {
                QueueItemCode = queueItemCode,
                MissionCode = request.MissionCode,
                MissionType = request.MissionType,
                RobotType = request.RobotType,
                Priority = request.Priority,

                // MapCode assignment
                PrimaryMapCode = segment.MapCode,
                SecondaryMapCode = null, // Only set for cross-map transitions

                // Mission data
                MissionStepsJson = segmentStepsJson,
                TemplateCode = null, // Custom missions don't use templates

                // Robot constraints
                RobotModels = request.RobotModels?.Any() == true
                    ? string.Join(",", request.RobotModels)
                    : null,
                RobotIds = request.RobotIds?.Any() == true
                    ? string.Join(",", request.RobotIds)
                    : null,

                // Additional parameters
                ContainerModelCode = request.ContainerModelCode,
                ContainerCode = request.ContainerCode,
                IdleNode = request.IdleNode,
                OrgId = request.OrgId,
                ViewBoardType = request.ViewBoardType,
                LockRobotAfterFinish = request.LockRobotAfterFinish,
                UnlockRobotId = request.UnlockRobotId,
                UnlockMissionCode = request.UnlockMissionCode,

                // First node coordinates for distance calculations
                StartNodeLabel = segment.StartNodeLabel,
                StartXCoordinate = segment.StartXCoordinate,
                StartYCoordinate = segment.StartYCoordinate,

                // Multi-map linking
                ParentQueueItemId = previousItem?.Id,
                NextQueueItemId = null, // Set after saving
                IsOpportunisticJob = false,

                // Status and tracking
                Status = MissionQueueStatus.Pending,
                TriggerSource = triggerSource,
                EnqueuedUtc = now,
                CreatedUtc = now,
                UpdatedUtc = now
            };

            queueItems.Add(queueItem);

            // Link previous item to this one
            if (previousItem != null)
            {
                previousItem.NextQueueItemId = queueItem.Id; // Will be set after save
            }

            previousItem = queueItem;
        }

        // Save all queue items
        await _dbContext.MissionQueueItems.AddRangeAsync(queueItems, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);

        // Update NextQueueItemId links now that we have IDs
        for (int i = 0; i < queueItems.Count - 1; i++)
        {
            queueItems[i].NextQueueItemId = queueItems[i + 1].Id;
        }
        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Enqueued custom mission {MissionCode}: {QueueItemCount} queue item(s), IsMultiMap={IsMultiMap}",
            request.MissionCode,
            queueItems.Count,
            analysis.IsMultiMap
        );

        return queueItems;
    }

    /// <summary>
    /// Enqueues a workflow-based mission (templateCode only, no explicit mission data).
    /// Looks up WorkflowDiagram to get MapCode.
    /// </summary>
    private async Task<List<MissionQueueItem>> EnqueueWorkflowMissionAsync(
        SubmitMissionRequest request,
        MissionTriggerSource triggerSource,
        DateTime now,
        CancellationToken cancellationToken)
    {
        // Look up workflow diagram by template code (WorkflowCode)
        var workflow = await _dbContext.WorkflowDiagrams
            .Where(w => w.WorkflowCode == request.TemplateCode)
            .FirstOrDefaultAsync(cancellationToken);

        if (workflow == null)
        {
            throw new InvalidOperationException(
                $"Workflow not found for TemplateCode: {request.TemplateCode}");
        }

        // Validate that queue is enabled for this MapCode
        var queueConfig = await _dbContext.MapCodeQueueConfigurations
            .FirstOrDefaultAsync(c => c.MapCode == workflow.MapCode && c.EnableQueue, cancellationToken);

        if (queueConfig == null)
        {
            throw new InvalidOperationException(
                $"Queue not enabled for MapCode: {workflow.MapCode}. Please configure MapCodeQueueConfiguration for this MapCode.");
        }

        _logger.LogDebug(
            "Queue validation passed for MapCode {MapCode}: MaxConcurrentRobots={MaxRobots}",
            workflow.MapCode,
            queueConfig.MaxConcurrentRobotsOnMap
        );

        // Fetch first node coordinates from WorkflowNodeCodes + QrCodes
        string? startNodeLabel = null;
        double? startX = null;
        double? startY = null;

        if (workflow.ExternalWorkflowId.HasValue)
        {
            var firstNode = await _dbContext.WorkflowNodeCodes
                .Where(wnc => wnc.ExternalWorkflowId == workflow.ExternalWorkflowId.Value)
                .OrderBy(wnc => wnc.Id)  // First inserted = first in sequence
                .Select(wnc => new { wnc.NodeCode })
                .FirstOrDefaultAsync(cancellationToken);

            if (firstNode != null)
            {
                var qrCode = await _dbContext.QrCodes
                    .Where(q => q.NodeUuid == firstNode.NodeCode)
                    .FirstOrDefaultAsync(cancellationToken);

                if (qrCode != null)
                {
                    startNodeLabel = qrCode.NodeLabel;
                    startX = qrCode.XCoordinate;
                    startY = qrCode.YCoordinate;

                    if (startX.HasValue && startY.HasValue)
                    {
                        _logger.LogInformation(
                            "Found first node NodeUuid={NodeUuid}, NodeLabel={NodeLabel} with coordinates ({X}, {Y}) for workflow {WorkflowCode}",
                            firstNode.NodeCode,
                            startNodeLabel,
                            startX.Value,
                            startY.Value,
                            workflow.WorkflowCode
                        );
                    }
                    else
                    {
                        _logger.LogWarning(
                            "QrCode with NodeUuid={NodeUuid} found but missing coordinates",
                            firstNode.NodeCode
                        );
                    }
                }
                else
                {
                    _logger.LogWarning(
                        "QrCode with NodeUuid={NodeUuid} not found for workflow {WorkflowCode}",
                        firstNode.NodeCode,
                        workflow.WorkflowCode
                    );
                }
            }
            else
            {
                _logger.LogWarning(
                    "No WorkflowNodeCodes found for workflow {WorkflowCode} (ExternalWorkflowId: {ExternalWorkflowId})",
                    workflow.WorkflowCode,
                    workflow.ExternalWorkflowId.Value
                );
            }
        }
        else
        {
            _logger.LogWarning(
                "Workflow {WorkflowCode} has no ExternalWorkflowId, cannot fetch node coordinates",
                workflow.WorkflowCode
            );
        }

        // For workflow-based missions, we don't have explicit mission data
        // The AMR system knows the steps based on templateCode
        // Create a single queue item using the workflow's MapCode
        var queueItem = new MissionQueueItem
        {
            QueueItemCode = request.MissionCode,
            MissionCode = request.MissionCode,
            MissionType = request.MissionType,
            RobotType = request.RobotType,
            Priority = request.Priority,

            // MapCode from WorkflowDiagram
            PrimaryMapCode = workflow.MapCode,
            SecondaryMapCode = null,

            // Workflow-based: no explicit mission steps, use templateCode instead
            MissionStepsJson = "[]", // Empty array for workflow-based missions
            TemplateCode = request.TemplateCode,

            // Robot constraints
            RobotModels = request.RobotModels?.Any() == true
                ? string.Join(",", request.RobotModels)
                : null,
            RobotIds = request.RobotIds?.Any() == true
                ? string.Join(",", request.RobotIds)
                : null,

            // Additional parameters
            ContainerModelCode = request.ContainerModelCode,
            ContainerCode = request.ContainerCode,
            IdleNode = request.IdleNode,
            OrgId = request.OrgId,
            ViewBoardType = request.ViewBoardType,
            LockRobotAfterFinish = request.LockRobotAfterFinish,
            UnlockRobotId = request.UnlockRobotId,
            UnlockMissionCode = request.UnlockMissionCode,

            // First node coordinates from WorkflowNodeCodes + QrCodes lookup
            StartNodeLabel = startNodeLabel,
            StartXCoordinate = startX,
            StartYCoordinate = startY,

            // Single queue item, no linking
            ParentQueueItemId = null,
            NextQueueItemId = null,
            IsOpportunisticJob = false,

            // Status and tracking
            Status = MissionQueueStatus.Pending,
            TriggerSource = triggerSource,
            EnqueuedUtc = now,
            CreatedUtc = now,
            UpdatedUtc = now
        };

        await _dbContext.MissionQueueItems.AddAsync(queueItem, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Enqueued workflow-based mission {MissionCode}: TemplateCode={TemplateCode}, MapCode={MapCode}",
            request.MissionCode,
            request.TemplateCode,
            workflow.MapCode
        );

        return new List<MissionQueueItem> { queueItem };
    }
}
