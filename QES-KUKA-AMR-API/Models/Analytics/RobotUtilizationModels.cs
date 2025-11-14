namespace QES_KUKA_AMR_API.Models.Analytics;

public class RobotUtilizationMetrics
{
    public string RobotId { get; set; } = string.Empty;

    public DateTime PeriodStartUtc { get; set; }

    public DateTime PeriodEndUtc { get; set; }

    public double TotalAvailableMinutes { get; set; }

    public double ManualPauseMinutes { get; set; }

    public double UtilizedMinutes { get; set; }

    public double UtilizationPercent { get; set; }

    // Charging-related metrics
    public double TotalChargingMinutes { get; set; }

    public double TotalWorkingMinutes { get; set; }

    public double TotalIdleMinutes { get; set; }

    public List<RobotUtilizationBreakdown> Breakdown { get; set; } = new();

    public List<RobotUtilizationMission> Missions { get; set; } = new();

    public List<ChargingSession> ChargingSessions { get; set; } = new();

    // Workflow metrics
    public List<WorkflowExecutionRecord> WorkflowExecutions { get; set; } = new();

    // Workflow summary properties
    public int TotalWorkflows => WorkflowExecutions.Count;

    public int CompletedWorkflows => WorkflowExecutions.Count(w => w.IsCompleted);

    public int CancelledWorkflows => WorkflowExecutions.Count(w => w.IsCancelled);

    public int ErrorWorkflows => WorkflowExecutions.Count(w => w.IsError);

    public int RunningWorkflows => WorkflowExecutions.Count(w => w.IsRunning);

    public int WaitingWorkflows => WorkflowExecutions.Count(w => w.IsWaiting);

    public double WorkflowSuccessRate => TotalWorkflows > 0
        ? (double)CompletedWorkflows / TotalWorkflows * 100
        : 0;

    public double AverageWorkflowDurationMinutes => WorkflowExecutions.Any()
        ? WorkflowExecutions.Average(w => w.DurationMinutes)
        : 0;

    public double TotalWorkflowMinutes => WorkflowExecutions.Sum(w => w.DurationMinutes);
}

public class RobotUtilizationBreakdown
{
    public DateTime BucketStartUtc { get; set; }

    public double TotalAvailableMinutes { get; set; }

    public double UtilizedMinutes { get; set; }

    public double ManualPauseMinutes { get; set; }

    public int CompletedMissions { get; set; }

    // Detailed breakdown
    public double ChargingMinutes { get; set; }

    public double WorkingMinutes { get; set; }

    public double IdleMinutes { get; set; }
}

public class RobotUtilizationMission
{
    public string MissionCode { get; set; } = string.Empty;

    public string? WorkflowName { get; set; }

    public int? SavedMissionId { get; set; }

    public string TriggerSource { get; set; } = "Manual";

    public DateTime? StartTimeUtc { get; set; }

    public DateTime? CompletedTimeUtc { get; set; }

    public double DurationMinutes { get; set; }

    public double ManualPauseMinutes { get; set; }
}

public enum UtilizationGroupingInterval
{
    Hour,
    Day
}

public class RobotUtilizationDiagnostics
{
    public string? RobotId { get; set; }

    public DateTime QueryStartUtc { get; set; }

    public DateTime QueryEndUtc { get; set; }

    public int TotalMissionsInDatabase { get; set; }

    public int MissionsWithAssignedRobotId { get; set; }

    public int MissionsWithCompletedDate { get; set; }

    public int MissionsMatchingRobotId { get; set; }

    public int MissionsMatchingDateRange { get; set; }

    public int FinalMissionsIncluded { get; set; }

    public List<MissionDiagnosticInfo> SampleMissions { get; set; } = new();

    public List<string> AvailableRobotIds { get; set; } = new();

    public string Analysis { get; set; } = string.Empty;
}

public class MissionDiagnosticInfo
{
    public string MissionCode { get; set; } = string.Empty;

    public string? AssignedRobotId { get; set; }

    public DateTime? ProcessedDate { get; set; }

    public DateTime? SubmittedToAmrDate { get; set; }

    public DateTime? CreatedDate { get; set; }

    public DateTime? CompletedDate { get; set; }

    public string? WorkflowName { get; set; }

    public string Status { get; set; } = string.Empty;

    public bool IncludedInQuery { get; set; }

    public string ExclusionReason { get; set; } = string.Empty;
}

public class ChargingSession
{
    public string MissionCode { get; set; } = string.Empty;

    public string TemplateCode { get; set; } = string.Empty;

    public string TemplateName { get; set; } = string.Empty;

    public DateTime BeginTimeUtc { get; set; }

    public DateTime EndTimeUtc { get; set; }

    public double DurationMinutes { get; set; }

    public string Status { get; set; } = string.Empty;

    public string RobotId { get; set; } = string.Empty;
}
