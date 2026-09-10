using EList.Models.Accounts;
using EList.Models.Person;

namespace EList.Models.Organizations
{
    /// <summary>
    /// Кандидат в менеджеры из графа подписок текущего пользователя.
    /// </summary>
    public class OrganizationManagerCandidate
    {
        /// <summary>
        /// Идентификатор аккаунта
        /// </summary>
        public Guid AccountId { get; set; }

        /// <summary>
        /// Публичные данные аккаунта
        /// </summary>
        public AccountPublicData? Account { get; set; }

        /// <summary>
        /// Персональные данные
        /// </summary>
        public PersonInfo? PersonInfo { get; set; }
    }
}
