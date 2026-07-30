using EList.Models.Enums;

namespace EList.Models.Organizations
{
    /// <summary>
    /// Ответ API с платёжными реквизитами организации
    /// </summary>
    public class OrganizationPayoutResponse
    {
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
        /// Статус онбординга у провайдера
        /// </summary>
        public ProviderOnboardingStatus OnboardingStatus { get; set; }
    }
}
