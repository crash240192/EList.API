using EList.Models.Enums;

namespace EList.Models.ContentReports
{
    public class ModerationPenalty
    {
        public Guid Id { get; set; }
        public Guid? AccountId { get; set; }
        public Guid? OrganizationId { get; set; }
        public Guid? EventId { get; set; }
        public Guid? ReportId { get; set; }
        public ModerationPenaltyType PenaltyType { get; set; }
        public string? Reason { get; set; }
        public DateTimeOffset StartsAt { get; set; }
        public DateTimeOffset? EndsAt { get; set; }
        public DateTimeOffset? RevokedAt { get; set; }
        public Guid? RevokedBy { get; set; }
        public DateTimeOffset? LiftedAt { get; set; }
        public Guid CreatedBy { get; set; }
        public DateTimeOffset CreatedAt { get; set; }

        public bool IsActive =>
            RevokedAt == null
            && LiftedAt == null
            && (EndsAt == null || EndsAt > DateTimeOffset.UtcNow);
    }
}
