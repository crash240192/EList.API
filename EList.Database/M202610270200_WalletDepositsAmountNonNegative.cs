using FluentMigrator;

namespace EList.Database
{
    [Migration(23, "wallet-deposits-amount-nonnegative")]
    public class M202610270200_WalletDepositsAmountNonNegative : Migration
    {
        public override void Down() { }

        public override void Up() =>
            Execute.EmbeddedScript("M202610270200_wallet_deposits_amount_nonnegative.sql");
    }
}
