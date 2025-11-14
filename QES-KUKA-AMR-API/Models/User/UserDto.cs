namespace QES_KUKA_AMR_API.Models.User;

public class UserDto
{
    public int id { get; set; }
    public string createTime { get; set; } = string.Empty;
    public string createBy { get; set; } = string.Empty;
    public string createApp { get; set; } = string.Empty;
    public string lastUpdateTime { get; set; } = string.Empty;
    public string lastUpdateBy { get; set; } = string.Empty;
    public string lastUpdateApp { get; set; } = string.Empty;
    public string username { get; set; } = String.Empty;
    public string nickname { get; set; } = String.Empty;
    public int isSuperAdmin { get; set; } = 0;
    public List<Role> roles { get; set; } = new List<Role>();
}

public class Role
{
    public int id { get; set; }
    public string name { get; set; } = String.Empty;
    public string roleCode { get; set; } = String.Empty;
    public string? isProtected { get; set; }
}
