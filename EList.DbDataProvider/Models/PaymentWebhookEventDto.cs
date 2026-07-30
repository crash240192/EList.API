using EList.DbDataProvider.Models.Enums;
using LinqToDB;
using LinqToDB.Mapping;

namespace EList.DbDataProvider.Models
{
    [Table("payment_webhook_events")]
    public class PaymentWebhookEventDto
    {
        [Column("id"), PrimaryKey, Identity]
        public Guid Id { get; set; }

        [Column("provider", DataType = DataType.Enum)]
        public PaymentProvider Provider { get; set; }

        [Column("provider_event_id")]
        public string ProviderEventId { get; set; }

        [Column("order_id")]
        public Guid? OrderId { get; set; }

        [Column("payload"), DataType("jsonb")]
        public string? Payload { get; set; }

        [Column("received_at")]
        public DateTimeOffset ReceivedAt { get; set; }

        [Column("processed_at")]
        public DateTimeOffset? ProcessedAt { get; set; }


        [Association(ThisKey = nameof(OrderId), OtherKey = nameof(OrderDto.Id))]
        public OrderDto? Order { get; set; }
    }
}
