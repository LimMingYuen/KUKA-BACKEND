namespace QES_KUKA_AMR_API.Options;

public class MobileRobotServiceOptions
{
    public const string SectionName = "MobileRobotService";

    public string MobileRobotListUrl { get; set; } = string.Empty;

    public string RealtimeInfoUrl { get; set; } = string.Empty;
}
