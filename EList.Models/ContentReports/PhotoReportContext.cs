namespace EList.Models.ContentReports
{
    public class PhotoReportContext
    {
        public Guid FileId { get; set; }
        public Guid? AlbumId { get; set; }
        public Guid? EventId { get; set; }
        public Guid? AccountId { get; set; }
        public Guid? OrganizationId { get; set; }
        public string Kind { get; set; } = string.Empty;

        public bool IsEventScoped => EventId != null;
    }
}
