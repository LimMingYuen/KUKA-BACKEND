using QES_KUKA_AMR_API.Services.Licensing.Models;

namespace QES_KUKA_AMR_API.Services.Licensing;

public interface ILicenseStateService
{
    /// <summary>
    /// Gets whether the application is in limited mode (unlicensed).
    /// </summary>
    bool IsLimitedMode { get; }

    /// <summary>
    /// Sets the limited mode state.
    /// </summary>
    void SetLimitedMode(bool isLimited);

    /// <summary>
    /// Gets the current license info (null if unlicensed).
    /// </summary>
    LicenseInfo? CurrentLicenseInfo { get; }

    /// <summary>
    /// Sets the current license info.
    /// </summary>
    void SetLicenseInfo(LicenseInfo? info);
}
