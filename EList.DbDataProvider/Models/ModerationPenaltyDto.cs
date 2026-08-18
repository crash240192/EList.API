using EList.DbDataProvider.Models.Enums;
using LinqToDB;
using LinqToDB.Mapping;

namespace EList.DbDataProvider.Models
{
    [Table("moderation_penalties")]
    public class ModerationPenaltyDto
    {
        [Column("id"), PrimaryKey, Identity]
        public Guid Id { get; set; }

        [Column("account_id")]
        public Guid? AccountId { get; set; }

        [Column("organization_id")]
        public Guid? OrganizationId { get; set; }

        [Column("event_id")]
        public Guid? EventId { get; set; }

        [Column("report_id")]
        public Guid? ReportId { get; set; }

        [Column("penalty_type", DataType = DataType.Enum)]
        public ModerationPenaltyType PenaltyType { get; set; }

        [Column("reason")]
        public string? Reason { get; set; }

        [Column("starts_at")]
        public DateTimeOffset StartsAt { get; set; }

        [Column("ends_at")]
        public DateTimeOffset? EndsAt { get; set; }

        [Column("revoked_at")]
        public DateTimeOffset? RevokedAt { get; set; }

        [Column("revoked_by")]
        public Guid? RevokedBy { get; set; }

        [Column("lifted_at")]
        public DateTimeOffset? LiftedAt { get; set; }

        [Column("created_by")]
        public Guid CreatedBy { get; set; }

        [Column("created_at")]
        public DateTimeOffset CreatedAt { get; set; }
    }
}
