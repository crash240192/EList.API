using FluentMigrator;

namespace EList.Database
{
    [Migration(15, "document-distribution-consent")]
    public class M202610131200_DocumentDistributionConsent : Migration
    {
        public override void Down() { }

        public override void Up() => Execute.EmbeddedScript("M202610131200_document_distribution_consent.sql");
    }
}
