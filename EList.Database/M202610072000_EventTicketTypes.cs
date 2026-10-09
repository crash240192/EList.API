using FluentMigrator;

namespace EList.Database
{
    [Migration(11, "event-ticket-types")]
    public class M202610072000_EventTicketTypes : Migration
    {
        public override void Down() { }

        public override void Up() => Execute.EmbeddedScript("M202610072000_event_ticket_types.sql");
    }
}
