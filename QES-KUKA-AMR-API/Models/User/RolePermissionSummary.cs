namespace QES_KUKA_AMR_API.Models.User
{
    public class RolePermissionSummary
    {
        public int RoleId { get; set; }
        public string RoleName { get; set; } = string.Empty;
        public List<string> PageNames { get; set; } = new();
    }

    public class RolePermissionDetail
    {
        public int RoleId { get; set; }
        public string RoleName { get; set; } = string.Empty;
        public List<int> PageIds { get; set; } = new();
    }
}
