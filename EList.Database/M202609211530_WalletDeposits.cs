using FluentMigrator;

namespace EList.Database
{
    [Migration(8, "wallet-deposits")]
    public class M202609211530_WalletDeposits : Migration
    {
        public override void Down() { }

        public override void Up() => Execute.EmbeddedScript("M202609211530_wallet_deposits.sql");
    }
}
