using FluentMigrator;

namespace EList.Database
{
    [Migration(16, "order-provider-deal-id")]
    public class M202610252230_OrderProviderDealId : Migration
    {
        public override void Down() { }

        public override void Up() => Execute.EmbeddedScript("M202610252230_order_provider_deal_id.sql");
    }
}
