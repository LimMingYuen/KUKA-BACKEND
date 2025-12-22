namespace QES_KUKA_AMR_API.Data.Entities
{
    /// <summary>
    /// Defines the type of IO channel on a controller device.
    /// </summary>
    public enum IoChannelType
    {
        /// <summary>
        /// Digital Input - read-only status from external sensors/switches
        /// </summary>
        DigitalInput = 0,

        /// <summary>
        /// Digital Output - controllable output to external devices
        /// </summary>
        DigitalOutput = 1
    }
}
