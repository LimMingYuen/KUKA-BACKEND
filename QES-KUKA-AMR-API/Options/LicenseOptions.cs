namespace QES_KUKA_AMR_API.Options;

public class LicenseOptions
{
    /// <summary>
    /// Path to the machine license file
    /// </summary>
    public string LicenseFilePath { get; set; } = @"C:\ProgramData\QES-KUKA-AMR\license.lic";

    /// <summary>
    /// Directory containing robot license files (e.g., AMR001.lic, AMR002.lic)
    /// </summary>
    public string RobotLicensesPath { get; set; } = @"C:\ProgramData\QES-KUKA-AMR\RobotLicenses";

    /// <summary>
    /// RSA public key in PEM format for validating license signatures
    /// </summary>
    public string PublicKey { get; set; } = string.Empty;
}
