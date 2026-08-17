using EList.DbDataProvider.Models.Enums;

namespace EList.DbDataProvider.Models.SearchRequests
{
    public class ContentReportsSearchRequest
    {
        public ReportTargetType? TargetType { get; set; }
        public Guid? TargetId { get; set; }
        public Guid? EventId { get; set; }
        public Guid? MessageId { get; set; }
        public Guid? FileId { get; set; }
        public Guid? ReportedAccountId { get; set; }
        public Guid? OrganizationId { get; set; }
        public Guid? ReasonId { get; set; }
        public ReportSeverity? Severity { get; set; }
        public Guid? ReporterAccountId { get; set; }
        public Guid? AssignedTo { get; set; }

        public ReportStatus? Status { get; set; }
        public ReportStatus? OrganizerStatus { get; set; }
        public ReportStatus? PlatformStatus { get; set; }

        /// <summary>
        /// Только жалобы, попадающие в очередь платформы (platform_status IS NOT NULL).
        /// </summary>
        public bool? InPlatformQueue { get; set; }

        /// <summary>
        /// Только жалобы, попадающие в очередь организаторов (organizer_status IS NOT NULL).
        /// </summary>
        public bool? InOrganizerQueue { get; set; }

        /// <summary>
        /// Только «активные» статусы: open / in_review / escalated.
        /// </summary>
        public bool? OnlyActive { get; set; }

        public int? PageIndex { get; set; }
        public int? PageSize { get; set; }
    }
}
