using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace QES_KUKA_AMR_API.Data.Entities
{
    /// <summary>
    /// Represents an IO controller device (e.g., ADAM-6052 Modbus module) for managing digital I/O channels.
    /// </summary>
    [Table("IoControllerDevices")]
    public class IoControllerDevice
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        /// <summary>
        /// User-friendly device name (e.g., "Line 1 IO Module", "Staging Area Controller")
        /// </summary>
        [Required]
        [MaxLength(100)]
        public string DeviceName { get; set; } = string.Empty;

        /// <summary>
        /// IP address of the Modbus TCP device
        /// </summary>
        [Required]
        [MaxLength(45)]
        public string IpAddress { get; set; } = string.Empty;

        /// <summary>
        /// Modbus TCP port (default 502)
        /// </summary>
        public int Port { get; set; } = 502;

        /// <summary>
        /// Modbus Unit/Slave ID (default 1)
        /// </summary>
        public byte UnitId { get; set; } = 1;

        /// <summary>
        /// Optional description/notes about this device
        /// </summary>
        [MaxLength(500)]
        public string? Description { get; set; }

        /// <summary>
        /// Whether this device is enabled for polling
        /// </summary>
        public bool IsActive { get; set; } = true;

        /// <summary>
        /// Polling interval in milliseconds (default 1000ms)
        /// </summary>
        public int PollingIntervalMs { get; set; } = 1000;

        /// <summary>
        /// Connection timeout in milliseconds (default 3000ms)
        /// </summary>
        public int ConnectionTimeoutMs { get; set; } = 3000;

        /// <summary>
        /// Last time the device was successfully polled
        /// </summary>
        public DateTime? LastPollUtc { get; set; }

        /// <summary>
        /// Last connection status (true = connected successfully, false = connection failed)
        /// </summary>
        public bool? LastConnectionSuccess { get; set; }

        /// <summary>
        /// Last error message if connection failed
        /// </summary>
        [MaxLength(500)]
        public string? LastErrorMessage { get; set; }

        /// <summary>
        /// Timestamp when this device record was created
        /// </summary>
        public DateTime CreatedUtc { get; set; }

        /// <summary>
        /// Timestamp when this device record was last updated
        /// </summary>
        public DateTime UpdatedUtc { get; set; }

        /// <summary>
        /// Username of the person who created this device record
        /// </summary>
        [MaxLength(100)]
        public string? CreatedBy { get; set; }

        /// <summary>
        /// Username of the person who last updated this device record
        /// </summary>
        [MaxLength(100)]
        public string? UpdatedBy { get; set; }
    }
}
