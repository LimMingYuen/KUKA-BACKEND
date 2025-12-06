namespace QES_KUKA_AMR_API.Services.Licensing.Models;

/// <summary>
/// Status of a robot's license for API responses.
/// </summary>
public class RobotLicenseStatus
{
    public string RobotId { get; set; } = string.Empty;
    public bool IsLicensed { get; set; }
    public string? ErrorCode { get; set; }
    public string? ErrorMessage { get; set; }
    public string? CustomerName { get; set; }
    public DateTime? ExpiresAt { get; set; }
    public int? DaysRemaining { get; set; }
}
