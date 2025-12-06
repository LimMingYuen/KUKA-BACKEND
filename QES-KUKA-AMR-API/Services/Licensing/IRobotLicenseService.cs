using QES_KUKA_AMR_API.Services.Licensing.Models;

namespace QES_KUKA_AMR_API.Services.Licensing;

/// <summary>
/// Service for validating and managing robot licenses.
/// </summary>
public interface IRobotLicenseService
{
    /// <summary>
    /// Validates a robot license file for a specific robot.
    /// </summary>
    Task<RobotLicenseValidationResult> ValidateRobotLicenseAsync(string robotId);

    /// <summary>
    /// Validates all robot licenses and returns a dictionary of results keyed by robot ID.
    /// </summary>
    Task<Dictionary<string, RobotLicenseValidationResult>> ValidateAllRobotLicensesAsync(IEnumerable<string> robotIds);

    /// <summary>
    /// Activates a robot license from an uploaded file stream.
    /// </summary>
    Task<RobotLicenseValidationResult> ActivateRobotLicenseAsync(string robotId, Stream licenseFileStream);

    /// <summary>
    /// Gets the license status for all robots.
    /// </summary>
    Task<List<RobotLicenseStatus>> GetAllRobotLicenseStatusesAsync(IEnumerable<string> robotIds);

    /// <summary>
    /// Gets the license status for a specific robot.
    /// </summary>
    Task<RobotLicenseStatus> GetRobotLicenseStatusAsync(string robotId);
}
