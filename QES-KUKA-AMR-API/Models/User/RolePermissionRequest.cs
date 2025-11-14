namespace QES_KUKA_AMR_API.Models.User
{
    public class RolePermissionRequest
    {
        public int RoleId { get; set; }
        public List<int> PageIds { get; set; } = new List<int>();
    }
}
