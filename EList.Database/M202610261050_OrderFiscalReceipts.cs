using FluentMigrator;

namespace EList.Database
{
    [Migration(19, "order-fiscal-receipts")]
    public class M202610261050_OrderFiscalReceipts : Migration
    {
        public override void Down() { }

        public override void Up() => Execute.EmbeddedScript("M202610261050_order_fiscal_receipts.sql");
    }
}
