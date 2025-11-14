namespace QES_KUKA_AMR_API_Simulator.Models.Login;

public class LoginUserInfo
{
    public int Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public string Nickname { get; set; } = string.Empty;
    public int IsSuperAdmin { get; set; }
    public IReadOnlyList<LoginRole> Roles { get; set; } = Array.Empty<LoginRole>();
}

public class LoginRole
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string RoleCode { get; set; } = string.Empty;
    public int IsProtected { get; set; }
}

public class LoginPermission
{
    public int Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public int Type { get; set; }
    public string PermissionGroup { get; set; } = string.Empty;
    public string PermissionClass { get; set; } = string.Empty;
    public string UiSign { get; set; } = string.Empty;
}
