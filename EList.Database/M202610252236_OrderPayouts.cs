using FluentMigrator;

namespace EList.Database
{
    [Migration(17, "order-payouts")]
    public class M202610252236_OrderPayouts : Migration
    {
        public override void Down() { }

        public override void Up() => Execute.EmbeddedScript("M202610252236_order_payouts.sql");
    }
}
