using EList.Models.Enums;

namespace EList.Models.Notifications
{
    /// <summary>
    /// Payload WebSocket/БД-уведомления по жалобе на контент.
    /// </summary>
    public class ContentReportNotificationData
    {
        public Guid ReportId { get; set; }
        public ReportTargetType TargetType { get; set; }
        public Guid TargetId { get; set; }
        public Guid? EventId { get; set; }
        public Guid? OrganizationId { get; set; }
        public string? ReasonCode { get; set; }
        public string? ReasonName { get; set; }
        public ReportResolutionAction? ResolutionAction { get; set; }
        public string? Queue { get; set; }
        public ModerationPenaltyType? PenaltyType { get; set; }
        public DateTimeOffset? PenaltyEndsAt { get; set; }
    }
}
