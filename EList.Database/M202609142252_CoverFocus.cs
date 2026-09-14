using FluentMigrator;

namespace EList.Database
{
    [Migration(5, "cover-focus")]
    public class M202609142252_CoverFocus : Migration
    {
        public override void Down() { }

        public override void Up() => Execute.EmbeddedScript("M202609142252_cover_focus.sql");
    }
}
