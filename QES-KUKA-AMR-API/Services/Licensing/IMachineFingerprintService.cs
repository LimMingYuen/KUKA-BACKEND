namespace QES_KUKA_AMR_API.Services.Licensing;

public interface IMachineFingerprintService
{
    /// <summary>
    /// Generates a unique fingerprint hash for this machine.
    /// </summary>
    string GenerateFingerprint();

    /// <summary>
    /// Gets the fingerprint formatted for display (with dashes for readability).
    /// </summary>
    string GetDisplayFingerprint();
}
