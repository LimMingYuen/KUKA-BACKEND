namespace QES_KUKA_AMR_API.Services.Licensing.Models;

public class LicenseData
{
    public string LicenseId { get; set; } = string.Empty;
    public string MachineId { get; set; } = string.Empty;
    public string CustomerName { get; set; } = string.Empty;
    public string CustomerEmail { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public string ProductVersion { get; set; } = string.Empty;
    public string LicenseType { get; set; } = string.Empty;
    public int MaxRobots { get; set; }
    public List<string> Features { get; set; } = new();
    public DateTime IssuedAt { get; set; }
    public DateTime? ExpiresAt { get; set; }
}
