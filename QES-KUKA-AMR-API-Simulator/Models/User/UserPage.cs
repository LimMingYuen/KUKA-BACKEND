using QES_KUKA_AMR_API_Simulator.Models.MobileRobot;

namespace QES_KUKA_AMR_API_Simulator.Models.User;

public class UserPage
{
    public int TotalPages { get; set; }
    public int TotalRecords { get; set; }
    public int Page {  get; set; }
    public int Size { get; set; }
    public List<UserDto> Content { get; set; } = new();
}
