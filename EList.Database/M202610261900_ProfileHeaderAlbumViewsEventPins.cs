using FluentMigrator;

namespace EList.Database
{
    [Migration(20, "profile-header-album-views-event-pins")]
    public class M202610261900_ProfileHeaderAlbumViewsEventPins : Migration
    {
        public override void Down() { }

        public override void Up() => Execute.EmbeddedScript("M202610261900_profile_header_album_views_event_pins.sql");
    }
}
