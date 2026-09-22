using EList.Models.Enums;

namespace EList.Models.Organizations
{
    /// <summary>
    /// Запрос на старт онбординга организации в платёжной системе
    /// </summary>
    public class OrganizationProviderOnboardingRequest
    {
        /// <summary>
        /// URL возврата после прохождения кабинета провайдера (для реальной ЮKassa)
        /// </summary>
        public string? ReturnUrl { get; set; }
    }

    /// <summary>
    /// Результат старта / текущего состояния онбординга у провайдера
    /// </summary>
    public class OrganizationProviderOnboardingResponse
    {
        public PaymentProvider? Provider { get; set; }
        public string? ProviderSellerId { get; set; }
        public ProviderOnboardingStatus OnboardingStatus { get; set; }
        public string? ConfirmationUrl { get; set; }
    }
}
