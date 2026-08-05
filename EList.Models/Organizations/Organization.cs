using EList.Models.Enums;
using EList.Models.Wallets;

namespace EList.Models.Organizations
{
    /// <summary>
    /// Организация
    /// </summary>
    public class Organization
    {
        /// <summary>
        /// Идентификатор
        /// </summary>
        public Guid Id { get; set; }

        /// <summary>
        /// Флаг активности
        /// </summary>
        public bool Active { get; set; }

        /// <summary>
        /// Название
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Описание
        /// </summary>
        public string? Description { get; set; }

        /// <summary>
        /// Адрес
        /// </summary>
        public string? Address { get; set; }

        /// <summary>
        /// Широта
        /// </summary>
        public double? Latitude { get; set; }

        /// <summary>
        /// Долгота
        /// </summary>
        public double? Longitude { get; set; }

        /// <summary>
        /// Идентификатор кошелька
        /// </summary>
        public Guid? WalletId { get; set; }

        /// <summary>
        /// Идентификатор аккаунта создателя
        /// </summary>
        public Guid? CreatedByAccountId { get; set; }

        /// <summary>
        /// Статус верификации
        /// </summary>
        public OrganizationVerificationStatus VerificationStatus { get; set; }

        /// <summary>
        /// Причина отклонения верификации
        /// </summary>
        public string? VerificationRejectReason { get; set; }

        /// <summary>
        /// Разрешена ли продажа билетов через сервис
        /// </summary>
        public bool CanSellTickets { get; set; }

        /// <summary>
        /// Дата создания
        /// </summary>
        public DateTimeOffset CreateDate { get; set; }

        /// <summary>
        /// Дата последнего обновления
        /// </summary>
        public DateTimeOffset UpdateDate { get; set; }

        /// <summary>
        /// Кошелёк организации
        /// </summary>
        public Wallet? Wallet { get; set; }

        /// <summary>
        /// Участники организации
        /// </summary>
        public List<OrganizationMember>? Members { get; set; }

        /// <summary>
        /// Юридические реквизиты
        /// </summary>
        public OrganizationLegal? Legal { get; set; }

        /// <summary>
        /// Платёжные реквизиты
        /// </summary>
        public OrganizationPayout? Payout { get; set; }
    }
}
