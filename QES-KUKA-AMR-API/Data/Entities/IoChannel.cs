using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace QES_KUKA_AMR_API.Data.Entities
{
    /// <summary>
    /// Represents a single IO channel (DI or DO) on an IO controller device.
    /// For ADAM-6052: 8 Digital Inputs (DI 0-7) and 8 Digital Outputs (DO 0-7).
    /// </summary>
    [Table("IoChannels")]
    public class IoChannel
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        /// <summary>
        /// Foreign key to the parent IoControllerDevice
        /// </summary>
        public int DeviceId { get; set; }

        /// <summary>
        /// Channel number (0-7 for both DI and DO on ADAM-6052)
        /// </summary>
        public int ChannelNumber { get; set; }

        /// <summary>
        /// Type of channel (DigitalInput or DigitalOutput)
        /// </summary>
        public IoChannelType ChannelType { get; set; }

        /// <summary>
        /// User-friendly label for this channel (e.g., "Emergency Stop", "Line Ready Signal")
        /// </summary>
        [MaxLength(100)]
        public string? Label { get; set; }

        /// <summary>
        /// Current state of the channel (true = ON/HIGH, false = OFF/LOW)
        /// </summary>
        public bool CurrentState { get; set; }

        /// <summary>
        /// Fail-Safe Value for Digital Output channels (value when communication WDT triggers).
        /// Only applicable for DO channels, null for DI channels.
        /// </summary>
        public bool? FailSafeValue { get; set; }

        /// <summary>
        /// Whether FSV (Fail-Safe Value) is enabled for this DO channel
        /// </summary>
        public bool FsvEnabled { get; set; } = false;

        /// <summary>
        /// Timestamp of the last state change for this channel
        /// </summary>
        public DateTime? LastStateChangeUtc { get; set; }

        /// <summary>
        /// Navigation property to the parent device
        /// </summary>
        [ForeignKey(nameof(DeviceId))]
        public IoControllerDevice? Device { get; set; }
    }
}
