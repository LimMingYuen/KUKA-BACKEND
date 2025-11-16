namespace QES_KUKA_AMR_API.Models.Queue;

public class RobotAssignment
{
    public string RobotId { get; set; } = string.Empty;
    public int QueueItemId { get; set; }
    public double Distance { get; set; }
    public double Score { get; set; }
    public DateTime AssignedUtc { get; set; }
    public RobotPosition Position { get; set; } = null!;
}

public class RobotDistanceScore
{
    public string RobotId { get; set; } = string.Empty;
    public double Distance { get; set; }
    public int BatteryLevel { get; set; }
    public int Priority { get; set; }
    public double Score { get; set; }
    public RobotPosition Position { get; set; } = null!;
}
