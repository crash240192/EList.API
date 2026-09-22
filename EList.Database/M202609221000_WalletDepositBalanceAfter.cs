using FluentMigrator;

namespace EList.Database
{
    [Migration(10, "wallet-deposit-balance-after")]
    public class M202609221000_WalletDepositBalanceAfter : Migration
    {
        public override void Down() { }

        public override void Up() => Execute.EmbeddedScript("M202609221000_wallet_deposit_balance_after.sql");
    }
}
