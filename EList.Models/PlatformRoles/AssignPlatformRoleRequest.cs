using EList.Models.Enums;

namespace EList.Models.PlatformRoles
{
    public class AssignPlatformRoleRequest
    {
        public Guid AccountId { get; set; }
        public PlatformRole Role { get; set; }
    }
}
