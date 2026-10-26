using EList.Models.Enums;

namespace EList.Models.Wallets
{
    /// <summary>
    /// Платёж тарифного контура: TopUp (legacy) или покупка/продление/upgrade периода.
    /// Не билетные деньги, не сплит.
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
        /// <summary>Баланс сразу после зачисления (до возможного автосписания тарифа).</summary>
        public double? BalanceAfter { get; set; }

        /// <summary>Тариф покупки (null для legacy TopUp).</summary>
        public Guid? TariffId { get; set; }

        public WalletTariffPurchaseKind PurchaseKind { get; set; } = WalletTariffPurchaseKind.TopUp;

        /// <summary>Учтённый credit (остаток периода + баланс) при Purchase/Upgrade.</summary>
        public decimal CreditApplied { get; set; }

        public DateTimeOffset? PeriodStart { get; set; }
        public DateTimeOffset? PeriodEnd { get; set; }
    }
}
