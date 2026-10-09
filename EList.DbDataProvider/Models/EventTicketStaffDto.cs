using LinqToDB.Mapping;

namespace EList.DbDataProvider.Models
{
    [Table("event_ticket_staff")]
    public class EventTicketStaffDto
    {
        [Column("id"), PrimaryKey, NotNull]
        public Guid Id { get; set; }

        [Column("event_id"), NotNull]
        public Guid EventId { get; set; }

        [Column("account_id"), NotNull]
        public Guid AccountId { get; set; }

        [Column("can_check_in"), NotNull]
        public bool CanCheckIn { get; set; }

        [Column("can_view_stats"), NotNull]
        public bool CanViewStats { get; set; }

        [Column("create_date"), NotNull]
        public DateTimeOffset CreateDate { get; set; }

        [Column("update_date")]
        public DateTimeOffset? UpdateDate { get; set; }
    }
}