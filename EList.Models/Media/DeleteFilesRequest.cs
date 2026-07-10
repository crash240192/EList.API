namespace EList.Models.Media
{
    public class DeleteFilesRequest
    {
        public List<Guid> FileIds { get; set; }
        public Guid AlbumId { get; set; }
    }
}
