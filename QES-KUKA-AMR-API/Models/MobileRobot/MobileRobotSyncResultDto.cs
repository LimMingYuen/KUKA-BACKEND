namespace QES_KUKA_AMR_API.Models.MobileRobot;

public class MobileRobotSyncResultDto
{
    public int Total { get; set; }
    public int Inserted { get; set; }
    public int Updated { get; set; }

    /// <summary>
    /// List of RobotIds that were skipped because they appeared multiple times in external API
    /// </summary>
    public List<string> SkippedDuplicates { get; set; } = new();
}
