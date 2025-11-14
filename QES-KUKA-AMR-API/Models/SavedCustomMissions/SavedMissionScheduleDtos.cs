using System.ComponentModel.DataAnnotations;
using QES_KUKA_AMR_API.Data.Entities;

namespace QES_KUKA_AMR_API.Models.SavedCustomMissions;

public class SavedMissionScheduleDto
{
    public int Id { get; set; }
    public int SavedMissionId { get; set; }
    public SavedMissionTriggerType TriggerType { get; set; }
    public string? CronExpression { get; set; }
    public DateTime? OneTimeRunUtc { get; set; }
    public string TimezoneId { get; set; } = "UTC";
    public bool IsEnabled { get; set; }
    public DateTime CreatedUtc { get; set; }
    public DateTime UpdatedUtc { get; set; }
    public DateTime? LastRunUtc { get; set; }
    public string? LastStatus { get; set; }
    public string? LastError { get; set; }
    public DateTime? NextRunUtc { get; set; }
}

public class SavedMissionScheduleRequest
{
    [Required]
    public SavedMissionTriggerType TriggerType { get; set; }

    [MaxLength(120)]
    public string? CronExpression { get; set; }

    public DateTime? RunAtLocalTime { get; set; }

    [Required]
    [MaxLength(100)]
    public string TimezoneId { get; set; } = "UTC";

    public bool IsEnabled { get; set; } = true;
}

public class SavedMissionScheduleLogDto
{
    public int Id { get; set; }
    public int ScheduleId { get; set; }
    public DateTime ScheduledForUtc { get; set; }
    public DateTime? EnqueuedUtc { get; set; }
    public int? QueueId { get; set; }
    public string ResultStatus { get; set; } = "Pending";
    public string? Error { get; set; }
    public DateTime CreatedUtc { get; set; }
}

public class RunNowResponse
{
    public string MissionCode { get; set; } = string.Empty;
    public string RequestId { get; set; } = string.Empty;
    public DateTime ScheduledForUtc { get; set; }
    public int? QueueId { get; set; }
    public string Status { get; set; } = "Queued";
}
