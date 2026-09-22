using EList.Models.Enums;

namespace EList.Services.Interfaces
{
    /// <summary>
    /// Онбординг продавца (организации) в платёжной системе (ЮKassa / stub).
    /// Подключается через DI рядом с <see cref="IPaymentProvider"/>.
    /// </summary>
    public interface ISellerOnboardingProvider
    {
        PaymentProvider Kind { get; }

        /// <summary>Stub завершает сразу; реальная ЮKassa может вернуть ConfirmationUrl.</summary>
        bool CompletesImmediately { get; }

        Task<SellerOnboardingStartResult> StartAsync(SellerOnboardingRequest request);

        Task<SellerOnboardingStatusInfo> GetStatusAsync(string providerSellerId);
    }

    public class SellerOnboardingRequest
    {
        public Guid OrganizationId { get; set; }
        public string? Inn { get; set; }
        public string? Ogrn { get; set; }
        public string? LegalName { get; set; }
        public string? LegalAddress { get; set; }
        public string? HeadName { get; set; }
        public string? BankAccount { get; set; }
        public string? Bik { get; set; }
        public string? BankName { get; set; }
        public string? Email { get; set; }
        public string? Phone { get; set; }
        public string? ReturnUrl { get; set; }
    }

    public class SellerOnboardingStartResult
    {
        public string ProviderSellerId { get; set; }
        public string? ConfirmationUrl { get; set; }
        public ProviderOnboardingStatus Status { get; set; }
    }

    public class SellerOnboardingStatusInfo
    {
        public string ProviderSellerId { get; set; }
        public ProviderOnboardingStatus Status { get; set; }
    }
}
