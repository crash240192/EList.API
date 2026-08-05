using EList.Models.Enums;

namespace EList.Models.Organizations
{
    /// <summary>
    /// Ответ API с данными организации
    /// </summary>
    public class OrganizationResponse
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
        /// Участники организации
        /// </summary>
        public List<OrganizationMemberResponse>? Members { get; set; }

        /// <summary>
        /// Юридические реквизиты
        /// </summary>
        public OrganizationLegalResponse? Legal { get; set; }

        /// <summary>
        /// Платёжные реквизиты
        /// </summary>
        public OrganizationPayoutResponse? Payout { get; set; }
    }
}
