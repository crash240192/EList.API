using LinqToDB.Mapping;

namespace EList.DbDataProvider.Models
{
    [Table("event_categories")]
    public class EventCategoryDto
    {
        [Column("id"), PrimaryKey, Identity]
        public Guid Id { get; set; }

        [Column("localization_path")]
        public string LocalizationPath { get; set; }

        [Column("name")]
        public string Name { get; set; }

        [Column("ico")]
        public byte[] Ico { get; set; }

        [Column("description")]
        public string Description { get; set; }

        [Column("color")]
        public string Color { get; set; }
    }
}
