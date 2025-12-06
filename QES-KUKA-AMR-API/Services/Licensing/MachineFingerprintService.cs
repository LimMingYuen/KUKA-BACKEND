using System.Net.NetworkInformation;
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

        var components = new List<string>();

        // 1. Get MAC address of primary network adapter
        var macAddress = GetMacAddress();
        if (!string.IsNullOrEmpty(macAddress))
        {
            components.Add($"MAC:{macAddress}");
        }

        // 2. Get Windows Machine GUID
        var machineGuid = GetWindowsMachineGuid();
        if (!string.IsNullOrEmpty(machineGuid))
        {
            components.Add($"GUID:{machineGuid}");
        }

        // 3. Get machine name
        var machineName = Environment.MachineName;
        if (!string.IsNullOrEmpty(machineName))
        {
            components.Add($"NAME:{machineName}");
        }

        // 4. Get processor count (basic but stable)
        components.Add($"CPU:{Environment.ProcessorCount}");

        // Combine all components and hash
        var combinedString = string.Join("|", components);
        _logger.LogDebug("Fingerprint components: {Components}", combinedString);

        using var sha256 = SHA256.Create();
        var hashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(combinedString));
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

    private string? GetMacAddress()
    {
        try
        {
            var networkInterfaces = NetworkInterface.GetAllNetworkInterfaces()
                .Where(nic => nic.OperationalStatus == OperationalStatus.Up
                           && nic.NetworkInterfaceType != NetworkInterfaceType.Loopback
                           && nic.NetworkInterfaceType != NetworkInterfaceType.Tunnel)
                .OrderByDescending(nic => nic.Speed)
                .FirstOrDefault();

            if (networkInterfaces != null)
            {
                var macBytes = networkInterfaces.GetPhysicalAddress().GetAddressBytes();
                if (macBytes.Length > 0)
                {
                    return Convert.ToHexString(macBytes);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to get MAC address");
        }

        return null;
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
