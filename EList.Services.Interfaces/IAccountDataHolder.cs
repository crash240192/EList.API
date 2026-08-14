using EList.Models.Accounts;
using EList.Models.Enums;
using EList.Models.Person;

namespace EList.Services.Interfaces
{
    public interface IAccountDataHolder
    {
        Guid? Token { get; set; }
        Account? Account { get; set; }
        string Jwt { get; set; }
        string ClientHash { get; set; }
        string ClientInfo { get; set; }
        PersonInfo? PersonInfo { get; set; }
        Guid? AccountId { get; }
        public bool AdultConfirmed { get; set; }

        /// <summary>
        /// Роль площадки текущего пользователя. null = обычный пользователь.
        /// </summary>
        PlatformRole? PlatformRole { get; set; }

        bool IsPlatformStaff { get; }
        bool IsPlatformModeratorOrAbove { get; }
        bool IsPlatformAdminOrAbove { get; }
        bool IsSuperuser { get; }

        string AccountNameFullString { get; }
        public int Age { get; }
    }
}
