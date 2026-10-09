using FluentMigrator;

namespace EList.Database
{
    [Migration(12, "ticket-type-id-not-null")]
    public class M202610091200_TicketTypeIdNotNull : Migration
    {
        public override void Down() { }

        public override void Up() => Execute.EmbeddedScript("M202610091200_ticket_type_id_not_null.sql");
    }
}