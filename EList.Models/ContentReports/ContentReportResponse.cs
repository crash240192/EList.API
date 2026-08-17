using EList.Models.Accounts;
using EList.Models.Enums;
using EList.Models.Events;
using EList.Models.Conversations;

namespace EList.Models.ContentReports
{
    public class ContentReportResponse
    {
        public Guid Id { get; set; }
        public Guid ReporterAccountId { get; set; }
        public ReportTargetType TargetType { get; set; }
        public Guid TargetId { get; set; }
        public Guid? EventId { get; set; }
        public Guid? MessageId { get; set; }
        public Guid? ConversationId { get; set; }
        public Guid? FileId { get; set; }
        public Guid? AlbumId { get; set; }
        public Guid? ReportedAccountId { get; set; }
        public Guid? OrganizationId { get; set; }
        public Guid? EventOrganizatorId { get; set; }
        public Guid ReasonId { get; set; }
        public string? Comment { get; set; }
        public string? TargetSnapshot { get; set; }
        public ReportStatus Status { get; set; }
        public ReportStatus? OrganizerStatus { get; set; }
        public ReportStatus? PlatformStatus { get; set; }
        public Guid? AssignedTo { get; set; }
        public ReportResolutionAction? ResolutionAction { get; set; }
        public string? ResolutionComment { get; set; }
        public Guid? ResolvedBy { get; set; }
        public DateTimeOffset? ResolvedAt { get; set; }
        public DateTimeOffset CreatedAt { get; set; }
        public DateTimeOffset UpdatedAt { get; set; }

        public ReportReason? Reason { get; set; }
        public AccountPublicData? Reporter { get; set; }
        public AccountPublicData? AssignedToAccount { get; set; }
        public AccountPublicData? ResolvedByAccount { get; set; }
        public EventShort? Event { get; set; }
        public Message? Message { get; set; }
        public List<ContentReportAction>? Actions { get; set; }
    }
}
