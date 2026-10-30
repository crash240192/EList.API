using FluentMigrator;

namespace EList.Database
{
    [Migration(24, "person-info-about")]
    public class M202610302200_PersonInfoAbout : Migration
    {
        public override void Down() { }

        public override void Up() => Execute.EmbeddedScript("M202610302200_person_info_about.sql");
    }
}
