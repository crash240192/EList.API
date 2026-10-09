using FluentMigrator;

namespace EList.Database
{
    [Migration(13, "ticket-taker-and-event-staff")]
    public class M202610092100_TicketTakerAndEventStaff : Migration
    {
        public override void Down() { }

        public override void Up() => Execute.EmbeddedScript("M202610092100_ticket_taker_and_event_staff.sql");
    }
}