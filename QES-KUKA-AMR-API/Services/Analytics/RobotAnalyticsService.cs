using System.Globalization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using QES_KUKA_AMR_API.Data;
using QES_KUKA_AMR_API.Data.Entities;
using QES_KUKA_AMR_API.Models.Analytics;
using QES_KUKA_AMR_API.Models.Missions;
using QES_KUKA_AMR_API.Services.Auth;
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
        TimeSpan? clientTimezoneOffset = null,
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
    private readonly IExternalApiTokenService _externalApiTokenService;
    private readonly ILogger<RobotAnalyticsService> _logger;

    // External KUKA API returns timestamps in Malaysia local time (UTC+8)
    private static readonly TimeZoneInfo MalaysiaTimeZone =
        TimeZoneInfo.FindSystemTimeZoneById("Singapore Standard Time");

    public RobotAnalyticsService(
        ApplicationDbContext dbContext,
        IWorkflowAnalyticsService workflowAnalyticsService,
        IMissionListClient missionListClient,
        IExternalApiTokenService externalApiTokenService,
        ILogger<RobotAnalyticsService> logger)
    {
        _dbContext = dbContext;
        _workflowAnalyticsService = workflowAnalyticsService;
        _missionListClient = missionListClient;
        _externalApiTokenService = externalApiTokenService;
        _logger = logger;
    }

    public async Task<RobotUtilizationMetrics> GetUtilizationAsync(
        string robotId,
        DateTime periodStartUtc,
        DateTime periodEndUtc,
        UtilizationGroupingInterval grouping,
        string? jwtToken = null,
        TimeSpan? clientTimezoneOffset = null,
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

        // Use timezone offset to align buckets to client's local time instead of UTC
        var timezoneOffset = clientTimezoneOffset ?? TimeSpan.Zero;

        var metrics = new RobotUtilizationMetrics
        {
            RobotId = robotId.Trim(),
            PeriodStartUtc = normalizedStart,
            PeriodEndUtc = normalizedEnd,
            TotalAvailableMinutes = Math.Max(0, (normalizedEnd - normalizedStart).TotalMinutes)
        };

        // MissionQueues entity removed - no active/recent missions to query
        var emptyMissions = new { MissionCode = "", WorkflowName = "", SavedMissionId = (int?)null, TriggerSource = MissionTriggerSource.Manual, Start = (DateTime?)null, End = (DateTime?)null, Status = "Completed" };
        var queueMissions = new[] { emptyMissions }.Where(x => false).ToList(); // Empty list with correct anonymous type

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
                Status = "Completed"  // History missions are always completed
            })
            .Where(m => m.Start != null && m.End != null && m.Start < normalizedEnd && m.End > normalizedStart)
            .ToListAsync(cancellationToken);

        // Combine both sources
        var missionSnapshots = queueMissions.Concat(historyMissions).ToList();

        _logger.LogInformation("Found {QueueCount} missions in queue (removed) and {HistoryCount} in MissionHistories for robot {RobotId}",
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
            foreach (var segment in SplitInterval(start, end, normalizedStart, normalizedEnd, grouping, bucketSize, timezoneOffset))
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

                foreach (var segment in SplitInterval(pauseStart, pauseEnd, normalizedStart, normalizedEnd, grouping, bucketSize, timezoneOffset))
                {
                    manualOverlapsByBucket.TryGetValue(segment.BucketStartUtc, out var existing);
                    manualOverlapsByBucket[segment.BucketStartUtc] = existing + segment.Minutes;
                }
            }

            var effectiveMissionMinutes = Math.Max(0, missionDurationMinutes - manualOverlapMinutes);

            foreach (var segment in SplitInterval(boundedStart, boundedEnd, normalizedStart, normalizedEnd, grouping, bucketSize, timezoneOffset))
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

        // Generate ALL buckets in the date range (fills gaps with zeros)
        // Buckets are aligned to client's local midnight using timezoneOffset
        var allBuckets = GenerateAllBuckets(normalizedStart, normalizedEnd, grouping, bucketSize, timezoneOffset)
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
        // Obtain external API token for authenticating with AMR system
        string externalToken;
        try
        {
            externalToken = await _externalApiTokenService.GetTokenAsync(cancellationToken);
            _logger.LogInformation("Successfully obtained external API token for charging sessions query");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to obtain external API token for charging sessions");
            // Continue with empty charging sessions rather than failing the entire analytics request
            externalToken = string.Empty;
        }

        var chargingSessions = await GetChargingSessionsAsync(
            robotId,
            normalizedStart,
            normalizedEnd,
            externalToken,
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
                boundedStart, boundedEnd, normalizedStart, normalizedEnd, grouping, bucketSize, timezoneOffset))
            {
                chargingMinutesByBucket.TryGetValue(segment.BucketStartUtc, out var existing);
                chargingMinutesByBucket[segment.BucketStartUtc] = existing + segment.Minutes;
            }
        }

        // Calculate totals with proper capping to ensure they don't exceed TotalAvailableMinutes
        var rawTotalChargingMinutes = chargingMinutesByBucket.Values.Sum();
        var rawTotalWorkingMinutes = metrics.UtilizedMinutes;

        // Cap TotalWorkingMinutes to TotalAvailableMinutes
        metrics.TotalWorkingMinutes = Math.Min(rawTotalWorkingMinutes, metrics.TotalAvailableMinutes);

        // Cap TotalChargingMinutes to remaining available time after working
        var remainingTotalAfterWorking = metrics.TotalAvailableMinutes - metrics.TotalWorkingMinutes;
        metrics.TotalChargingMinutes = Math.Min(rawTotalChargingMinutes, remainingTotalAfterWorking);

        // Calculate TotalIdleMinutes as the remainder
        metrics.TotalIdleMinutes = Math.Max(0,
            metrics.TotalAvailableMinutes - metrics.TotalWorkingMinutes - metrics.TotalChargingMinutes);

        // Update breakdown with charging, working, and idle minutes
        // IMPORTANT: Ensure Working + Charging + Idle = TotalAvailableMinutes (no overlap causing >24h)
        foreach (var bucket in metrics.Breakdown)
        {
            chargingMinutesByBucket.TryGetValue(bucket.BucketStartUtc, out var chargingMinutes);

            var rawWorkingMinutes = bucket.UtilizedMinutes;
            var rawChargingMinutes = chargingMinutes;

            // Step 1: Cap WorkingMinutes to TotalAvailableMinutes
            // This handles cases where overlapping mission records cause accumulated time > 24h
            bucket.WorkingMinutes = Math.Min(rawWorkingMinutes, bucket.TotalAvailableMinutes);

            if (rawWorkingMinutes > bucket.TotalAvailableMinutes)
            {
                _logger.LogWarning(
                    "WorkingMinutes ({RawWorking}min) exceeded TotalAvailableMinutes ({Available}min) for bucket {BucketStart}. " +
                    "Capped to {Capped}min. This may indicate overlapping mission records in the database.",
                    rawWorkingMinutes, bucket.TotalAvailableMinutes, bucket.BucketStartUtc, bucket.WorkingMinutes);
            }

            // Step 2: Cap ChargingMinutes to remaining available time after working
            var remainingAfterWorking = bucket.TotalAvailableMinutes - bucket.WorkingMinutes;
            bucket.ChargingMinutes = Math.Min(Math.Round(rawChargingMinutes, 2), remainingAfterWorking);

            if (rawChargingMinutes > remainingAfterWorking)
            {
                _logger.LogDebug(
                    "ChargingMinutes ({RawCharging}min) exceeded remaining time ({Remaining}min) for bucket {BucketStart}. " +
                    "Capped to {Capped}min. Working and Charging time overlap detected.",
                    rawChargingMinutes, remainingAfterWorking, bucket.BucketStartUtc, bucket.ChargingMinutes);
            }

            // Step 3: Calculate idle time as the remainder (guaranteed to be non-negative)
            bucket.IdleMinutes = Math.Max(0,
                bucket.TotalAvailableMinutes - bucket.WorkingMinutes - bucket.ChargingMinutes);
        }

        // Calculate utilization: Working / (Available - Charging)
        var effectiveAvailableExcludingCharging = Math.Max(0, metrics.TotalAvailableMinutes - metrics.TotalChargingMinutes);
        if (effectiveAvailableExcludingCharging <= 0)
        {
            metrics.UtilizationPercent = 0;
        }
        else
        {
            // Utilization = Working / (Available - Charging)
            metrics.UtilizationPercent = Math.Round(Math.Min(metrics.TotalWorkingMinutes / effectiveAvailableExcludingCharging, 1.0) * 100, 2);
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

        // MissionQueues entity removed - no active/recent missions to query
        var emptyMissions = new { MissionCode = "", AssignedRobotId = "", ProcessedDate = (DateTime?)null, SubmittedToAmrDate = (DateTime?)null, CreatedDate = (DateTime?)null, CompletedDate = (DateTime?)null, WorkflowName = "", Status = "", Start = (DateTime?)null };
        var queueMissions = new[] { emptyMissions }.Where(x => false).ToList(); // Empty list with correct anonymous type

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

        // MissionQueues removed, only using history missions now
        var allMissions = historyMissions.ToList();

        diagnostics.TotalMissionsInDatabase = allMissions.Count;
        _logger.LogInformation("Diagnostic found {QueueCount} in queue (removed) + {HistoryCount} in MissionHistories = {Total} total",
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

            // Parse begin and end times (external API returns Malaysia local time UTC+8)
            if (!DateTime.TryParseExact(
                mission.BeginTime,
                "yyyy-MM-dd HH:mm:ss",
                CultureInfo.InvariantCulture,
                DateTimeStyles.None,
                out var localBeginTime))
            {
                _logger.LogWarning(
                    "Failed to parse BeginTime '{BeginTime}' for mission {MissionCode}",
                    mission.BeginTime, mission.Code);
                continue;
            }
            var beginTime = TimeZoneInfo.ConvertTimeToUtc(localBeginTime, MalaysiaTimeZone);

            if (!DateTime.TryParseExact(
                mission.EndTime,
                "yyyy-MM-dd HH:mm:ss",
                CultureInfo.InvariantCulture,
                DateTimeStyles.None,
                out var localEndTime))
            {
                _logger.LogWarning(
                    "Failed to parse EndTime '{EndTime}' for mission {MissionCode}",
                    mission.EndTime, mission.Code);
                continue;
            }
            var endTime = TimeZoneInfo.ConvertTimeToUtc(localEndTime, MalaysiaTimeZone);

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

    private static DateTime GetBucketStart(DateTime value, UtilizationGroupingInterval grouping, TimeSpan timezoneOffset = default)
    {
        var normalized = NormalizeToUtc(value);

        // To align buckets to client's local midnight instead of UTC midnight:
        // 1. Convert UTC time to local time by adding the offset
        // 2. Truncate to local midnight/hour
        // 3. Convert back to UTC by subtracting the offset
        var localTime = normalized.Add(timezoneOffset);

        var localBucketStart = grouping switch
        {
            UtilizationGroupingInterval.Hour => new DateTime(localTime.Year, localTime.Month, localTime.Day, localTime.Hour, 0, 0, DateTimeKind.Unspecified),
            UtilizationGroupingInterval.Day => new DateTime(localTime.Year, localTime.Month, localTime.Day, 0, 0, 0, DateTimeKind.Unspecified),
            _ => throw new ArgumentOutOfRangeException(nameof(grouping), grouping, "Unsupported grouping interval.")
        };

        // Convert back to UTC
        var utcBucketStart = DateTime.SpecifyKind(localBucketStart.Subtract(timezoneOffset), DateTimeKind.Utc);
        return utcBucketStart;
    }

    private static IEnumerable<(DateTime BucketStartUtc, double Minutes)> SplitInterval(
        DateTime intervalStart,
        DateTime intervalEnd,
        DateTime periodStart,
        DateTime periodEnd,
        UtilizationGroupingInterval grouping,
        TimeSpan bucketSize,
        TimeSpan timezoneOffset = default)
    {
        var boundedStart = Max(intervalStart, periodStart);
        var boundedEnd = Min(intervalEnd, periodEnd);
        if (boundedEnd <= boundedStart)
        {
            yield break;
        }

        var currentBucketStart = GetBucketStart(boundedStart, grouping, timezoneOffset);
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

    /// <summary>
    /// Generates all bucket start timestamps for the given date range.
    /// This ensures the breakdown includes all time periods, even those with no activity.
    /// Buckets are aligned to the client's local midnight using the provided timezone offset.
    /// </summary>
    private static IEnumerable<DateTime> GenerateAllBuckets(
        DateTime periodStart,
        DateTime periodEnd,
        UtilizationGroupingInterval grouping,
        TimeSpan bucketSize,
        TimeSpan timezoneOffset)
    {
        // Get the bucket containing the start of the period, aligned to local midnight
        var currentBucket = GetBucketStart(periodStart, grouping, timezoneOffset);

        // Generate buckets until we've covered the entire period
        while (currentBucket < periodEnd)
        {
            yield return currentBucket;
            currentBucket = currentBucket.Add(bucketSize);
        }
    }
}
