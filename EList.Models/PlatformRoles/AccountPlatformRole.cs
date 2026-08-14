using EList.Models.Accounts;
using EList.Models.Enums;

namespace EList.Models.PlatformRoles
{
    public class AccountPlatformRole
    {
        public Guid Id { get; set; }
        public Guid AccountId { get; set; }
        public PlatformRole Role { get; set; }
        public bool Active { get; set; }
        public DateTimeOffset AssignedAt { get; set; }
        public Guid? AssignedBy { get; set; }

        public AccountPublicData? Account { get; set; }
        public AccountPublicData? AssignedByAccount { get; set; }
    }
}
