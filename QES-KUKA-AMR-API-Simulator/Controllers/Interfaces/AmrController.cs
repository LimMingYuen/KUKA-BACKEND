using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using QES_KUKA_AMR_API_Simulator.Models.Missions;
using QES_KUKA_AMR_API_Simulator.Models.Jobs;

namespace QES_KUKA_AMR_API_Simulator.Controllers.Interfaces;

[ApiController]
[Route("api/amr")]
public class AmrController : ControllerBase
{
    private static readonly object RequestLock = new();
    private static readonly HashSet<string> UsedRequestIds = new(StringComparer.OrdinalIgnoreCase);
    private static readonly HashSet<string> UsedMissionCodes = new(StringComparer.OrdinalIgnoreCase);
    private static readonly Dictionary<string, SimulatedJob> ActiveJobs = new(StringComparer.OrdinalIgnoreCase);

    private readonly ILogger<AmrController> _logger;
    private readonly IConfiguration _configuration;

    public AmrController(ILogger<AmrController> logger, IConfiguration configuration)
    {
        _logger = logger;
        _configuration = configuration;
    }

    [HttpPost("submitMission")]
    public ActionResult<SubmitMissionResponse> SubmitMission([FromBody] SubmitMissionRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.OrgId) ||
            string.IsNullOrWhiteSpace(request.RequestId) ||
            string.IsNullOrWhiteSpace(request.MissionCode))
        {
            _logger.LogWarning("Submit mission request missing required fields: {@Request}", request);
            return BadRequest(new SubmitMissionResponse
            {
                Code = "MISSION_VALIDATION_FAILED",
                Message = "Required fields orgId, requestId, and missionCode must be provided.",
                Success = false
            });
        }

        // Robot validation removed - RobotType, RobotModels, and RobotIds are now optional

        // Log mission type (template-based vs custom with missionData)
        if (request.MissionData != null && request.MissionData.Any())
        {
            _logger.LogInformation("Received CUSTOM mission with {StepCount} inline steps (MissionCode: {MissionCode})",
                request.MissionData.Count, request.MissionCode);
            _logger.LogInformation("Mission steps: {@MissionData}", request.MissionData);
        }
        else if (!string.IsNullOrWhiteSpace(request.TemplateCode))
        {
            _logger.LogInformation("Received TEMPLATE-BASED mission (MissionCode: {MissionCode}, TemplateCode: {TemplateCode})",
                request.MissionCode, request.TemplateCode);
        }
        else
        {
            _logger.LogWarning("Mission {MissionCode} has neither templateCode nor missionData - this may indicate incomplete request",
                request.MissionCode);
        }

        lock (RequestLock)
        {
            if (UsedRequestIds.Contains(request.RequestId))
            {
                _logger.LogWarning("Submit mission requestId already used: {RequestId}", request.RequestId);
                return Conflict(new SubmitMissionResponse
                {
                    Code = "100408",
                    Message = $"RequestId:[{request.RequestId}] is already used",
                    Success = false
                });
            }

            if (UsedMissionCodes.Contains(request.MissionCode))
            {
                _logger.LogWarning("Submit mission missionCode already used: {MissionCode}", request.MissionCode);
                return Conflict(new SubmitMissionResponse
                {
                    Code = "100408",
                    Message = $"MissionCode:[{request.MissionCode}] is already used",
                    Success = false
                });
            }

            UsedRequestIds.Add(request.RequestId);
            UsedMissionCodes.Add(request.MissionCode);

            // Parse mission steps if missionData is provided
            List<MissionDataItem>? missionSteps = null;
            string beginNode = "Sim1-1-2";
            string finalNode = "Sim1-1-20";

            if (request.MissionData != null && request.MissionData.Any())
            {
                missionSteps = request.MissionData.ToList();
                _logger.LogInformation("Parsed {StepCount} mission steps for mission {MissionCode}", missionSteps.Count, request.MissionCode);

                // Log manual waypoints
                var manualSteps = missionSteps.Where(s => string.Equals(s.PassStrategy, "MANUAL", StringComparison.OrdinalIgnoreCase)).ToList();
                if (manualSteps.Any())
                {
                    _logger.LogInformation("Found {ManualCount} MANUAL waypoints: {ManualPositions}",
                        manualSteps.Count,
                        string.Join(", ", manualSteps.Select(s => s.Position)));
                }

                // Use actual start and end positions from mission data
                if (missionSteps.Any())
                {
                    beginNode = missionSteps.First().Position;
                    finalNode = missionSteps.Last().Position;
                }
            }

            // Create a simulated job entry for this mission
            var simulatedJob = new SimulatedJob
            {
                Request = request,
                CreatedAt = DateTime.UtcNow,
                IsCancelled = false,
                WorkflowCode = string.IsNullOrWhiteSpace(request.TemplateCode) ? "W000000001" : request.TemplateCode,
                ContainerCode = string.IsNullOrWhiteSpace(request.ContainerCode) ? "1-2" : request.ContainerCode,
                RobotId = request.RobotIds?.FirstOrDefault() ?? "1003",
                WorkflowPriority = request.Priority == 0 ? 1 : request.Priority,
                MapCode = "Sim1",
                TargetCellCode = finalNode,
                BeginCellCode = beginNode,
                FinalNodeCode = finalNode,
                Source = "UNIVERSAL",
                MissionSteps = missionSteps,
                CurrentStepIndex = 0,
                IsWaitingForManualResume = false
            };
            ActiveJobs[request.MissionCode] = simulatedJob;
        }

        _logger.LogInformation("Mission submitted successfully: {MissionCode} (RequestId: {RequestId})", request.MissionCode, request.RequestId);

        return Ok(new SubmitMissionResponse
        {
            Data = null,
            Code = "0",
            Message = null,
            Success = true
        });
    }

    [HttpPost("missionCancel")]
    public ActionResult<MissionCancelResponse> MissionCancel([FromBody] MissionCancelRequest request)
    {
        // Validate required fields
        if (string.IsNullOrWhiteSpace(request.RequestId) ||
            string.IsNullOrWhiteSpace(request.MissionCode))
        {
            _logger.LogWarning("Mission cancel request missing required fields: {@Request}", request);
            return BadRequest(new MissionCancelResponse
            {
                Code = "CANCEL_VALIDATION_FAILED",
                Message = "Required fields requestId and missionCode must be provided.",
                Success = false
            });
        }

        // Validate cancelMode - only support FORCE, NORMAL, REDIRECT_START
        var validCancelModes = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "FORCE",
            "NORMAL",
            "REDIRECT_START"
        };

        if (!string.IsNullOrWhiteSpace(request.CancelMode) &&
            !validCancelModes.Contains(request.CancelMode))
        {
            _logger.LogWarning("Mission cancel request with invalid cancelMode: {CancelMode}", request.CancelMode);
            return BadRequest(new MissionCancelResponse
            {
                Code = "INVALID_CANCEL_MODE",
                Message = "cancelMode must be one of: FORCE, NORMAL, REDIRECT_START",
                Success = false
            });
        }

        // Log the cancel request
        _logger.LogInformation("Mission cancel request received: {MissionCode} (RequestId: {RequestId}, CancelMode: {CancelMode}, Reason: {Reason})",
            request.MissionCode, request.RequestId, request.CancelMode, request.Reason);

        // Update job status to cancelled if it exists
        lock (RequestLock)
        {
            if (ActiveJobs.TryGetValue(request.MissionCode, out var job))
            {
                job.IsCancelled = true;
                job.CancelledAt ??= DateTime.UtcNow;
                _logger.LogInformation("Job marked as cancelled: {MissionCode}", request.MissionCode);
            }
        }

        return Ok(new MissionCancelResponse
        {
            Data = null,
            Code = "0",
            Message = null,
            Success = true
        });
    }

    [HttpPost("jobQuery")]
    public ActionResult<JobQueryResponse> JobQuery([FromBody] JobQueryRequest request)
    {
        _logger.LogInformation("Job query request received with filters: {@Request}", request);

        request.Limit ??= 10;
        var limit = request.Limit.Value;
        if (limit <= 0)
        {
            limit = 10;
        }

        List<JobDto> matchingJobs;

        lock (RequestLock)
        {
            matchingJobs = ActiveJobs.Values
                .Select(job => ConvertToJobDto(job))
                .Where(job => MatchesFilters(job, request))
                .OrderByDescending(job => job.CreateTime)
                .Take(limit)
                .ToList();
        }

        _logger.LogInformation("Job query returned {Count} matching jobs", matchingJobs.Count);

        return Ok(new JobQueryResponse
        {
            Data = matchingJobs
        });
    }

    [HttpPost("operationFeedback")]
    public ActionResult<OperationFeedbackResponse> OperationFeedback([FromBody] OperationFeedbackRequest request)
    {
        _logger.LogInformation("=== AmrController.OperationFeedback (Simulator) ===");
        _logger.LogInformation("Received operation feedback - RequestId={RequestId}, MissionCode={MissionCode}, Position={Position}",
            request.RequestId, request.MissionCode, request.Position);

        // Validate required fields
        if (string.IsNullOrWhiteSpace(request.RequestId) ||
            string.IsNullOrWhiteSpace(request.MissionCode) ||
            string.IsNullOrWhiteSpace(request.Position))
        {
            _logger.LogWarning("Operation feedback request missing required fields");
            return BadRequest(new OperationFeedbackResponse
            {
                Code = "VALIDATION_FAILED",
                Message = "Required fields requestId, missionCode, and position must be provided.",
                Success = false
            });
        }

        // Find the job and mark manual position as completed
        lock (RequestLock)
        {
            if (ActiveJobs.TryGetValue(request.MissionCode, out var job))
            {
                var matchingStep = job.MissionSteps?
                    .FirstOrDefault(step =>
                        !string.IsNullOrWhiteSpace(step.Position) &&
                        string.Equals(step.Position, request.Position, StringComparison.OrdinalIgnoreCase));

                if (matchingStep != null)
                {
                    var requiresManual = string.Equals(matchingStep.PassStrategy, "MANUAL", StringComparison.OrdinalIgnoreCase);
                    var alreadyCompleted = job.CompletedManualPositions.Contains(matchingStep.Position);
                    _logger.LogDebug(
                        "OperationFeedback comparison - MissionCode={MissionCode}, StepSequence={Sequence}, StepPosition={StepPosition}, PassStrategy={PassStrategy}, RequiresManual={RequiresManual}, AlreadyCompleted={AlreadyCompleted}, TotalSteps={TotalSteps}",
                        request.MissionCode,
                        matchingStep.Sequence,
                        matchingStep.Position,
                        matchingStep.PassStrategy,
                        requiresManual,
                        alreadyCompleted,
                        job.MissionSteps?.Count ?? 0);
                }
                else
                {
                    _logger.LogDebug(
                        "OperationFeedback comparison - MissionCode={MissionCode}, Position={Position} not found in mission data. Available positions: {Positions}",
                        request.MissionCode,
                        request.Position,
                        job.MissionSteps != null && job.MissionSteps.Count > 0
                            ? string.Join(", ", job.MissionSteps.Select(step => $"{step.Sequence}:{step.Position}({step.PassStrategy})"))
                            : "none");
                }

                // Mark this manual position as completed
                job.CompletedManualPositions.Add(request.Position);

                // Resume the robot - clear the waiting flag
                if (job.IsWaitingForManualResume)
                {
                    job.IsWaitingForManualResume = false;
                    job.CurrentStepStartTime = DateTime.UtcNow; // Reset timer for next step
                    _logger.LogInformation("✓ Robot {RobotId} resumed from manual waypoint {Position}",
                        job.RobotId, request.Position);
                }

                _logger.LogInformation("✓ Operation feedback processed - mission {MissionCode}, position {Position} marked as completed",
                    request.MissionCode, request.Position);
            }
            else
            {
                _logger.LogWarning("Mission {MissionCode} not found in active jobs", request.MissionCode);
            }
        }

        return Ok(new OperationFeedbackResponse
        {
            Data = null,
            Code = "0",
            Message = "Operation feedback received successfully",
            Success = true
        });
    }

    [HttpPost("robotQuery")]
    public ActionResult<RobotQueryResponse> RobotQuery([FromBody] RobotQueryRequest request)
    {
        _logger.LogInformation("=== AmrController.RobotQuery (Simulator) ===");
        _logger.LogInformation("Robot query received - RobotId={RobotId}, RobotType={RobotType}, MapCode={MapCode}, FloorNumber={FloorNumber}",
            request.RobotId, request.RobotType, request.MapCode, request.FloorNumber);

        // Validate required fields
        if (string.IsNullOrWhiteSpace(request.RobotId))
        {
            _logger.LogWarning("Robot query request missing robotId");
            return BadRequest(new RobotQueryResponse
            {
                Code = "VALIDATION_FAILED",
                Message = "Required field robotId must be provided.",
                Success = false
            });
        }

        // Find active job for this robot
        SimulatedJob? activeJob = null;
        lock (RequestLock)
        {
            activeJob = ActiveJobs.Values.FirstOrDefault(j => j.RobotId == request.RobotId && !j.IsCompleted && !j.IsCancelled);
        }

        // Simulate robot position data
        var robotData = new RobotDataDto
        {
            RobotId = request.RobotId,
            NodeCode = activeJob != null ? GetSimulatedNodeCode(activeJob) : "Sim1-1-2", // Idle position
            MissionCode = activeJob?.Request.MissionCode,
            Status = activeJob != null ? 20 : 0, // 20 = Executing, 0 = Idle
            BatteryLevel = 85,
            RobotType = request.RobotType ?? "LIFT",
            MapCode = request.MapCode ?? "M001",
            FloorNumber = request.FloorNumber ?? "A001"
        };

        _logger.LogInformation("✓ Robot {RobotId} position: NodeCode={NodeCode}, MissionCode={MissionCode}, Status={Status}",
            robotData.RobotId, robotData.NodeCode, robotData.MissionCode, robotData.Status);

        if (activeJob != null)
        {
            if (activeJob.MissionSteps != null && activeJob.MissionSteps.Count > 0)
            {
                var currentIndex = activeJob.CurrentStepIndex;
                if (currentIndex >= activeJob.MissionSteps.Count)
                {
                    currentIndex = activeJob.MissionSteps.Count - 1;
                }

                var currentStep = activeJob.MissionSteps[currentIndex];
                var matchesStep = string.Equals(robotData.NodeCode, currentStep.Position, StringComparison.OrdinalIgnoreCase);
                var isManual = string.Equals(currentStep.PassStrategy, "MANUAL", StringComparison.OrdinalIgnoreCase);
                var manualCompleted = activeJob.CompletedManualPositions.Contains(currentStep.Position);

                _logger.LogDebug(
                    "RobotQuery comparison - MissionCode={MissionCode}, NodeCode={NodeCode}, CurrentStepIndex={StepIndex}, StepSequence={Sequence}, StepPosition={StepPosition}, PassStrategy={PassStrategy}, MatchesStep={MatchesStep}, ManualCompleted={ManualCompleted}, WaitingForManual={WaitingForManual}",
                    activeJob.Request.MissionCode,
                    robotData.NodeCode,
                    currentIndex,
                    currentStep.Sequence,
                    currentStep.Position,
                    currentStep.PassStrategy,
                    matchesStep,
                    manualCompleted,
                    activeJob.IsWaitingForManualResume);
            }
            else
            {
                _logger.LogDebug(
                    "RobotQuery comparison - MissionCode={MissionCode} has no missionData steps; using legacy simulated path. NodeCode={NodeCode}",
                    activeJob.Request.MissionCode,
                    robotData.NodeCode);
            }
        }
        else
        {
            _logger.LogDebug(
                "RobotQuery comparison - No active mission for RobotId={RobotId}. NodeCode={NodeCode}",
                robotData.RobotId,
                robotData.NodeCode);
        }

        return Ok(new RobotQueryResponse
        {
            Data = new List<RobotDataDto> { robotData },
            Code = "0",
            Message = null,
            Success = true
        });
    }

    /// <summary>
    /// Looks up an area by ZoneCode in the static MapZones list
    /// </summary>
    private QES_KUKA_AMR_API_Simulator.Models.MapZone.MapZoneDto? LookupArea(string position)
    {
        // Check if position matches any area ZoneCode
        return QES_KUKA_AMR_API_Simulator.Controllers.Data.MapZoneController.MapZones
            .FirstOrDefault(mz => string.Equals(mz.ZoneCode, position, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Parses the AreaNodeList from a MapZone's Configs and returns list of node codes
    /// </summary>
    private List<string> ParseAreaNodes(QES_KUKA_AMR_API_Simulator.Models.MapZone.MapZoneDto area)
    {
        var nodes = new List<string>();

        if (area.Configs == null || string.IsNullOrWhiteSpace(area.Configs.AreaNodeList))
        {
            return nodes;
        }

        try
        {
            var areaNodes = System.Text.Json.JsonSerializer.Deserialize<List<QES_KUKA_AMR_API_Simulator.Models.MapZone.AreaNodeDto>>(
                area.Configs.AreaNodeList,
                new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            if (areaNodes != null && areaNodes.Count > 0)
            {
                // Return nodes ordered by sort
                nodes = areaNodes.OrderBy(n => n.Sort).Select(n => n.CellCode).ToList();
                _logger.LogDebug("Resolved area {AreaCode} to {Count} nodes: [{Nodes}]",
                    area.ZoneCode, nodes.Count, string.Join(", ", nodes));
            }
        }
        catch (System.Text.Json.JsonException ex)
        {
            _logger.LogWarning(ex, "Failed to parse AreaNodeList for area {AreaCode}", area.ZoneCode);
        }

        return nodes;
    }

    /// <summary>
    /// Resolves a position to actual node code. If position is an area, returns the first node in the area.
    /// </summary>
    private string ResolvePositionToNode(SimulatedJob job, string position)
    {
        // Check cache first
        if (job.AreaPositionToResolvedNode.TryGetValue(position, out var cachedNode))
        {
            return cachedNode;
        }

        // Try to look up as area
        var area = LookupArea(position);
        if (area != null)
        {
            var nodes = ParseAreaNodes(area);
            if (nodes.Count > 0)
            {
                // Use first node (lowest sort order)
                var resolvedNode = nodes[0];
                job.AreaPositionToResolvedNode[position] = resolvedNode;
                _logger.LogInformation("📍 Area {AreaCode} resolved to node {NodeCode} (first of {Count} nodes)",
                    position, resolvedNode, nodes.Count);
                return resolvedNode;
            }
        }

        // Not an area or no nodes found - treat as direct node code
        return position;
    }

    private string GetSimulatedNodeCode(SimulatedJob job)
    {
        // If no mission steps, use legacy hardcoded simulation
        if (job.MissionSteps == null || !job.MissionSteps.Any())
        {
            return GetLegacySimulatedNodeCode(job);
        }

        var now = DateTime.UtcNow;

        // Initialize on first query
        if (job.FirstQueryTime == null)
        {
            job.FirstQueryTime = now;
            job.CurrentStepIndex = 0;
            job.CurrentStepStartTime = now;
            // Resolve area to node if needed
            var firstPosition = job.MissionSteps[0].Position;
            return ResolvePositionToNode(job, firstPosition);
        }

        // If waiting for manual resume, stay at current position
        if (job.IsWaitingForManualResume)
        {
            var currentStep = job.MissionSteps[job.CurrentStepIndex];
            _logger.LogDebug("Robot {RobotId} waiting for manual resume at position {Position}",
                job.RobotId, currentStep.Position);
            // Resolve area to node if needed
            return ResolvePositionToNode(job, currentStep.Position);
        }

        // Get current step
        if (job.CurrentStepIndex >= job.MissionSteps.Count)
        {
            // Mission complete - at final position
            return ResolvePositionToNode(job, job.FinalNodeCode);
        }

        var step = job.MissionSteps[job.CurrentStepIndex];
        var stepStartTime = job.CurrentStepStartTime ?? now;
        var timeAtCurrentStep = (now - stepStartTime).TotalSeconds;

        // Time to travel to each node (configurable, default 4 seconds)
        var travelTimePerNode = _configuration.GetValue<int>("JobSimulation:TravelTimePerNodeSeconds", 4);

        // Check if robot has reached current step
        if (timeAtCurrentStep < travelTimePerNode)
        {
            // Still traveling to this node - resolve area to node if needed
            return ResolvePositionToNode(job, step.Position);
        }

        // Robot has arrived at current step
        // Check if this is a manual waypoint
        if (string.Equals(step.PassStrategy, "MANUAL", StringComparison.OrdinalIgnoreCase))
        {
            // Check if this manual waypoint has been resumed
            if (!job.CompletedManualPositions.Contains(step.Position))
            {
                // Stop here and wait for operation feedback
                job.IsWaitingForManualResume = true;
                _logger.LogInformation("Robot {RobotId} reached MANUAL waypoint at {Position}, waiting for resume",
                    job.RobotId, step.Position);
                // Resolve area to node if needed
                return ResolvePositionToNode(job, step.Position);
            }
        }

        // Move to next step
        job.CurrentStepIndex++;
        job.CurrentStepStartTime = now;

        if (job.CurrentStepIndex >= job.MissionSteps.Count)
        {
            // Reached final position
            _logger.LogInformation("Robot {RobotId} reached final position {Position}",
                job.RobotId, job.FinalNodeCode);
            return ResolvePositionToNode(job, job.FinalNodeCode);
        }

        // Return new current step position - resolve area to node if needed
        var nextPosition = job.MissionSteps[job.CurrentStepIndex].Position;
        return ResolvePositionToNode(job, nextPosition);
    }

    private string GetLegacySimulatedNodeCode(SimulatedJob job)
    {
        // Legacy simulation for missions without missionData
        if (job.FirstQueryTime == null)
        {
            return job.BeginCellCode;
        }

        var elapsed = (DateTime.UtcNow - job.FirstQueryTime.Value).TotalSeconds;

        if (elapsed < 5)
            return job.BeginCellCode;
        else if (elapsed < 10)
            return "Sim1-1-5";
        else if (elapsed < 15)
            return "Sim1-1-10";
        else if (elapsed < 20)
            return "Sim1-1-15";
        else
            return job.FinalNodeCode;
    }

    private JobDto ConvertToJobDto(SimulatedJob simulatedJob)
    {
        var now = DateTime.UtcNow;
        var currentStatus = DetermineJobStatus(simulatedJob, now);
        var request = simulatedJob.Request;

        int? spendTime = null;
        string? completeTime = null;
        if (simulatedJob.IsCompleted && simulatedJob.CompletedAt.HasValue)
        {
            spendTime = Math.Max(1, (int)(simulatedJob.CompletedAt.Value - simulatedJob.CreatedAt).TotalSeconds);
            completeTime = simulatedJob.CompletedAt.Value.ToString("yyyy-MM-dd HH:mm:ss");
        }

        return new JobDto
        {
            JobCode = request.MissionCode,
            WorkflowId = simulatedJob.WorkflowId,
            ContainerCode = simulatedJob.ContainerCode,
            RobotId = simulatedJob.RobotId,
            Status = currentStatus,
            WorkflowName = simulatedJob.WorkflowName,
            WorkflowCode = simulatedJob.WorkflowCode,
            WorkflowPriority = simulatedJob.WorkflowPriority,
            MapCode = simulatedJob.MapCode,
            TargetCellCode = simulatedJob.TargetCellCode,
            BeginCellCode = simulatedJob.BeginCellCode,
            TargetCellCodeForeign = null,
            BeginCellCodeForeign = null,
            FinalNodeCode = simulatedJob.FinalNodeCode,
            WarnFlag = 0,
            WarnCode = null,
            CompleteTime = completeTime,
            SpendTime = spendTime,
            CreateUsername = simulatedJob.CreateUsername,
            CreateTime = simulatedJob.CreatedAt.ToString("yyyy-MM-dd HH:mm:ss"),
            Source = simulatedJob.Source,
            MaterialsInfo = "-"
        };
    }

    private int DetermineJobStatus(SimulatedJob job, DateTime now)
    {
        // Handle cancelled state
        if (job.IsCancelled)
        {
            job.CancelledAt ??= now;
            var cancelDuration = (now - job.CancelledAt.Value).TotalSeconds;
            // Show Cancelling (28) for 2 seconds, then Cancelled (31)
            return cancelDuration < 2 ? 28 : 31;
        }

        // Initialize job on first query - starts in Created state
        if (!job.FirstQueryTime.HasValue)
        {
            job.FirstQueryTime = now;
            return 10; // Created
        }

        // Calculate elapsed time since first query
        var elapsed = (now - job.FirstQueryTime.Value).TotalSeconds;

        // Read timing configuration from appsettings.json
        var createdToExecuting = _configuration.GetValue<int>("JobSimulation:ProgressionTimingSeconds:CreatedToExecuting", 3);
        var executingToWaiting = _configuration.GetValue<int>("JobSimulation:ProgressionTimingSeconds:ExecutingToWaiting", 5);
        var waitingToExecuting = _configuration.GetValue<int>("JobSimulation:ProgressionTimingSeconds:WaitingToExecuting", 3);
        var executingToComplete = _configuration.GetValue<int>("JobSimulation:ProgressionTimingSeconds:ExecutingToComplete", 10);

        // Status progression timeline:
        // Created (10) -> Executing (20) -> Waiting (25) -> Executing (20) -> Complete (30)

        if (elapsed < createdToExecuting)
        {
            return 10; // Created
        }

        var stage1End = createdToExecuting + executingToWaiting;
        if (elapsed < stage1End)
        {
            return 20; // Executing (first phase)
        }

        var stage2End = stage1End + waitingToExecuting;
        if (elapsed < stage2End)
        {
            return 25; // Waiting
        }

        var stage3End = stage2End + executingToComplete;
        if (elapsed < stage3End)
        {
            return 20; // Executing (resumed, second phase)
        }

        // Mark as completed
        if (!job.IsCompleted)
        {
            job.IsCompleted = true;
            job.CompletedAt = now;
        }

        return 30; // Complete
    }

    private static string MapSourceValue(int sourceValue) => sourceValue switch
    {
        1 => "PDA",
        2 => "INTERFACE",
        3 => "PDA",
        4 => "DEVICE",
        5 => "MLS",
        6 => "SELF",
        7 => "EVENT",
        _ => "INTERFACE"
    };

    private bool MatchesFilters(JobDto job, JobQueryRequest request)
    {
        if (request.WorkflowId.HasValue && job.WorkflowId != request.WorkflowId)
            return false;

        if (!string.IsNullOrEmpty(request.ContainerCode) &&
            !string.Equals(job.ContainerCode, request.ContainerCode, StringComparison.OrdinalIgnoreCase))
            return false;

        if (!string.IsNullOrEmpty(request.JobCode) &&
            !string.Equals(job.JobCode, request.JobCode, StringComparison.OrdinalIgnoreCase))
            return false;

        if (!string.IsNullOrEmpty(request.Status))
        {
            if (int.TryParse(request.Status, out var requestedStatus) && job.Status != requestedStatus)
                return false;
        }

        if (!string.IsNullOrEmpty(request.RobotId) &&
            !string.Equals(job.RobotId, request.RobotId, StringComparison.OrdinalIgnoreCase))
            return false;

        if (!string.IsNullOrEmpty(request.TargetCellCode) &&
            !string.Equals(job.TargetCellCode, request.TargetCellCode, StringComparison.OrdinalIgnoreCase))
            return false;

        if (!string.IsNullOrEmpty(request.WorkflowName) &&
            !string.Equals(job.WorkflowName, request.WorkflowName, StringComparison.OrdinalIgnoreCase))
            return false;

        if (!string.IsNullOrEmpty(request.WorkflowCode) &&
            !string.Equals(job.WorkflowCode, request.WorkflowCode, StringComparison.OrdinalIgnoreCase))
            return false;

        if (request.Maps != null && request.Maps.Any())
        {
            if (string.IsNullOrEmpty(job.MapCode) || !request.Maps.Contains(job.MapCode, StringComparer.OrdinalIgnoreCase))
                return false;
        }

        if (!string.IsNullOrEmpty(request.CreateUsername) &&
            !string.Equals(job.CreateUsername, request.CreateUsername, StringComparison.OrdinalIgnoreCase))
            return false;

        if (request.SourceValue.HasValue)
        {
            var sourceString = MapSourceValue(request.SourceValue.Value);
            if (!string.Equals(job.Source, sourceString, StringComparison.OrdinalIgnoreCase))
                return false;
        }

        return true;
    }

    private class SimulatedJob
    {
        public SubmitMissionRequest Request { get; set; } = null!;
        public DateTime CreatedAt { get; set; }
        public bool IsCancelled { get; set; }
        public bool IsCompleted { get; set; }
        public DateTime? FirstQueryTime { get; set; }
        public DateTime? CompletedAt { get; set; }
        public DateTime? CancelledAt { get; set; }
        public long WorkflowId { get; set; } = 204;
        public string WorkflowName { get; set; } = "area1_to_area3";
        public string WorkflowCode { get; set; } = "W000000001";
        public int WorkflowPriority { get; set; } = 1;
        public string ContainerCode { get; set; } = "1-2";
        public string RobotId { get; set; } = "1003";
        public string MapCode { get; set; } = "Sim1";
        public string TargetCellCode { get; set; } = "Sim1-1-20";
        public string BeginCellCode { get; set; } = "Sim1-1-2";
        public string FinalNodeCode { get; set; } = "Sim1-1-20";
        public string Source { get; set; } = "UNIVERSAL";
        public string? CreateUsername { get; set; }

        // Mission step tracking for manual waypoint simulation
        public List<MissionDataItem>? MissionSteps { get; set; }
        public int CurrentStepIndex { get; set; } = 0;
        public DateTime? CurrentStepStartTime { get; set; }
        public HashSet<string> CompletedManualPositions { get; set; } = new(StringComparer.OrdinalIgnoreCase);
        public bool IsWaitingForManualResume { get; set; }

        // Area resolution cache: maps area code to resolved node code
        public Dictionary<string, string> AreaPositionToResolvedNode { get; set; } = new(StringComparer.OrdinalIgnoreCase);
    }
}
