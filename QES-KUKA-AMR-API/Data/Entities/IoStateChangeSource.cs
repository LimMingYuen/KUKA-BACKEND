namespace QES_KUKA_AMR_API.Data.Entities
{
    /// <summary>
    /// Defines the source of an IO state change for audit logging purposes.
    /// </summary>
    public enum IoStateChangeSource
    {
        /// <summary>
        /// State changed by system (startup initialization, polling detected external change)
        /// </summary>
        System = 0,

        /// <summary>
        /// State changed by user action through the web interface
        /// </summary>
        User = 1,

        /// <summary>
        /// State changed via external Modbus write (from another system)
        /// </summary>
        Modbus = 2,

        /// <summary>
        /// State changed due to Fail-Safe Value activation (WDT triggered)
        /// </summary>
        FailSafe = 3
    }
}
