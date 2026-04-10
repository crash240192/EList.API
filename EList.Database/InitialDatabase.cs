using FluentMigrator;

namespace EList.Database
{
    [Migration(1, "1.0.0.0")]
    public class InitialDatabase : Migration
    {
        public override void Down()
        {
        }

        public override void Up()
        {
            Execute.EmbeddedScript("InitialDatabase.sql");
        }
    }
}
