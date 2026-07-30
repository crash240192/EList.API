using EList.Models.Accounts;
using EList.Models.Enums;
using EList.Models.Person;

namespace EList.Models.Organizations
{
    /// <summary>
    /// Ответ API с данными участника организации
    /// </summary>
    public class OrganizationMemberResponse
    {
        /// <summary>
        /// Идентификатор записи
        /// </summary>
        public Guid Id { get; set; }

        /// <summary>
        /// Идентификатор аккаунта
        /// </summary>
        public Guid AccountId { get; set; }

        /// <summary>
        /// Идентификатор организации
        /// </summary>
        public Guid OrganizationId { get; set; }

        /// <summary>
        /// Роль
        /// </summary>
        public OrganizationMemberRole Role { get; set; }

        /// <summary>
        /// Флаг активности участника
        /// </summary>
        public bool Active { get; set; }

        /// <summary>
        /// Дата вступления
        /// </summary>
        public DateTimeOffset JoinedAt { get; set; }

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
