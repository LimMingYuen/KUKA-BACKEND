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

    // Status codes from external AMR API (configured in appsettings.json JobStatusPolling.StatusCodes)
    public bool IsCompleted => Status == 30 || Status == 35; // Complete or ManualComplete
    public bool IsCancelled => Status == 31; // Cancelled
    public bool IsError => Status == 60; // Failed
    public bool IsRunning => Status == 20; // Executing
    public bool IsWaiting => Status == 25; // Waiting

    public string GetStatusName()
    {
        return Status switch
        {
            10 => "Created",
            20 => "Executing",
            25 => "Waiting",
            28 => "Cancelling",
            30 => "Complete",
            31 => "Cancelled",
            35 => "ManualComplete",
            50 => "Warning",
            60 => "Failed",
            _ => $"Unknown ({Status})"
        };
    }

    public string GetStatusCssClass()
    {
        return Status switch
        {
            10 => "secondary",
            20 => "info",
            25 => "warning",
            28 => "warning",
            30 => "success",
            31 => "warning",
            35 => "success",
            50 => "warning",
            60 => "danger",
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
