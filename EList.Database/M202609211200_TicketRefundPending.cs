using FluentMigrator;

namespace EList.Database
{
    [Migration(7, "ticket-refund-pending")]
    public class M202609211200_TicketRefundPending : Migration
    {
        public override void Down() { }

        public override void Up() => Execute.EmbeddedScript("M202609211200_ticket_refund_pending.sql");
    }
}
