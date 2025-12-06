namespace QES_KUKA_AMR_API.Services.Licensing.Models;

/// <summary>
/// The license data for a robot license.
/// </summary>
public class RobotLicenseData
{
    public string LicenseId { get; set; } = string.Empty;
    public string RobotId { get; set; } = string.Empty;
    public string MachineId { get; set; } = string.Empty;
    public string CustomerName { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public string ProductVersion { get; set; } = string.Empty;
    public DateTime IssuedAt { get; set; }
    public DateTime? ExpiresAt { get; set; }
}
