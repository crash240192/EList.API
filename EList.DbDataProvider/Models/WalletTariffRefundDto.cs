using EList.DbDataProvider.Models.Enums;
using LinqToDB.Mapping;

namespace EList.DbDataProvider.Models
{
    [Table("wallet_tariff_refunds")]
    public class WalletTariffRefundDto
    {
        [Column("id"), PrimaryKey, Identity]
        public Guid Id { get; set; }

        [Column("wallet_id")]
        public Guid WalletId { get; set; }

        [Column("deposit_id")]
        public Guid DepositId { get; set; }

        [Column("amount")]
        public decimal Amount { get; set; }

        [Column("currency")]
        public string Currency { get; set; } = "RUB";

        [Column("status", DataType = LinqToDB.DataType.Enum)]
        public RefundStatus Status { get; set; }

        [Column("reason")]
        public string? Reason { get; set; }

        [Column("provider_refund_id")]
        public string? ProviderRefundId { get; set; }

        [Column("idempotency_key")]
        public string? IdempotencyKey { get; set; }

        [Column("remaining_days")]
        public decimal RemainingDays { get; set; }

        [Column("period_days")]
        public int PeriodDays { get; set; }

        [Column("create_date")]
        public DateTimeOffset CreateDate { get; set; }

        [Column("succeeded_at")]
        public DateTimeOffset? SucceededAt { get; set; }
    }
}