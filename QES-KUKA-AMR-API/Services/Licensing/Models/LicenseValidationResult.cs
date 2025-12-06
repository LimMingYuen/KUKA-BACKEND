namespace QES_KUKA_AMR_API.Services.Licensing.Models;

public class LicenseValidationResult
{
    public bool IsValid { get; set; }
    public string? ErrorCode { get; set; }
    public string? ErrorMessage { get; set; }
    public LicenseInfo? LicenseInfo { get; set; }
}

public class LicenseInfo
{
    public string LicenseId { get; set; } = string.Empty;
    public string CustomerName { get; set; } = string.Empty;
    public string LicenseType { get; set; } = string.Empty;
    public DateTime? ExpiresAt { get; set; }
    public int? DaysRemaining { get; set; }
    public int MaxRobots { get; set; }
    public List<string> Features { get; set; } = new();
}
