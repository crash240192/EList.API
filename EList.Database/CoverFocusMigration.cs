using FluentMigrator;

namespace EList.Database
{
    [Migration(5, "cover-focus")]
    public class CoverFocusMigration : Migration
    {
        public override void Down() { }

        public override void Up() => Execute.EmbeddedScript("CoverFocus.sql");
    }
}