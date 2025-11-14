namespace QES_KUKA_AMR_API.Options;

public class LoginServiceOptions
{
    public const string SectionName = "LoginService";

    public string? LoginUrl { get; set; }
    public bool HashPassword { get; set; }
}
