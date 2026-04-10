using LinqToDB.Mapping;

namespace EList.DbDataProvider.Models
{
    [Table("public.event_type_rls")]
    public class EventTypeRelationDto
    {
        [Column("id"), PrimaryKey, Identity]
        public Guid Id { get; set; }

        [Column("event_id")]
        public Guid EventId { get; set; }

        [Column("event_type_id")]
        public Guid EventTypeId { get; set; }

        [Association(ThisKey = nameof(EventId), OtherKey = nameof(EventDto.Id))]
        public EventDto Event { get; set; }

        [Association(ThisKey = nameof(EventTypeId), OtherKey = nameof(EventTypeDto.Id))]
        public EventTypeDto Type { get; set; }
    }
}
