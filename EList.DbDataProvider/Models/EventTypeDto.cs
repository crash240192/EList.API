using LinqToDB.Mapping;

namespace EList.DbDataProvider.Models
{
    [Table("public.event_types")]
    public class EventTypeDto
    {
        [Column("id"), PrimaryKey, Identity]
        public Guid Id { get; set; }

        [Column("name_path")]
        public string NamePath { get; set; }

        [Column("description")]
        public string Description { get; set; }

        [Column("ico")]
        public byte[] Ico { get; set; }

        [Column("category_id")]
        public Guid EventCategoryId { get; set; }

        [Association(ThisKey = nameof(EventCategoryId), OtherKey = nameof(EventCategoryDto.Id))]
        public EventCategoryDto EventCategory { get; set; }

        [Association(ThisKey = nameof(Id), OtherKey = nameof(EventTypeRelationDto.EventTypeId))]
        public List<EventTypeRelationDto> Relations { get; set; }
    }
}
