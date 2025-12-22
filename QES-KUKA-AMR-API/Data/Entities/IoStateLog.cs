using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace QES_KUKA_AMR_API.Data.Entities
{
    /// <summary>
    /// Audit log entry for IO channel state changes. Records all state transitions
    /// with timestamps, source, and user attribution for DO changes.
    /// </summary>
    [Table("IoStateLogs")]
    public class IoStateLog
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        /// <summary>
        /// Foreign key to the IoControllerDevice where the change occurred
        /// </summary>
        public int DeviceId { get; set; }

        /// <summary>
        /// Channel number that changed (0-7)
        /// </summary>
        public int ChannelNumber { get; set; }

        /// <summary>
        /// Type of channel that changed (DigitalInput or DigitalOutput)
        /// </summary>
        public IoChannelType ChannelType { get; set; }

        /// <summary>
        /// Previous state before the change (true = ON, false = OFF)
        /// </summary>
        public bool PreviousState { get; set; }

        /// <summary>
        /// New state after the change (true = ON, false = OFF)
        /// </summary>
        public bool NewState { get; set; }

        /// <summary>
        /// Source of the state change (System, User, Modbus, FailSafe)
        /// </summary>
        public IoStateChangeSource ChangeSource { get; set; }

        /// <summary>
        /// Username of the person who triggered the change (null for system/modbus changes)
        /// </summary>
        [MaxLength(100)]
        public string? ChangedBy { get; set; }

        /// <summary>
        /// Timestamp when the change occurred
        /// </summary>
        public DateTime ChangedUtc { get; set; }

        /// <summary>
        /// Optional reason or note for the change (e.g., "Manual override for maintenance")
        /// </summary>
        [MaxLength(500)]
        public string? Reason { get; set; }

        /// <summary>
        /// Navigation property to the device
        /// </summary>
        [ForeignKey(nameof(DeviceId))]
        public IoControllerDevice? Device { get; set; }
    }
}
