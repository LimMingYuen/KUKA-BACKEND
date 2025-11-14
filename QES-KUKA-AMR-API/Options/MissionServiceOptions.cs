namespace QES_KUKA_AMR_API.Options;

public class MissionServiceOptions
{
    public const string SectionName = "MissionService";

    public string? SubmitMissionUrl { get; set; }
    public string? MissionCancelUrl { get; set; }
    public string? JobQueryUrl { get; set; }
    public string? WorkflowQueryUrl { get; set; }
    public string? OperationFeedbackUrl { get; set; }
    public string? RobotQueryUrl { get; set; }
}
