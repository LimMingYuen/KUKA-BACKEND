namespace QES_KUKA_AMR_API.Models.MobileRobot;

/// <summary>
/// Wrapper for external API response that has nested pageData structure
/// </summary>
public class MobileRobotPageWrapper
{
    public MobileRobotPage PageData { get; set; } = new();
    public bool LimitFlag { get; set; }
}
