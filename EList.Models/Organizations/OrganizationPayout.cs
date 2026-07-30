using EList.Models.Enums;

namespace EList.Models.Organizations
{
    /// <summary>
    /// Платёжные реквизиты организации и онбординг у провайдера
    /// </summary>
    public class OrganizationPayout
    {
        /// <summary>
        /// Идентификатор организации
        /// </summary>
        public Guid OrganizationId { get; set; }

        /// <summary>
        /// Расчётный счёт
        /// </summary>
        public string? BankAccount { get; set; }

        /// <summary>
        /// БИК
        /// </summary>
        public string? Bik { get; set; }

        /// <summary>
        /// Название банка
        /// </summary>
        public string? BankName { get; set; }

        /// <summary>
        /// Налоговый режим
        /// </summary>
        public string? TaxRegime { get; set; }

        /// <summary>
        /// Платёжный провайдер
        /// </summary>
        public PaymentProvider? Provider { get; set; }

        /// <summary>
        /// Идентификатор продавца у провайдера
        /// </summary>
        public string? ProviderSellerId { get; set; }

        /// <summary>
        /// Статус онбординга у провайдера
        /// </summary>
        public ProviderOnboardingStatus OnboardingStatus { get; set; }

        /// <summary>
        /// Кто последний обновлял реквизиты
        /// </summary>
        public Guid? UpdatedBy { get; set; }

        /// <summary>
        /// Дата последнего обновления
        /// </summary>
        public DateTimeOffset UpdateDate { get; set; }
    }
}
