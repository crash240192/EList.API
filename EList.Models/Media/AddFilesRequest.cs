namespace EList.Models.Media
{
    public class AddFilesRequest
    {
        public Guid AlbumId { get; set; }
        public List<Guid> FileIds { get; set; }
    }
}
