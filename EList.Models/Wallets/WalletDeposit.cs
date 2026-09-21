using EList.Models.Enums;

namespace EList.Models.Wallets
{
    /// <summary>
    /// Пополнение тарифного кошелька (не билетные деньги, не сплит).
    /// </summary>
    public class WalletDeposit
    {
        public Guid Id { get; set; }
        public Guid WalletId { get; set; }
        public decimal Amount { get; set; }
        public string Currency { get; set; } = "RUB";
        public WalletDepositStatus Status { get; set; }
        public PaymentProvider? Provider { get; set; }
        public string? ProviderPaymentId { get; set; }
        public string? IdempotencyKey { get; set; }
        public DateTimeOffset CreateDate { get; set; }
        public DateTimeOffset? PaidAt { get; set; }
    }
}
