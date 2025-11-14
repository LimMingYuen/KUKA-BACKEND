namespace QES_KUKA_AMR_API.Options;

public class AmrServiceOptions
{
    public const string SectionName = "AmrServiceOptions";

    public string OperationFeedbackUrl { get; set; } = string.Empty;
    public string RobotQueryUrl { get; set; } = string.Empty;
}
