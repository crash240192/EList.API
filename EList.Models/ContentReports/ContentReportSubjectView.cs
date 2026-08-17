using EList.Models.Enums;

namespace EList.Models.ContentReports
{
    /// <summary>
    /// Карточка жалобы для адресата (на кого пожаловались).
    /// Без личности жалобщика, внутренних очередей и аудита модераторов.
    /// </summary>
    public class ContentReportSubjectView
    {
        public Guid Id { get; set; }
        public ReportTargetType TargetType { get; set; }
        public Guid TargetId { get; set; }
        public Guid? EventId { get; set; }
        public Guid? MessageId { get; set; }
        public Guid? FileId { get; set; }
        public Guid? AlbumId { get; set; }
        public Guid? OrganizationId { get; set; }
        public Guid? EventOrganizatorId { get; set; }
        public string? TargetSnapshot { get; set; }
        public ReportStatus Status { get; set; }
        public ReportResolutionAction? ResolutionAction { get; set; }
        public string? ModeratorRemark { get; set; }
        public DateTimeOffset? ResolvedAt { get; set; }
        public DateTimeOffset CreatedAt { get; set; }
        public DateTimeOffset UpdatedAt { get; set; }
        public ReportReason? Reason { get; set; }
    }
}
