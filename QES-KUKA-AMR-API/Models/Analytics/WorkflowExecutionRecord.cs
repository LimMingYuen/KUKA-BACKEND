namespace QES_KUKA_AMR_API.Models.Analytics;

/// <summary>
/// Represents a workflow execution record from the JobQuery API
/// </summary>
public class WorkflowExecutionRecord
{
    public string JobCode { get; set; } = string.Empty;
    public string RobotId { get; set; } = string.Empty;
    public int Status { get; set; }
    public string? WorkflowName { get; set; }
    public string? CompleteTime { get; set; }
    public int SpendTime { get; set; } // milliseconds
    public double DurationMinutes => SpendTime / 60.0;

    public bool IsCompleted => Status == 5; // Complete status code
    public bool IsCancelled => Status == 31; // Cancelled status code
    public bool IsError => Status == 99; // StartupError status code
    public bool IsRunning => Status == 2; // Executing status code
    public bool IsWaiting => Status == 3; // Waiting status code

    public string GetStatusName()
    {
        return Status switch
        {
            0 => "Created",
            2 => "Executing",
            3 => "Waiting",
            4 => "Cancelling",
            5 => "Completed",
            31 => "Cancelled",
            32 => "Manual Complete",
            50 => "Warning",
            99 => "Error",
            _ => $"Unknown ({Status})"
        };
    }

    public string GetStatusCssClass()
    {
        return Status switch
        {
            0 => "secondary",
            2 => "info",
            3 => "warning",
            4 => "warning",
            5 => "success",
            31 => "warning",
            32 => "success",
            50 => "warning",
            99 => "danger",
            _ => "secondary"
        };
    }
}

/// <summary>
/// DTO for JobQuery API response
/// </summary>
public class JobQueryResponseDto
{
    public List<JobDto>? Data { get; set; }
    public string Code { get; set; } = "0";
    public string? Message { get; set; }
    public bool Success { get; set; } = true;
}

/// <summary>
/// DTO for individual job from JobQuery API
/// </summary>
public class JobDto
{
    public string JobCode { get; set; } = string.Empty;
    public long? WorkflowId { get; set; }
    public string? ContainerCode { get; set; }
    public string? RobotId { get; set; }
    public int Status { get; set; }
    public string? WorkflowName { get; set; }
    public string? WorkflowCode { get; set; }
    public int? WorkflowPriority { get; set; }
    public string? MapCode { get; set; }
    public string? CompleteTime { get; set; }
    public int? SpendTime { get; set; }
}
