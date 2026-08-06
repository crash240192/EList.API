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

        /// <summary>
        /// Диалог виден только участникам мероприятия (администраторы/организаторы — всегда)
        /// </summary>
        [Column("participants_only_visible")]
        public bool ParticipantsOnlyVisible { get; set; }

        /// <summary>
        /// Участники могут только читать сообщения (администраторы/организаторы — всегда могут писать)
        /// </summary>
        [Column("participants_readonly")]
        public bool ParticipantsReadonly { get; set; }

        [Column("create_date")]
        public DateTimeOffset CreateDate { get; set; }

        [Column("update_date")]
        public DateTimeOffset UpdateDate { get; set; }
    }
}
