using EList.Models.Enums;

namespace EList.Models.BugReports
{
    public class BugReportSearchRequest
    {
        public Guid? CategoryId { get; set; }
        public BugReportStatus? Status { get; set; }
        public Guid? ReporterAccountId { get; set; }
        public string? Description { get; set; }
        public int? PageIndex { get; set; }
        public int? PageSize { get; set; }
    }
}
