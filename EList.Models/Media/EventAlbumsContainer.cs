using EList.Models.Events;
namespace EList.Models.Media
{
    public class EventAlbumsContainer
    {
        public EventShort Event { get; set; }
        public List<MediaAlbum> Albums { get; set; }
    }
}
