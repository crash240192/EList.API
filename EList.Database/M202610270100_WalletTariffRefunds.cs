using FluentMigrator;

namespace EList.Database
{
    [Migration(22, "wallet-tariff-refunds")]
    public class M202610270100_WalletTariffRefunds : Migration
    {
        public override void Down() { }

        public override void Up() => Execute.EmbeddedScript("M202610270100_wallet_tariff_refunds.sql");
    }
}
