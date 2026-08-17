namespace EList.DbDataProvider.Models
{
    public class PhotoReportContextDto
    {
        public Guid FileId { get; set; }
        public Guid? AlbumId { get; set; }
        public Guid? EventId { get; set; }
        public Guid? AccountId { get; set; }
        public Guid? OrganizationId { get; set; }
        public string Kind { get; set; } = string.Empty;
    }
}
