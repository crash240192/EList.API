using EList.DbDataProvider.Models.Enums;
using LinqToDB;
using LinqToDB.Mapping;

namespace EList.DbDataProvider.Models
{
    [Table("orders")]
    public class OrderDto
    {
        [Column("id"), PrimaryKey, Identity]
        public Guid Id { get; set; }

        [Column("event_id")]
        public Guid EventId { get; set; }

        [Column("buyer_account_id")]
        public Guid BuyerAccountId { get; set; }

        [Column("seller_organization_id")]
        public Guid SellerOrganizationId { get; set; }

        [Column("quantity")]
        public int Quantity { get; set; }

        [Column("amount_total")]
        public decimal AmountTotal { get; set; }

        [Column("amount_seller")]
        public decimal AmountSeller { get; set; }

        [Column("amount_commission")]
        public decimal AmountCommission { get; set; }

        [Column("currency")]
        public string Currency { get; set; }

        [Column("status", DataType = DataType.Enum)]
        public OrderStatus Status { get; set; }

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


        [Association(ThisKey = nameof(EventId), OtherKey = nameof(EventDto.Id))]
        public EventDto Event { get; set; }

        [Association(ThisKey = nameof(BuyerAccountId), OtherKey = nameof(AccountDto.Id))]
        public AccountDto BuyerAccount { get; set; }

        [Association(ThisKey = nameof(SellerOrganizationId), OtherKey = nameof(OrganizationDto.Id))]
        public OrganizationDto SellerOrganization { get; set; }

        [Association(ThisKey = nameof(Id), OtherKey = nameof(TicketDto.OrderId))]
        public List<TicketDto> Tickets { get; set; }

        [Association(ThisKey = nameof(Id), OtherKey = nameof(RefundDto.OrderId))]
        public List<RefundDto> Refunds { get; set; }
    }
}
