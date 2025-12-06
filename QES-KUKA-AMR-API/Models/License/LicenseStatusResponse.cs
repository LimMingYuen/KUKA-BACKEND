namespace QES_KUKA_AMR_API.Models.License;

public class LicenseStatusResponse
{
    public bool IsValid { get; set; }
    public string? ErrorCode { get; set; }
    public string? ErrorMessage { get; set; }
    public string? CustomerName { get; set; }
    public string? LicenseType { get; set; }
    public DateTime? ExpiresAt { get; set; }
    public int? DaysRemaining { get; set; }
    public int? MaxRobots { get; set; }
}
