namespace QES_KUKA_AMR_API.Services.Licensing.Models;

public class LicenseFile
{
    public int Version { get; set; }
    public string Data { get; set; } = string.Empty;
    public string Signature { get; set; } = string.Empty;
}
