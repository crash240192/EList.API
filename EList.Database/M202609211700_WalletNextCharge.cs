using FluentMigrator;

namespace EList.Database
{
    [Migration(9, "wallet-next-charge")]
    public class M202609211700_WalletNextCharge : Migration
    {
        public override void Down() { }

        public override void Up() => Execute.EmbeddedScript("M202609211700_wallet_next_charge.sql");
    }
}
