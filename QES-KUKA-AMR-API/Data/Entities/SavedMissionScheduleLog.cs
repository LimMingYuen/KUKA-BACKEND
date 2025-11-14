using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace QES_KUKA_AMR_API.Data.Entities;

[Table("SavedMissionScheduleLogs")]
public class SavedMissionScheduleLog
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    [Required]
    public int ScheduleId { get; set; }

    [ForeignKey(nameof(ScheduleId))]
    public SavedMissionSchedule? Schedule { get; set; }

    /// <summary>
    /// Denormalized SavedMissionId for efficient querying without JOIN.
    /// This value is copied from Schedule.SavedMissionId when the log is created.
    /// </summary>
    [Required]
    public int SavedMissionId { get; set; }

    public DateTime ScheduledForUtc { get; set; }

    public DateTime? EnqueuedUtc { get; set; }

    public int? QueueId { get; set; }

    [MaxLength(30)]
    public string ResultStatus { get; set; } = "Pending";

    [MaxLength(500)]
    public string? Error { get; set; }

    public DateTime CreatedUtc { get; set; }
}
