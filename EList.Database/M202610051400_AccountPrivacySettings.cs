using FluentMigrator;

namespace EList.Database
{
    [Migration(11, "account-privacy-settings")]
    public class M202610051400_AccountPrivacySettings : Migration
    {
        public override void Down() { }

        public override void Up() => Execute.EmbeddedScript("M202610051400_account_privacy_settings.sql");
    }
}
