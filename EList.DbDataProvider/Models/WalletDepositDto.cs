using EList.DbDataProvider.Models.Enums;
using LinqToDB;
using LinqToDB.Mapping;

namespace EList.DbDataProvider.Models
{
    [Table("wallet_deposits")]
    public class WalletDepositDto
    {
        [Column("id"), PrimaryKey, Identity]
        public Guid Id { get; set; }

        [Column("wallet_id")]
        public Guid WalletId { get; set; }

        [Column("amount")]
        public decimal Amount { get; set; }

        [Column("currency")]
        public string Currency { get; set; }

        [Column("status", DataType = DataType.Enum)]
        public WalletDepositStatus Status { get; set; }

        [Column("provider", DataType = DataType.Enum)]
        public PaymentProvider? Provider { get; set; }

        [Column("provider_payment_id")]
        public string? ProviderPaymentId { get; set; }

        [Column("idempotency_key")]
        public string? IdempotencyKey { get; set; }

        [Column("create_date")]
        public DateTimeOffset CreateDate { get; set; }

        [Column("paid_at")]
        public DateTimeOffset? PaidAt { get; set; }

        /// <summary>Баланс кошелька сразу после зачисления (до возможного автосписания тарифа).</summary>
        [Column("balance_after")]
        public double? BalanceAfter { get; set; }

        [Column("tariff_id")]
        public Guid? TariffId { get; set; }

        /// <summary>0=TopUp, 1=Purchase, 2=Extend, 3=Upgrade.</summary>
        [Column("purchase_kind")]
        public short PurchaseKind { get; set; }

        [Column("credit_applied")]
        public decimal CreditApplied { get; set; }

        [Column("period_start")]
        public DateTimeOffset? PeriodStart { get; set; }

        [Column("period_end")]
        public DateTimeOffset? PeriodEnd { get; set; }

        [Association(ThisKey = nameof(WalletId), OtherKey = nameof(WalletDto.Id))]
        public WalletDto Wallet { get; set; }
    }
}
