using System.Globalization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using QES_KUKA_AMR_API.Data;
using QES_KUKA_AMR_API.Data.Entities;
using QES_KUKA_AMR_API.Models.Analytics;
using QES_KUKA_AMR_API.Models.Missions;
using QES_KUKA_AMR_API.Services.Missions;

namespace QES_KUKA_AMR_API.Services.Analytics;

public interface IRobotAnalyticsService
{
    Task<RobotUtilizationMetrics> GetUtilizationAsync(
        string robotId,
        DateTime periodStartUtc,
        DateTime periodEndUtc,
        UtilizationGroupingInterval grouping,
        string? jwtToken = null,
        CancellationToken cancellationToken = default);

    Task<RobotUtilizationDiagnostics> GetUtilizationDiagnosticsAsync(
        string? robotId,
        DateTime periodStartUtc,
        DateTime periodEndUtc,
        CancellationToken cancellationToken = default);
}

public class RobotAnalyticsService : IRobotAnalyticsService
{
    private readonly ApplicationDbContext _dbContext;
    private readonly IWorkflowAnalyticsService _workflowAnalyticsService;
    private readonly IMissionListClient _missionListClient;
    private readonly ILogger<RobotAnalyticsService> _logger;

    public RobotAnalyticsService(
        ApplicationDbContext dbContext,
        IWorkflowAnalyticsService workflowAnalyticsService,
        IMissionListClient missionListClient,
        ILogger<RobotAnalyticsService> logger)
    {
        _dbContext = dbContext;
        _workflowAnalyticsService = workflowAnalyticsService;
        _missionListClient = missionListClient;
        _logger = logger;
    }

    public async Task<RobotUtilizationMetrics> GetUtilizationAsync(
        string robotId,
        DateTime periodStartUtc,
        DateTime periodEndUtc,
        UtilizationGroupingInterval grouping,
        string? jwtToken = null,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(robotId))
        {
            throw new ArgumentException("Robot ID is required", nameof(robotId));
        }

        var normalizedStart = NormalizeToUtc(periodStartUtc);
        var normalizedEnd = NormalizeToUtc(periodEndUtc);

        if (normalizedEnd <= normalizedStart)
        {
            throw new ArgumentException("The end of the period must be greater than the start.", nameof(periodEndUtc));
        }

        var bucketSize = grouping switch
        {
            UtilizationGroupingInterval.Hour => TimeSpan.FromHours(1),
            UtilizationGroupingInterval.Day => TimeSpan.FromDays(1),
            _ => throw new ArgumentOutOfRangeException(nameof(grouping), grouping, "Unsupported grouping interval.")
        };

        var metrics = new RobotUtilizationMetrics
        {
            RobotId = robotId.Trim(),
            PeriodStartUtc = normalizedStart,
            PeriodEndUtc = normalizedEnd,
            TotalAvailableMinutes = Math.Max(0, (normalizedEnd - normalizedStart).TotalMinutes)
        };

        // Query completed missions from MissionQueues (active/recent missions)
        var queueMissions = await _dbContext.MissionQueues
            .AsNoTracking()
            .Where(m =>
                m.AssignedRobotId != null &&
                m.AssignedRobotId == robotId &&
                m.CompletedDate != null)
            .Select(m => new
            {
                m.MissionCode,
                m.WorkflowName,
                m.SavedMissionId,
                m.TriggerSource,
                Start = (DateTime?)(m.ProcessedDate ?? m.SubmittedToAmrDate ?? m.CreatedDate),
                End = m.CompletedDate,
                Status = m.Status
            })
            .Where(m => m.Start != null && m.End != null && m.Start < normalizedEnd && m.End > normalizedStart)
            .ToListAsync(cancellationToken);

        // CRITICAL FIX: Also query MissionHistories (archived completed missions)
        var historyMissions = await _dbContext.MissionHistories
            .AsNoTracking()
            .Where(m =>
                m.AssignedRobotId != null &&
                m.AssignedRobotId == robotId &&
                m.CompletedDate != null)
            .Select(m => new
            {
                m.MissionCode,
                m.WorkflowName,
                m.SavedMissionId,
                m.TriggerSource,
                Start = (DateTime?)(m.ProcessedDate ?? m.SubmittedToAmrDate ?? m.CreatedDate),
                End = m.CompletedDate,
                Status = QueueStatus.Completed  // History missions are always completed
            })
            .Where(m => m.Start != null && m.End != null && m.Start < normalizedEnd && m.End > normalizedStart)
            .ToListAsync(cancellationToken);

        // Combine both sources
        var missionSnapshots = queueMissions.Concat(historyMissions).ToList();

        _logger.LogInformation("Found {QueueCount} missions in MissionQueues and {HistoryCount} in MissionHistories for robot {RobotId}",
            queueMissions.Count, historyMissions.Count, robotId);

        var manualPauses = await _dbContext.RobotManualPauses
            .AsNoTracking()
            .Where(p =>
                p.RobotId == robotId &&
                p.PauseStartUtc < normalizedEnd &&
                (p.PauseEndUtc ?? normalizedEnd) > normalizedStart)
            .Select(p => new
            {
                p.MissionCode,
                p.PauseStartUtc,
                p.PauseEndUtc
            })
            .ToListAsync(cancellationToken);

        var manualMinutesByBucket = new Dictionary<DateTime, double>();
        foreach (var pause in manualPauses)
        {
            var start = NormalizeToUtc(pause.PauseStartUtc);
            var end = NormalizeToUtc(pause.PauseEndUtc ?? normalizedEnd);
            foreach (var segment in SplitInterval(start, end, normalizedStart, normalizedEnd, grouping, bucketSize))
            {
                manualMinutesByBucket.TryGetValue(segment.BucketStartUtc, out var existing);
                manualMinutesByBucket[segment.BucketStartUtc] = existing + segment.Minutes;
            }
        }

        metrics.ManualPauseMinutes = manualMinutesByBucket.Values.Sum();

        var missionMinutesByBucket = new Dictionary<DateTime, double>();
        var manualOverlapsByBucket = new Dictionary<DateTime, double>();

        foreach (var mission in missionSnapshots)
        {
            if (mission.Start is null || mission.End is null)
            {
                continue;
            }

            var missionStart = NormalizeToUtc(mission.Start.Value);
            var missionEnd = NormalizeToUtc(mission.End.Value);

            if (missionEnd <= missionStart)
            {
                continue;
            }

            var boundedStart = Max(missionStart, normalizedStart);
            var boundedEnd = Min(missionEnd, normalizedEnd);
            if (boundedEnd <= boundedStart)
            {
                continue;
            }

            var missionDurationMinutes = (boundedEnd - boundedStart).TotalMinutes;
            var manualOverlapMinutes = 0.0;

            var missionPauseEntries = manualPauses
                .Where(p => string.Equals(p.MissionCode, mission.MissionCode, StringComparison.OrdinalIgnoreCase))
                .ToList();

            foreach (var pause in missionPauseEntries)
            {
                var pauseStart = Max(NormalizeToUtc(pause.PauseStartUtc), boundedStart);
                var pauseEnd = Min(NormalizeToUtc(pause.PauseEndUtc ?? normalizedEnd), boundedEnd);
                if (pauseEnd <= pauseStart)
                {
                    continue;
                }

                manualOverlapMinutes += (pauseEnd - pauseStart).TotalMinutes;

                foreach (var segment in SplitInterval(pauseStart, pauseEnd, normalizedStart, normalizedEnd, grouping, bucketSize))
                {
                    manualOverlapsByBucket.TryGetValue(segment.BucketStartUtc, out var existing);
                    manualOverlapsByBucket[segment.BucketStartUtc] = existing + segment.Minutes;
                }
            }

            var effectiveMissionMinutes = Math.Max(0, missionDurationMinutes - manualOverlapMinutes);

            foreach (var segment in SplitInterval(boundedStart, boundedEnd, normalizedStart, normalizedEnd, grouping, bucketSize))
            {
                missionMinutesByBucket.TryGetValue(segment.BucketStartUtc, out var existing);
                missionMinutesByBucket[segment.BucketStartUtc] = existing + segment.Minutes;
            }

            metrics.Missions.Add(new RobotUtilizationMission
            {
                MissionCode = mission.MissionCode,
                WorkflowName = mission.WorkflowName,
                SavedMissionId = mission.SavedMissionId,
                TriggerSource = mission.TriggerSource.ToString(),
                StartTimeUtc = boundedStart,
                CompletedTimeUtc = boundedEnd,
                DurationMinutes = Math.Round(missionDurationMinutes, 2),
                ManualPauseMinutes = Math.Round(manualOverlapMinutes, 2)
            });

            metrics.UtilizedMinutes += effectiveMissionMinutes;

            // Count mission completion within the bucket containing its completion timestamp
            var completionBucket = GetBucketStart(Min(boundedEnd, normalizedEnd - TimeSpan.FromTicks(1)), grouping);
            var breakdownEntry = metrics.Breakdown.FirstOrDefault(b => b.BucketStartUtc == completionBucket);
            if (breakdownEntry == null)
            {
                metrics.Breakdown.Add(new RobotUtilizationBreakdown
                {
                    BucketStartUtc = completionBucket,
                    UtilizedMinutes = 0,
                    ManualPauseMinutes = 0,
                    CompletedMissions = 1
                });
            }
            else
            {
                breakdownEntry.CompletedMissions += 1;
            }
        }

        // Merge bucket data
        var allBuckets = missionMinutesByBucket.Keys
            .Union(manualMinutesByBucket.Keys)
            .Union(manualOverlapsByBucket.Keys)
            .Union(metrics.Breakdown.Select(b => b.BucketStartUtc))
            .Distinct()
            .OrderBy(b => b)
            .ToList();

        var breakdownLookup = metrics.Breakdown.ToDictionary(b => b.BucketStartUtc);
        metrics.Breakdown.Clear();

        foreach (var bucket in allBuckets)
        {
            missionMinutesByBucket.TryGetValue(bucket, out var missionMinutes);
            manualMinutesByBucket.TryGetValue(bucket, out var manualMinutes);
            manualOverlapsByBucket.TryGetValue(bucket, out var manualOverlaps);

            if (!breakdownLookup.TryGetValue(bucket, out var entry))
            {
                entry = new RobotUtilizationBreakdown
                {
                    BucketStartUtc = bucket,
                    CompletedMissions = 0
                };
            }

            // Calculate available minutes for this bucket (handling edge cases for first/last buckets)
            var bucketEnd = bucket + bucketSize;
            var effectiveBucketStart = bucket < normalizedStart ? normalizedStart : bucket;
            var effectiveBucketEnd = bucketEnd > normalizedEnd ? normalizedEnd : bucketEnd;
            entry.TotalAvailableMinutes = Math.Max(0, (effectiveBucketEnd - effectiveBucketStart).TotalMinutes);

            entry.UtilizedMinutes += Math.Max(0, missionMinutes - manualOverlaps);
            entry.ManualPauseMinutes += manualMinutes;
            metrics.Breakdown.Add(entry);
        }

        metrics.Breakdown = metrics.Breakdown
            .OrderBy(b => b.BucketStartUtc)
            .ToList();

        var effectiveAvailable = Math.Max(0, metrics.TotalAvailableMinutes - metrics.ManualPauseMinutes);
        if (effectiveAvailable <= 0)
        {
            metrics.UtilizationPercent = 0;
        }
        else
        {
            metrics.UtilizationPercent = Math.Round(Math.Min(metrics.UtilizedMinutes / effectiveAvailable, 1.0) * 100, 2);
        }

        metrics.UtilizedMinutes = Math.Round(metrics.UtilizedMinutes, 2);
        metrics.ManualPauseMinutes = Math.Round(metrics.ManualPauseMinutes, 2);

        metrics.Missions = metrics.Missions
            .OrderByDescending(m => m.CompletedTimeUtc)
            .ToList();

        // Fetch workflow execution data
        var workflowExecutions = await _workflowAnalyticsService
            .GetWorkflowExecutionsAsync(
                robotId,
                normalizedStart,
                normalizedEnd,
                cancellationToken);

        metrics.WorkflowExecutions = workflowExecutions;

        // Fetch and process charging sessions
        var chargingSessions = await GetChargingSessionsAsync(
            robotId,
            normalizedStart,
            normalizedEnd,
            jwtToken,
            cancellationToken);

        metrics.ChargingSessions = chargingSessions;

        // Calculate charging minutes by bucket
        var chargingMinutesByBucket = new Dictionary<DateTime, double>();
        foreach (var session in chargingSessions)
        {
            var boundedStart = Max(session.BeginTimeUtc, normalizedStart);
            var boundedEnd = Min(session.EndTimeUtc, normalizedEnd);

            if (boundedEnd <= boundedStart)
            {
                continue;
            }

            foreach (var segment in SplitInterval(
                boundedStart, boundedEnd, normalizedStart, normalizedEnd, grouping, bucketSize))
            {
                chargingMinutesByBucket.TryGetValue(segment.BucketStartUtc, out var existing);
                chargingMinutesByBucket[segment.BucketStartUtc] = existing + segment.Minutes;
            }
        }

        metrics.TotalChargingMinutes = chargingMinutesByBucket.Values.Sum();
        metrics.TotalWorkingMinutes = metrics.UtilizedMinutes; // Mission execution time
        metrics.TotalIdleMinutes = Math.Max(0,
            metrics.TotalAvailableMinutes - metrics.ManualPauseMinutes - metrics.TotalWorkingMinutes - metrics.TotalChargingMinutes);

        // Update breakdown with charging, working, and idle minutes
        foreach (var bucket in metrics.Breakdown)
        {
            chargingMinutesByBucket.TryGetValue(bucket.BucketStartUtc, out var chargingMinutes);

            bucket.ChargingMinutes = Math.Round(chargingMinutes, 2);
            bucket.WorkingMinutes = bucket.UtilizedMinutes; // Mission execution time
            bucket.IdleMinutes = Math.Max(0,
                bucket.TotalAvailableMinutes - bucket.ManualPauseMinutes - bucket.WorkingMinutes - bucket.ChargingMinutes);
        }

        // Recalculate utilization to include charging as productive time
        var effectiveAvailableWithCharging = Math.Max(0, metrics.TotalAvailableMinutes - metrics.ManualPauseMinutes);
        if (effectiveAvailableWithCharging <= 0)
        {
            metrics.UtilizationPercent = 0;
        }
        else
        {
            // Utilization = (Working + Charging) / Available
            var totalProductive = metrics.TotalWorkingMinutes + metrics.TotalChargingMinutes;
            metrics.UtilizationPercent = Math.Round(Math.Min(totalProductive / effectiveAvailableWithCharging, 1.0) * 100, 2);
        }

        // Round metrics
        metrics.TotalChargingMinutes = Math.Round(metrics.TotalChargingMinutes, 2);
        metrics.TotalWorkingMinutes = Math.Round(metrics.TotalWorkingMinutes, 2);
        metrics.TotalIdleMinutes = Math.Round(metrics.TotalIdleMinutes, 2);

        _logger.LogInformation(
            "Computed utilization for robot {RobotId} between {Start} and {End}. " +
            "Utilization={UtilizationPercent}%, Working={WorkingMin}min, Charging={ChargingMin}min, " +
            "Idle={IdleMin}min, Workflows={WorkflowCount}, SuccessRate={SuccessRate}%",
            metrics.RobotId,
            metrics.PeriodStartUtc,
            metrics.PeriodEndUtc,
            metrics.UtilizationPercent,
            metrics.TotalWorkingMinutes,
            metrics.TotalChargingMinutes,
            metrics.TotalIdleMinutes,
            metrics.TotalWorkflows,
            metrics.WorkflowSuccessRate);

        return metrics;
    }

    public async Task<RobotUtilizationDiagnostics> GetUtilizationDiagnosticsAsync(
        string? robotId,
        DateTime periodStartUtc,
        DateTime periodEndUtc,
        CancellationToken cancellationToken = default)
    {
        var normalizedStart = NormalizeToUtc(periodStartUtc);
        var normalizedEnd = NormalizeToUtc(periodEndUtc);

        var diagnostics = new RobotUtilizationDiagnostics
        {
            RobotId = robotId,
            QueryStartUtc = normalizedStart,
            QueryEndUtc = normalizedEnd
        };

        // Get all missions from MissionQueues
        var queueMissions = await _dbContext.MissionQueues
            .AsNoTracking()
            .Select(m => new
            {
                m.MissionCode,
                m.AssignedRobotId,
                m.ProcessedDate,
                m.SubmittedToAmrDate,
                m.CreatedDate,
                m.CompletedDate,
                m.WorkflowName,
                Status = m.Status.ToString(),
                Start = (DateTime?)(m.ProcessedDate ?? m.SubmittedToAmrDate ?? m.CreatedDate)
            })
            .ToListAsync(cancellationToken);

        // ALSO get missions from MissionHistories
        var historyMissions = await _dbContext.MissionHistories
            .AsNoTracking()
            .Select(m => new
            {
                m.MissionCode,
                m.AssignedRobotId,
                m.ProcessedDate,
                m.SubmittedToAmrDate,
                m.CreatedDate,
                m.CompletedDate,
                m.WorkflowName,
                m.Status,
                Start = (DateTime?)(m.ProcessedDate ?? m.SubmittedToAmrDate ?? m.CreatedDate)
            })
            .ToListAsync(cancellationToken);

        // Combine both sources
        var allMissions = queueMissions.Concat(historyMissions).ToList();

        diagnostics.TotalMissionsInDatabase = allMissions.Count;
        _logger.LogInformation("Diagnostic found {QueueCount} in MissionQueues + {HistoryCount} in MissionHistories = {Total} total",
            queueMissions.Count, historyMissions.Count, allMissions.Count);
        diagnostics.MissionsWithAssignedRobotId = allMissions.Count(m => m.AssignedRobotId != null);
        diagnostics.MissionsWithCompletedDate = allMissions.Count(m => m.CompletedDate != null);

        // Get available robot IDs
        diagnostics.AvailableRobotIds = allMissions
            .Where(m => m.AssignedRobotId != null)
            .Select(m => m.AssignedRobotId!)
            .Distinct()
            .OrderBy(id => id)
            .ToList();

        // Apply filters step by step
        var missionsWithRobotId = allMissions.Where(m => m.AssignedRobotId != null).ToList();
        var missionsWithCompletedDate = missionsWithRobotId.Where(m => m.CompletedDate != null).ToList();

        var missionsMatchingRobot = string.IsNullOrWhiteSpace(robotId)
            ? missionsWithCompletedDate
            : missionsWithCompletedDate.Where(m => m.AssignedRobotId == robotId).ToList();

        diagnostics.MissionsMatchingRobotId = missionsMatchingRobot.Count;

        var missionsMatchingDateRange = missionsMatchingRobot
            .Where(m => m.Start != null && m.CompletedDate != null &&
                       m.Start < normalizedEnd && m.CompletedDate > normalizedStart)
            .ToList();

        diagnostics.MissionsMatchingDateRange = missionsMatchingDateRange.Count;
        diagnostics.FinalMissionsIncluded = missionsMatchingDateRange.Count;

        // Build sample mission list with diagnostic info
        diagnostics.SampleMissions = allMissions.Take(20).Select(m =>
        {
            var included = missionsMatchingDateRange.Any(mm => mm.MissionCode == m.MissionCode);
            var exclusionReason = string.Empty;

            if (!included)
            {
                if (m.AssignedRobotId == null)
                {
                    exclusionReason = "No assigned robot ID";
                }
                else if (m.CompletedDate == null)
                {
                    exclusionReason = "Not completed";
                }
                else if (!string.IsNullOrWhiteSpace(robotId) && m.AssignedRobotId != robotId)
                {
                    exclusionReason = $"Robot ID mismatch (expected: {robotId}, actual: {m.AssignedRobotId})";
                }
                else if (m.Start == null || m.CompletedDate == null)
                {
                    exclusionReason = "Missing start or completion date";
                }
                else if (m.Start >= normalizedEnd || m.CompletedDate <= normalizedStart)
                {
                    exclusionReason = $"Outside date range (mission: {m.Start:yyyy-MM-dd HH:mm} - {m.CompletedDate:yyyy-MM-dd HH:mm}, query: {normalizedStart:yyyy-MM-dd HH:mm} - {normalizedEnd:yyyy-MM-dd HH:mm})";
                }
                else
                {
                    exclusionReason = "Unknown reason";
                }
            }

            return new MissionDiagnosticInfo
            {
                MissionCode = m.MissionCode,
                AssignedRobotId = m.AssignedRobotId,
                ProcessedDate = m.ProcessedDate,
                SubmittedToAmrDate = m.SubmittedToAmrDate,
                CreatedDate = m.CreatedDate,
                CompletedDate = m.CompletedDate,
                WorkflowName = m.WorkflowName,
                Status = m.Status.ToString(),
                IncludedInQuery = included,
                ExclusionReason = exclusionReason
            };
        }).ToList();

        // Generate analysis text
        var analysisLines = new List<string>();

        if (diagnostics.TotalMissionsInDatabase == 0)
        {
            analysisLines.Add("No missions found in the database. The system may be newly installed or no missions have been created yet.");
        }
        else if (diagnostics.MissionsWithAssignedRobotId == 0)
        {
            analysisLines.Add($"Found {diagnostics.TotalMissionsInDatabase} missions in database, but NONE have an assigned robot ID.");
            analysisLines.Add("The JobStatusPollerBackgroundService may not be running, or the AMR system is not assigning robots to missions.");
            analysisLines.Add("Check: 1) Background service logs, 2) AMR API connectivity, 3) Mission submission process");
        }
        else if (diagnostics.MissionsWithCompletedDate == 0)
        {
            analysisLines.Add($"Found {diagnostics.MissionsWithAssignedRobotId} missions with robot IDs, but NONE are completed.");
            analysisLines.Add("All missions may still be in progress, queued, or failed without completion timestamps.");
        }
        else if (!string.IsNullOrWhiteSpace(robotId) && diagnostics.MissionsMatchingRobotId == 0)
        {
            analysisLines.Add($"Found {diagnostics.MissionsWithCompletedDate} completed missions, but NONE match robot ID '{robotId}'.");
            analysisLines.Add($"Available robot IDs in database: {string.Join(", ", diagnostics.AvailableRobotIds)}");
            analysisLines.Add("Verify the robot ID is correct (case-sensitive, no extra whitespace).");
        }
        else if (diagnostics.MissionsMatchingDateRange == 0)
        {
            analysisLines.Add($"Found {diagnostics.MissionsMatchingRobotId} missions for the specified robot, but NONE fall within the date range.");
            analysisLines.Add($"Query range: {normalizedStart:yyyy-MM-dd HH:mm:ss} UTC to {normalizedEnd:yyyy-MM-dd HH:mm:ss} UTC");
            analysisLines.Add("Try expanding the date range or check if mission timestamps are in the expected timezone.");
        }
        else
        {
            analysisLines.Add($"SUCCESS: Found {diagnostics.FinalMissionsIncluded} missions matching all criteria.");
            analysisLines.Add($"Robot: {robotId ?? "first available"}, Date range: {normalizedStart:yyyy-MM-dd} to {normalizedEnd:yyyy-MM-dd}");
            analysisLines.Add($"Filter funnel: {diagnostics.TotalMissionsInDatabase} total → {diagnostics.MissionsWithAssignedRobotId} with robot → {diagnostics.MissionsWithCompletedDate} completed → {diagnostics.MissionsMatchingRobotId} matching robot → {diagnostics.FinalMissionsIncluded} in date range");
        }

        diagnostics.Analysis = string.Join(" ", analysisLines);

        _logger.LogInformation(
            "Utilization diagnostics completed. Total={Total}, WithRobot={WithRobot}, Completed={Completed}, " +
            "MatchingRobot={MatchingRobot}, MatchingDateRange={MatchingDateRange}",
            diagnostics.TotalMissionsInDatabase,
            diagnostics.MissionsWithAssignedRobotId,
            diagnostics.MissionsWithCompletedDate,
            diagnostics.MissionsMatchingRobotId,
            diagnostics.MissionsMatchingDateRange);

        return diagnostics;
    }

    private async Task<List<ChargingSession>> GetChargingSessionsAsync(
        string robotId,
        DateTime periodStartUtc,
        DateTime periodEndUtc,
        string? jwtToken,
        CancellationToken cancellationToken = default)
    {
        var chargingSessions = new List<ChargingSession>();

        try
        {
            // Format dates as required by the API (yyyy-MM-dd HH:mm:ss)
            var startTimeStr = periodStartUtc.ToString("yyyy-MM-dd HH:mm:ss");
            var endTimeStr = periodEndUtc.ToString("yyyy-MM-dd HH:mm:ss");

            _logger.LogInformation(
                "Fetching charging sessions for robot {RobotId} from {Start} to {End}",
                robotId, startTimeStr, endTimeStr);

            // Query for manualCharging missions
            var manualChargingRequest = new MissionListRequest
            {
                PageNum = 1,
                PageSize = 1000000, // Get all records
                Query = new MissionListQuery
                {
                    RobotId = robotId,
                    BeginTimeStart = startTimeStr,
                    BeginTimeEnd = endTimeStr,
                    TemplateCode = "manualCharging"
                }
            };

            var manualChargingResponse = await _missionListClient.GetMissionListAsync(
                manualChargingRequest, jwtToken, cancellationToken);

            if (manualChargingResponse?.Data?.Content != null)
            {
                _logger.LogInformation(
                    "Found {Count} manual charging missions",
                    manualChargingResponse.Data.Content.Count);

                chargingSessions.AddRange(ProcessChargingMissions(manualChargingResponse.Data.Content));
            }

            // Query for autoCharging missions
            var autoChargingRequest = new MissionListRequest
            {
                PageNum = 1,
                PageSize = 1000000,
                Query = new MissionListQuery
                {
                    RobotId = robotId,
                    BeginTimeStart = startTimeStr,
                    BeginTimeEnd = endTimeStr,
                    TemplateCode = "autoCharging"
                }
            };

            var autoChargingResponse = await _missionListClient.GetMissionListAsync(
                autoChargingRequest, jwtToken, cancellationToken);

            if (autoChargingResponse?.Data?.Content != null)
            {
                _logger.LogInformation(
                    "Found {Count} auto charging missions",
                    autoChargingResponse.Data.Content.Count);

                chargingSessions.AddRange(ProcessChargingMissions(autoChargingResponse.Data.Content));
            }

            _logger.LogInformation(
                "Total charging sessions found: {Count}",
                chargingSessions.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Error fetching charging sessions for robot {RobotId}", robotId);
        }

        return chargingSessions;
    }

    private List<ChargingSession> ProcessChargingMissions(List<MissionListItem> missions)
    {
        var sessions = new List<ChargingSession>();

        foreach (var mission in missions)
        {
            // Only process FINISHED missions
            if (!string.Equals(mission.Status, "FINISHED", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            // Parse begin and end times
            if (!DateTime.TryParseExact(
                mission.BeginTime,
                "yyyy-MM-dd HH:mm:ss",
                CultureInfo.InvariantCulture,
                DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal,
                out var beginTime))
            {
                _logger.LogWarning(
                    "Failed to parse BeginTime '{BeginTime}' for mission {MissionCode}",
                    mission.BeginTime, mission.Code);
                continue;
            }

            if (!DateTime.TryParseExact(
                mission.EndTime,
                "yyyy-MM-dd HH:mm:ss",
                CultureInfo.InvariantCulture,
                DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal,
                out var endTime))
            {
                _logger.LogWarning(
                    "Failed to parse EndTime '{EndTime}' for mission {MissionCode}",
                    mission.EndTime, mission.Code);
                continue;
            }

            var duration = (endTime - beginTime).TotalMinutes;

            if (duration < 0)
            {
                _logger.LogWarning(
                    "Negative duration detected for mission {MissionCode}: {Duration} minutes",
                    mission.Code, duration);
                continue;
            }

            sessions.Add(new ChargingSession
            {
                MissionCode = mission.Code,
                TemplateCode = mission.TemplateCode,
                TemplateName = mission.TemplateName,
                BeginTimeUtc = beginTime,
                EndTimeUtc = endTime,
                DurationMinutes = Math.Round(duration, 2),
                Status = mission.Status,
                RobotId = mission.RobotId
            });
        }

        return sessions;
    }

    private static DateTime NormalizeToUtc(DateTime value) =>
        value.Kind switch
        {
            DateTimeKind.Utc => value,
            DateTimeKind.Local => value.ToUniversalTime(),
            _ => DateTime.SpecifyKind(value, DateTimeKind.Utc)
        };

    private static DateTime Max(DateTime first, DateTime second) =>
        first >= second ? first : second;

    private static DateTime Min(DateTime first, DateTime second) =>
        first <= second ? first : second;

    private static DateTime GetBucketStart(DateTime value, UtilizationGroupingInterval grouping)
    {
        var normalized = NormalizeToUtc(value);

        return grouping switch
        {
            UtilizationGroupingInterval.Hour => new DateTime(normalized.Year, normalized.Month, normalized.Day, normalized.Hour, 0, 0, DateTimeKind.Utc),
            UtilizationGroupingInterval.Day => new DateTime(normalized.Year, normalized.Month, normalized.Day, 0, 0, 0, DateTimeKind.Utc),
            _ => throw new ArgumentOutOfRangeException(nameof(grouping), grouping, "Unsupported grouping interval.")
        };
    }

    private static IEnumerable<(DateTime BucketStartUtc, double Minutes)> SplitInterval(
        DateTime intervalStart,
        DateTime intervalEnd,
        DateTime periodStart,
        DateTime periodEnd,
        UtilizationGroupingInterval grouping,
        TimeSpan bucketSize)
    {
        var boundedStart = Max(intervalStart, periodStart);
        var boundedEnd = Min(intervalEnd, periodEnd);
        if (boundedEnd <= boundedStart)
        {
            yield break;
        }

        var currentBucketStart = GetBucketStart(boundedStart, grouping);
        while (currentBucketStart < boundedEnd)
        {
            var bucketEnd = currentBucketStart.Add(bucketSize);
            var sliceStart = Max(boundedStart, currentBucketStart);
            var sliceEnd = Min(boundedEnd, bucketEnd);

            if (sliceEnd > sliceStart)
            {
                yield return (currentBucketStart, (sliceEnd - sliceStart).TotalMinutes);
            }

            currentBucketStart = bucketEnd;
        }
    }
}
