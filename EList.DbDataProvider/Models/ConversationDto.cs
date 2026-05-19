using LinqToDB.Mapping;

namespace EList.DbDataProvider.Models
{
    [Table("conversation")]
    public class ConversationDto
    {
        [Column("id"), PrimaryKey, Identity]
        public Guid Id { get; set; }

        [Column("name")]
        public string Name { get; set; }

        [Column("event_id")]
        public Guid? EventId { get; set; }

        [Column("create_date")]
        public DateTimeOffset CreateDate { get; set; }

        [Column("update_date")]
        public DateTimeOffset UpdateDate { get; set; }
    }
}
