using EList.DbDataProvider.Models.Enums;
using LinqToDB;
using LinqToDB.Mapping;

namespace EList.DbDataProvider.Models
{
    [Table("refunds")]
    public class RefundDto
    {
        [Column("id"), PrimaryKey, Identity]
        public Guid Id { get; set; }

        [Column("order_id")]
        public Guid OrderId { get; set; }

        [Column("amount")]
        public decimal Amount { get; set; }

        [Column("reason")]
        public string? Reason { get; set; }

        [Column("provider_refund_id")]
        public string? ProviderRefundId { get; set; }

        [Column("status", DataType = DataType.Enum)]
        public RefundStatus Status { get; set; }

        [Column("create_date")]
        public DateTimeOffset CreateDate { get; set; }


        [Association(ThisKey = nameof(OrderId), OtherKey = nameof(OrderDto.Id))]
        public OrderDto Order { get; set; }
    }
}
