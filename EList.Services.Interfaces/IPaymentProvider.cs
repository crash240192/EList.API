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

        Task<RefundCreationResult> CreateRefundAsync(RefundCreationRequest request);

        /// <summary>Только для stub: имитация успешного возврата.</summary>
        Task CompleteRefundManuallyAsync(string providerRefundId);
    }

    public class PaymentCreationRequest
    {
        public Guid OrderId { get; set; }
        public Guid? WalletDepositId { get; set; }
        public decimal Amount { get; set; }
        public string Currency { get; set; } = "RUB";
        public string Description { get; set; }
        public string? ReturnUrl { get; set; }
        public string? IdempotencyKey { get; set; }
        public Guid BuyerAccountId { get; set; }
        public Guid EventId { get; set; }

        /// <summary>Организация-продавец (билеты). Null для пополнения кошелька.</summary>
        public Guid? SellerOrganizationId { get; set; }

        /// <summary>ShopCode / ProviderSellerId продавца для мультирасчётов Т-Банка.</summary>
        public string? SellerShopCode { get; set; }

        /// <summary>Доля продавца (руб). Для Init.Shops.Amount.</summary>
        public decimal? AmountSeller { get; set; }

        /// <summary>Комиссия площадки (руб). Для Init.Shops.Fee.</summary>
        public decimal? AmountCommission { get; set; }
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

    public class RefundCreationRequest
    {
        public Guid RefundId { get; set; }
        public Guid OrderId { get; set; }
        public string ProviderPaymentId { get; set; }
        public decimal Amount { get; set; }
        public string Currency { get; set; } = "RUB";
        public string? Reason { get; set; }
    }

    public class RefundCreationResult
    {
        public string ProviderRefundId { get; set; }
        public PaymentProviderStatus Status { get; set; }
    }

    public enum PaymentProviderStatus
    {
        Pending = 0,
        Succeeded = 1,
        Canceled = 2,
        Failed = 3
    }
}
