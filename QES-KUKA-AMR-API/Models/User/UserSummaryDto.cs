namespace QES_KUKA_AMR_API.Models.User;

public class UserSummaryDto
{
    public string UserName { get; set; } = String.Empty;
    public List<string> rolename { get; set; } = new List<string>();
    public string lastUpdateTime { get; set; } = String.Empty;
}
