using LinqToDB.Mapping;

namespace EList.DbDataProvider.Models
{
    [Table("public.event_categories")]
    public class EventCategoryDto
    {
        [Column("id"), PrimaryKey, Identity]
        public Guid Id { get; set; }

        [Column("name_path")]
        public string NamePath { get; set; }

        [Column("ico")]
        public byte[] Ico { get; set; }

        [Column("description")]
        public string Description { get; set; }
    }
}
