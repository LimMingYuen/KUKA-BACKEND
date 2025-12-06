using QES_KUKA_AMR_API.Services.Licensing.Models;

namespace QES_KUKA_AMR_API.Services.Licensing;

public interface ILicenseValidationService
{
    /// <summary>
    /// Validates the current license file.
    /// </summary>
    Task<LicenseValidationResult> ValidateLicenseAsync(CancellationToken ct = default);

    /// <summary>
    /// Activates a license from an uploaded file.
    /// </summary>
    Task<LicenseValidationResult> ActivateLicenseAsync(Stream licenseFileStream, CancellationToken ct = default);

    /// <summary>
    /// Gets the current license info (if valid).
    /// </summary>
    LicenseInfo? GetCurrentLicenseInfo();
}
