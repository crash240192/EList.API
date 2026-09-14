using FluentMigrator;

namespace EList.Database
{
    [Migration(3, "soft-delete-catalogs")]
    public class M202609031136_SoftDeleteCatalogs : Migration
    {
        public override void Down() { }

        public override void Up() => Execute.EmbeddedScript("M202609031136_soft_delete_catalogs.sql");
    }
}
