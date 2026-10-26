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

        /// <summary>
        /// Отмена неоплаченного платежа (Pending/Authorized) у провайдера.
        /// Для T-Bank — Cancel без частичной суммы; для stub — локальный статус Canceled.
        /// </summary>
        Task CancelPaymentAsync(string providerPaymentId);

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

        /// <summary>Email покупателя для Init.Receipt (обязателен Email или Phone при кассе на терминале).</summary>
        public string? BuyerEmail { get; set; }

        /// <summary>Телефон покупателя для Init.Receipt (+7…).</summary>
        public string? BuyerPhone { get; set; }

        /// <summary>Организация-продавец (билеты). Null для пополнения кошелька.</summary>
        public Guid? SellerOrganizationId { get; set; }

        /// <summary>
        /// ShopCode / ProviderSellerId продавца.
        /// Для Deal NN — признак, что билетный Init создаёт сделку (A2C позже по этому коду).
        /// </summary>
        public string? SellerShopCode { get; set; }

        /// <summary>Доля продавца (руб). Учёт в заказе; A2C payout в P3.</summary>
        public decimal? AmountSeller { get; set; }

        /// <summary>Комиссия площадки (руб). Остаток сделки при close/FinalPayout.</summary>
        public decimal? AmountCommission { get; set; }
    }

    public class PaymentCreationResult
    {
        public string ProviderPaymentId { get; set; }
        public string? ConfirmationUrl { get; set; }
        public PaymentProviderStatus Status { get; set; }

        /// <summary>DealId, если банк вернул сразу (обычно приходит в webhook как SpAccumulationId).</summary>
        public string? ProviderDealId { get; set; }
    }

    public class PaymentStatusInfo
    {
        public string ProviderPaymentId { get; set; }
        public PaymentProviderStatus Status { get; set; }
        public decimal? Amount { get; set; }
        public string? Currency { get; set; }
        public string? ProviderDealId { get; set; }
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
