using QES_KUKA_AMR_API.Services.Licensing.Models;

namespace QES_KUKA_AMR_API.Services.Licensing;

/// <summary>
/// Singleton service to track the application's license state.
/// </summary>
public class LicenseStateService : ILicenseStateService
{
    private volatile bool _isLimitedMode;
    private LicenseInfo? _currentLicenseInfo;
    private readonly object _lock = new();

    public bool IsLimitedMode => _isLimitedMode;

    public LicenseInfo? CurrentLicenseInfo
    {
        get
        {
            lock (_lock)
            {
                return _currentLicenseInfo;
            }
        }
    }

    public void SetLimitedMode(bool isLimited)
    {
        _isLimitedMode = isLimited;
    }

    public void SetLicenseInfo(LicenseInfo? info)
    {
        lock (_lock)
        {
            _currentLicenseInfo = info;
        }
    }
}
