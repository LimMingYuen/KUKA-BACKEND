using System.ComponentModel.DataAnnotations;

namespace QES_KUKA_AMR_API.Models.AutoSync;

/// <summary>
/// DTO for returning auto-sync settings
/// </summary>
public class AutoSyncSettingsDto
{
    public bool Enabled { get; set; }
    public int IntervalMinutes { get; set; }
    public bool SyncWorkflows { get; set; }
    public bool SyncQrCodes { get; set; }
    public bool SyncMapZones { get; set; }
    public bool SyncMobileRobots { get; set; }
    public DateTime? LastRun { get; set; }
    public DateTime? NextRun { get; set; }
}

/// <summary>
/// Request DTO for updating auto-sync settings
/// </summary>
public class AutoSyncSettingsUpdateRequest
{
    public bool Enabled { get; set; }

    [Range(1, 1440, ErrorMessage = "Interval must be between 1 and 1440 minutes (24 hours)")]
    public int IntervalMinutes { get; set; } = 60;

    public bool SyncWorkflows { get; set; } = true;
    public bool SyncQrCodes { get; set; } = true;
    public bool SyncMapZones { get; set; } = true;
    public bool SyncMobileRobots { get; set; } = true;
}

/// <summary>
/// Result of a sync operation
/// </summary>
public class SyncResultDto
{
    public string ApiName { get; set; } = string.Empty;
    public bool Success { get; set; }
    public int Total { get; set; }
    public int Inserted { get; set; }
    public int Updated { get; set; }
    public string? ErrorMessage { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Result of running all enabled syncs
/// </summary>
public class AutoSyncRunResultDto
{
    public DateTime RunTime { get; set; } = DateTime.UtcNow;
    public List<SyncResultDto> Results { get; set; } = new();
    public int TotalApis => Results.Count;
    public int SuccessfulApis => Results.Count(r => r.Success);
    public int FailedApis => Results.Count(r => !r.Success);
}
