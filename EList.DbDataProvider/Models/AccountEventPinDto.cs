using LinqToDB.Mapping;

namespace EList.DbDataProvider.Models
{
    [Table("account_event_pins")]
    public class AccountEventPinDto
    {
        [Column("account_id"), PrimaryKey(1)]
        public Guid AccountId { get; set; }

        [Column("event_id"), PrimaryKey(2)]
        public Guid EventId { get; set; }

        [Column("pinned_at")]
        public DateTimeOffset PinnedAt { get; set; }

        [Column("sort_order")]
        public int SortOrder { get; set; }
    }
}
