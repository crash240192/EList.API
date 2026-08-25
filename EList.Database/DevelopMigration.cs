using FluentMigrator;

namespace EList.Database
{
    [Migration(2, "develop-incremental")]
    public class DevelopMigration : Migration
    {
        public override void Down() { }

        public override void Up() => Execute.EmbeddedScript("DevelopMigration.sql");
    }
}
