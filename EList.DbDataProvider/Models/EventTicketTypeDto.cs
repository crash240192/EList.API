using LinqToDB.Mapping;

namespace EList.DbDataProvider.Models
{
    [Table("event_ticket_types")]
    public class EventTicketTypeDto
    {
        [Column("id"), PrimaryKey, Identity]
        public Guid Id { get; set; }

        [Column("event_id")]
        public Guid EventId { get; set; }

        [Column("name")]
        public string Name { get; set; } = string.Empty;

        [Column("description")]
        public string? Description { get; set; }

        [Column("price")]
        public decimal Price { get; set; }

        [Column("currency")]
        public string Currency { get; set; } = "RUB";

        [Column("capacity")]
        public int? Capacity { get; set; }

        [Column("sort_order")]
        public int SortOrder { get; set; }

        [Column("active")]
        public bool Active { get; set; } = true;

        [Column("create_date")]
        public DateTimeOffset CreateDate { get; set; }

        [Column("update_date")]
        public DateTimeOffset UpdateDate { get; set; }

        [Association(ThisKey = nameof(EventId), OtherKey = nameof(EventDto.Id))]
        public EventDto Event { get; set; }
    }
}
