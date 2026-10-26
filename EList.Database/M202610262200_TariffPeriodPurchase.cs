using FluentMigrator;

namespace EList.Database
{
    [Migration(20, "tariff-period-purchase")]
    public class M202610262200_TariffPeriodPurchase : Migration
    {
        public override void Down() { }

        public override void Up() => Execute.EmbeddedScript("M202610262200_tariff_period_purchase.sql");
    }
}
