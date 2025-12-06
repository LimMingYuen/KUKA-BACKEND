namespace QES_KUKA_AMR_API.Services.Licensing.Models;

/// <summary>
/// Result of robot license validation.
/// </summary>
public class RobotLicenseValidationResult
{
    public bool IsValid { get; set; }
    public string? ErrorCode { get; set; }
    public string? ErrorMessage { get; set; }
    public RobotLicenseData? LicenseData { get; set; }

    public static RobotLicenseValidationResult Success(RobotLicenseData licenseData) => new()
    {
        IsValid = true,
        LicenseData = licenseData
    };

    public static RobotLicenseValidationResult Failure(string errorCode, string errorMessage) => new()
    {
        IsValid = false,
        ErrorCode = errorCode,
        ErrorMessage = errorMessage
    };
}
