namespace QES_KUKA_AMR_API_Simulator.Models.MobileRobot;

public class MobileRobotRequest
{
    public List<string> Query {  get; set; }
    public int PageNum {  get; set; }
    public int PageSize { get; set; }
    public string OrderBy { get; set; }
    public Boolean asc { get; set; }
}
