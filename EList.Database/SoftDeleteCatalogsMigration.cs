using FluentMigrator;

namespace EList.Database
{
    [Migration(3, "soft-delete-catalogs")]
    public class SoftDeleteCatalogsMigration : Migration
    {
        public override void Down() { }

        public override void Up() => Execute.EmbeddedScript("SoftDeleteCatalogs.sql");
    }
}
