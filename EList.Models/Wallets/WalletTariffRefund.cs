using EList.Models.Enums;

namespace EList.Models.Wallets
{
    /// <summary>Возврат средств за отмену PeriodPurchase-тарифа.</summary>
    public class WalletTariffRefund
    {
        public Guid Id { get; set; }
        public Guid WalletId { get; set; }
        public Guid DepositId { get; set; }
        public decimal Amount { get; set; }
        public string Currency { get; set; } = "RUB";
        public RefundStatus Status { get; set; }
        public string? Reason { get; set; }
        public string? ProviderRefundId { get; set; }
        public string? IdempotencyKey { get; set; }
        public decimal RemainingDays { get; set; }
        public int PeriodDays { get; set; }
        public DateTimeOffset CreateDate { get; set; }
        public DateTimeOffset? SucceededAt { get; set; }
    }

    public class WalletTariffRefundResponse
    {
        public Guid Id { get; set; }
        public Guid WalletId { get; set; }
        public Guid DepositId { get; set; }
        public decimal Amount { get; set; }
        public string Currency { get; set; } = "RUB";
        public RefundStatus Status { get; set; }
        public string? Reason { get; set; }
        public string? ProviderRefundId { get; set; }
        public decimal RemainingDays { get; set; }
        public int PeriodDays { get; set; }
        public DateTimeOffset CreateDate { get; set; }
        public DateTimeOffset? SucceededAt { get; set; }
    }
}
