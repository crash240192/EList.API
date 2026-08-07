using EList.Models.Accounts;
using EList.Models.Enums;

namespace EList.Models.BugReports
{
    public class BugReportResponse
    {
        public Guid Id { get; set; }
        public Guid ReporterAccountId { get; set; }
        public Guid CategoryId { get; set; }
        public string Description { get; set; }
        public BugReportStatus Status { get; set; }
        public DateTimeOffset CreateDate { get; set; }
        public DateTimeOffset UpdateDate { get; set; }

        public BugReportCategory? Category { get; set; }
        public AccountPublicData? Reporter { get; set; }
        public List<Guid> FileIds { get; set; } = new();
    }
}
