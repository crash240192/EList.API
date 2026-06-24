namespace EList.DbDataProvider.Models
{
    public class EventAlbumsGroupDto
    {
        public EventDto Event { get; set; }
        public List<MediaAlbumDto> Albums { get; set; }
    }
}
