using EList.Models.Enums;

namespace EList.Models.ContentReports
{
    public class ContentReportsSearchRequest
    {
        public ReportTargetType? TargetType { get; set; }
        public Guid? TargetId { get; set; }
        public Guid? EventId { get; set; }
        public Guid? MessageId { get; set; }
        public Guid? ReasonId { get; set; }
        public ReportSeverity? Severity { get; set; }
        public Guid? ReporterAccountId { get; set; }
        public Guid? AssignedTo { get; set; }
        public ReportStatus? Status { get; set; }
        public ReportStatus? OrganizerStatus { get; set; }
        public ReportStatus? PlatformStatus { get; set; }
        public bool? InPlatformQueue { get; set; }
        public bool? InOrganizerQueue { get; set; }
        public bool? OnlyActive { get; set; }
        public int? PageIndex { get; set; }
        public int? PageSize { get; set; }
    }
}
