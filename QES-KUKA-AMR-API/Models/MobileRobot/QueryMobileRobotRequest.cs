namespace QES_KUKA_AMR_API.Models.MobileRobot;

public class QueryMobileRobotRequest
{
    public object Query { get; set; } = new { }; // Changed to object to accept {} or filters
    public int PageNum { get; set; } = 1;
    public int PageSize { get; set; } = 100000;
    public string OrderBy { get; set; } = "lastUpdateTime";
    public bool Asc { get; set; } = false;
}
