using System.Security.Cryptography;
using System.Text;
using Microsoft.Win32;

namespace QES_KUKA_AMR_API.Services.Licensing;

public class MachineFingerprintService : IMachineFingerprintService
{
    private readonly ILogger<MachineFingerprintService> _logger;
    private string? _cachedFingerprint;

    public MachineFingerprintService(ILogger<MachineFingerprintService> logger)
    {
        _logger = logger;
    }

    public string GenerateFingerprint()
    {
        if (_cachedFingerprint != null)
        {
            return _cachedFingerprint;
        }

        // Use only Windows Machine GUID - most stable identifier
        // Only changes if Windows is reinstalled (valid reason to re-license)
        var machineGuid = GetWindowsMachineGuid();

        if (string.IsNullOrEmpty(machineGuid))
        {
            _logger.LogError("Failed to retrieve Windows Machine GUID - cannot generate fingerprint");
            throw new InvalidOperationException("Cannot generate machine fingerprint: Windows Machine GUID not available");
        }

        _logger.LogDebug("Fingerprint based on Windows Machine GUID");

        using var sha256 = SHA256.Create();
        var hashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes($"GUID:{machineGuid}"));
        _cachedFingerprint = Convert.ToHexString(hashBytes);

        return _cachedFingerprint;
    }

    public string GetDisplayFingerprint()
    {
        var fingerprint = GenerateFingerprint();

        // Format with dashes for readability: XXXX-XXXX-XXXX-XXXX...
        var formatted = new StringBuilder();
        for (int i = 0; i < fingerprint.Length; i++)
        {
            if (i > 0 && i % 8 == 0)
            {
                formatted.Append('-');
            }
            formatted.Append(fingerprint[i]);
        }

        return formatted.ToString();
    }

    private string? GetWindowsMachineGuid()
    {
        try
        {
            // Windows Machine GUID from registry
            using var key = Registry.LocalMachine.OpenSubKey(
                @"SOFTWARE\Microsoft\Cryptography");

            if (key != null)
            {
                var guid = key.GetValue("MachineGuid") as string;
                return guid;
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to get Windows Machine GUID");
        }

        return null;
    }
}
