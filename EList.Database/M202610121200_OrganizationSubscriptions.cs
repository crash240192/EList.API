using FluentMigrator;

namespace EList.Database
{
    [Migration(14, "organization-subscriptions")]
    public class M202610121200_OrganizationSubscriptions : Migration
    {
        public override void Down() { }

        public override void Up() => Execute.EmbeddedScript("M202610121200_organization_subscriptions.sql");
    }
}