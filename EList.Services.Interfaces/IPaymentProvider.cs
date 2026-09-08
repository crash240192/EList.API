using EList.Models.Enums;

namespace EList.Services.Interfaces
{
    /// <summary>
    /// Платёжный провайдер (ЮKassa / stub). Подключается через DI.
    /// </summary>
    public interface IPaymentProvider
    {
        PaymentProvider Kind { get; }

        /// <summary>Stub поддерживает ручное Complete; реальная ЮKassa — нет.</summary>
        bool SupportsManualComplete { get; }

        Task<PaymentCreationResult> CreatePaymentAsync(PaymentCreationRequest request);

        Task<PaymentStatusInfo> GetStatusAsync(string providerPaymentId);

        /// <summary>Только для stub: имитация успешной оплаты.</summary>
        Task CompleteManuallyAsync(string providerPaymentId);
    }

    public class PaymentCreationRequest
    {
        public Guid OrderId { get; set; }
        public decimal Amount { get; set; }
        public string Currency { get; set; } = "RUB";
        public string Description { get; set; }
        public string? ReturnUrl { get; set; }
        public string? IdempotencyKey { get; set; }
        public Guid BuyerAccountId { get; set; }
        public Guid EventId { get; set; }
    }

    public class PaymentCreationResult
    {
        public string ProviderPaymentId { get; set; }
        public string? ConfirmationUrl { get; set; }
        public PaymentProviderStatus Status { get; set; }
    }

    public class PaymentStatusInfo
    {
        public string ProviderPaymentId { get; set; }
        public PaymentProviderStatus Status { get; set; }
        public decimal? Amount { get; set; }
        public string? Currency { get; set; }
    }

    public enum PaymentProviderStatus
    {
        Pending = 0,
        Succeeded = 1,
        Canceled = 2,
        Failed = 3
    }
}
