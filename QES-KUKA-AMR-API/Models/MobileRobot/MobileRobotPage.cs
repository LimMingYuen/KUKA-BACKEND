namespace QES_KUKA_AMR_API.Models.MobileRobot;

public class MobileRobotPage
{
    public int TotalPages { get; set; }
    public int TotalRecords { get; set; }
    public int Page { get; set; }
    public int Size { get; set; }
    public List<MobileRobotDto> Content { get; set; } = new();
}
