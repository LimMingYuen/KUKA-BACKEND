namespace QES_KUKA_AMR_API.Models.MobileRobot;

public class QueryMobileRobotRequest
{
    public List<string>? Query { get; set; } = new List<string>(); // Must be List<string> to match simulator API
    public int PageNum { get; set; } = 1;
    public int PageSize { get; set; } = 100000;
    public string OrderBy { get; set; } = "lastUpdateTime";
    public bool Asc { get; set; } = false;
}
