namespace QES_KUKA_AMR_API.Models.Auth;

public class VerifyAdminResponse
{
    public bool IsValid { get; set; }
    public string? AdminUsername { get; set; }
    public string? Message { get; set; }
}
