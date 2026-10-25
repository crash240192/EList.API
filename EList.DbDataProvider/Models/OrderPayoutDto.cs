using EList.DbDataProvider.Models.Enums;
using LinqToDB;
using LinqToDB.Mapping;

namespace EList.DbDataProvider.Models
{
    [Table("order_payouts")]
    public class OrderPayoutDto
    {
        [Column("id"), PrimaryKey]
        public Guid Id { get; set; }

        [Column("order_id")]
        public Guid OrderId { get; set; }

        [Column("kind", DataType = DataType.Enum)]
        public OrderPayoutKind Kind { get; set; }

        [Column("amount")]
        public decimal Amount { get; set; }

        [Column("currency")]
        public string Currency { get; set; } = "RUB";

        [Column("provider", DataType = DataType.Enum)]
        public PaymentProvider Provider { get; set; }

        [Column("provider_deal_id")]
        public string? ProviderDealId { get; set; }

        [Column("partner_id")]
        public string? PartnerId { get; set; }

        [Column("provider_payment_id")]
        public string? ProviderPaymentId { get; set; }

        [Column("provider_order_id")]
        public string? ProviderOrderId { get; set; }

        [Column("final_payout")]
        public bool FinalPayout { get; set; }

        [Column("status", DataType = DataType.Enum)]
        public OrderPayoutStatus Status { get; set; }

        [Column("error_message")]
        public string? ErrorMessage { get; set; }

        [Column("create_date")]
        public DateTimeOffset CreateDate { get; set; }

        [Column("updated_at")]
        public DateTimeOffset UpdatedAt { get; set; }

        [Column("completed_at")]
        public DateTimeOffset? CompletedAt { get; set; }
    }
}
