using LinqToDB.Mapping;

namespace EList.DbDataProvider.Models
{
    [Table("event_types")]
    public class EventTypeDto
    {
        [Column("id"), PrimaryKey, Identity]
        public Guid Id { get; set; }

        [Column("localization_path")]
        public string LocalizationPath { get; set; }

        [Column("name")]
        public string Name { get; set; }

        [Column("description")]
        public string Description { get; set; }

        [Column("ico")]
        public string Ico { get; set; }

        [Column("category_id")]
        public Guid EventCategoryId { get; set; }

        [Association(ThisKey = nameof(EventCategoryId), OtherKey = nameof(EventCategoryDto.Id))]
        public EventCategoryDto EventCategory { get; set; }

        [Association(ThisKey = nameof(Id), OtherKey = nameof(EventTypeRelationDto.EventTypeId))]
        public List<EventTypeRelationDto> Relations { get; set; }
    }
}
