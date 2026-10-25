using FluentMigrator;

namespace EList.Database
{
    [Migration(18, "order-deal-closed-at")]
    public class M202610252251_OrderDealClosedAt : Migration
    {
        public override void Down() { }

        public override void Up() => Execute.EmbeddedScript("M202610252251_order_deal_closed_at.sql");
    }
}
