using FluentMigrator;

namespace EList.Database
{
    [Migration(6, "message-files-system-album")]
    public class M202609152340_MessageFilesSystemAlbum : Migration
    {
        public override void Down() { }

        public override void Up() => Execute.EmbeddedScript("M202609152340_message_files_system_album.sql");
    }
}
