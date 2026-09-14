using FluentMigrator;

namespace EList.Database
{
    [Migration(2, "develop-incremental")]
    public class M202608250914_DevelopIncremental : Migration
    {
        public override void Down() { }

        public override void Up() => Execute.EmbeddedScript("M202608250914_develop_incremental.sql");
    }
}
