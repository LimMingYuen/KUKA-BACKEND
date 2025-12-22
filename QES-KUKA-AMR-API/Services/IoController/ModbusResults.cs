namespace QES_KUKA_AMR_API.Services.IoController;

/// <summary>
/// Result of reading digital inputs/outputs from a Modbus device.
/// </summary>
public class IoReadResult
{
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Array of boolean values for channels 0-7.
    /// Index corresponds to channel number.
    /// </summary>
    public bool[] ChannelStates { get; set; } = Array.Empty<bool>();

    public static IoReadResult Failure(string errorMessage) =>
        new() { Success = false, ErrorMessage = errorMessage };

    public static IoReadResult Ok(bool[] states) =>
        new() { Success = true, ChannelStates = states };
}

/// <summary>
/// Result of writing to a Modbus device.
/// </summary>
public class IoWriteResult
{
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }

    public static IoWriteResult Failure(string errorMessage) =>
        new() { Success = false, ErrorMessage = errorMessage };

    public static IoWriteResult Ok() =>
        new() { Success = true };
}

/// <summary>
/// Result of testing connection to a Modbus device.
/// </summary>
public class IoConnectionResult
{
    public bool IsConnected { get; set; }
    public string? ErrorMessage { get; set; }
    public int ResponseTimeMs { get; set; }

    public static IoConnectionResult Connected(int responseTimeMs) =>
        new() { IsConnected = true, ResponseTimeMs = responseTimeMs };

    public static IoConnectionResult Failed(string errorMessage) =>
        new() { IsConnected = false, ErrorMessage = errorMessage };
}

/// <summary>
/// Result of reading FSV (Fail-Safe Value) settings from a device.
/// </summary>
public class IoFsvResult
{
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// FSV enabled flags for DO channels 0-7.
    /// </summary>
    public bool[] FsvEnabled { get; set; } = Array.Empty<bool>();

    /// <summary>
    /// FSV values for DO channels 0-7 (true = HIGH on failsafe, false = LOW).
    /// </summary>
    public bool[] FsvValues { get; set; } = Array.Empty<bool>();

    public static IoFsvResult Failure(string errorMessage) =>
        new() { Success = false, ErrorMessage = errorMessage };

    public static IoFsvResult Ok(bool[] enabled, bool[] values) =>
        new() { Success = true, FsvEnabled = enabled, FsvValues = values };
}

/// <summary>
/// Result of reading WDT (Watchdog Timer) settings from a device.
/// </summary>
public class IoWdtResult
{
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Whether WDT is enabled.
    /// </summary>
    public bool WdtEnabled { get; set; }

    /// <summary>
    /// WDT timeout in seconds.
    /// </summary>
    public int TimeoutSeconds { get; set; }

    public static IoWdtResult Failure(string errorMessage) =>
        new() { Success = false, ErrorMessage = errorMessage };

    public static IoWdtResult Ok(bool enabled, int timeoutSeconds) =>
        new() { Success = true, WdtEnabled = enabled, TimeoutSeconds = timeoutSeconds };
}

/// <summary>
/// WDT settings to write to a device.
/// </summary>
public class WdtSettings
{
    public bool Enabled { get; set; }
    public int TimeoutSeconds { get; set; }
}
