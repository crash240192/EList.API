using EList.Models.Events;
namespace EList.Models.Media
{
    public class EventAlbumsContainer
    {
        public Event Event { get; set; }
        public List<MediaAlbum> Albums { get; set; }
    }
}
