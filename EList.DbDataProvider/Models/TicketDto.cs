using EList.DbDataProvider.Models.Enums;
using LinqToDB;
using LinqToDB.Mapping;

namespace EList.DbDataProvider.Models
{
    [Table("tickets")]
    public class TicketDto
    {
        [Column("id"), PrimaryKey, Identity]
        public Guid Id { get; set; }

        [Column("order_id")]
        public Guid OrderId { get; set; }

        [Column("event_id")]
        public Guid EventId { get; set; }

        [Column("holder_account_id")]
        public Guid HolderAccountId { get; set; }

        [Column("status", DataType = DataType.Enum)]
        public TicketStatus Status { get; set; }

        [Column("code")]
        public string Code { get; set; }

        [Column("issued_at")]
        public DateTimeOffset IssuedAt { get; set; }


        [Association(ThisKey = nameof(OrderId), OtherKey = nameof(OrderDto.Id))]
        public OrderDto Order { get; set; }

        [Association(ThisKey = nameof(EventId), OtherKey = nameof(EventDto.Id))]
        public EventDto Event { get; set; }

        [Association(ThisKey = nameof(HolderAccountId), OtherKey = nameof(AccountDto.Id))]
        public AccountDto HolderAccount { get; set; }
    }
}
