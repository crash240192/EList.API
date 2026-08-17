using EList.DbDataProvider.Models.Enums;
using LinqToDB;
using LinqToDB.Mapping;

namespace EList.DbDataProvider.Models
{
    [Table("content_reports")]
    public class ContentReportDto
    {
        [Column("id"), PrimaryKey, Identity]
        public Guid Id { get; set; }

        [Column("reporter_account_id")]
        public Guid ReporterAccountId { get; set; }

        [Column("target_type", DataType = DataType.Enum)]
        public ReportTargetType TargetType { get; set; }

        [Column("target_id")]
        public Guid TargetId { get; set; }

        [Column("event_id")]
        public Guid? EventId { get; set; }

        [Column("message_id")]
        public Guid? MessageId { get; set; }

        [Column("conversation_id")]
        public Guid? ConversationId { get; set; }

        [Column("file_id")]
        public Guid? FileId { get; set; }

        [Column("album_id")]
        public Guid? AlbumId { get; set; }

        [Column("reported_account_id")]
        public Guid? ReportedAccountId { get; set; }

        [Column("organization_id")]
        public Guid? OrganizationId { get; set; }

        [Column("event_organizator_id")]
        public Guid? EventOrganizatorId { get; set; }

        [Column("reason_id")]
        public Guid ReasonId { get; set; }

        [Column("comment")]
        public string? Comment { get; set; }

        [Column("target_snapshot"), DataType("jsonb")]
        public string? TargetSnapshot { get; set; }

        [Column("status", DataType = DataType.Enum)]
        public ReportStatus Status { get; set; }

        [Column("organizer_status", DataType = DataType.Enum)]
        public ReportStatus? OrganizerStatus { get; set; }

        [Column("platform_status", DataType = DataType.Enum)]
        public ReportStatus? PlatformStatus { get; set; }

        [Column("assigned_to")]
        public Guid? AssignedTo { get; set; }

        [Column("resolution_action", DataType = DataType.Enum)]
        public ReportResolutionAction? ResolutionAction { get; set; }

        [Column("resolution_comment")]
        public string? ResolutionComment { get; set; }

        [Column("resolved_by")]
        public Guid? ResolvedBy { get; set; }

        [Column("resolved_at")]
        public DateTimeOffset? ResolvedAt { get; set; }

        [Column("created_at")]
        public DateTimeOffset CreatedAt { get; set; }

        [Column("updated_at")]
        public DateTimeOffset UpdatedAt { get; set; }


        [Association(ThisKey = nameof(ReporterAccountId), OtherKey = nameof(AccountDto.Id))]
        public AccountDto? ReporterAccount { get; set; }

        [Association(ThisKey = nameof(ReasonId), OtherKey = nameof(ReportReasonDto.Id))]
        public ReportReasonDto? Reason { get; set; }

        [Association(ThisKey = nameof(EventId), OtherKey = nameof(EventDto.Id))]
        public EventDto? Event { get; set; }

        [Association(ThisKey = nameof(MessageId), OtherKey = nameof(MessageDto.Id))]
        public MessageDto? Message { get; set; }

        [Association(ThisKey = nameof(ConversationId), OtherKey = nameof(ConversationDto.Id))]
        public ConversationDto? Conversation { get; set; }

        [Association(ThisKey = nameof(ReportedAccountId), OtherKey = nameof(AccountDto.Id))]
        public AccountDto? ReportedAccount { get; set; }

        [Association(ThisKey = nameof(OrganizationId), OtherKey = nameof(OrganizationDto.Id))]
        public OrganizationDto? Organization { get; set; }

        [Association(ThisKey = nameof(EventOrganizatorId), OtherKey = nameof(EventOrganizatorDto.Id))]
        public EventOrganizatorDto? EventOrganizator { get; set; }

        [Association(ThisKey = nameof(AlbumId), OtherKey = nameof(MediaAlbumDto.Id))]
        public MediaAlbumDto? Album { get; set; }

        [Association(ThisKey = nameof(AssignedTo), OtherKey = nameof(AccountDto.Id))]
        public AccountDto? AssignedToAccount { get; set; }

        [Association(ThisKey = nameof(ResolvedBy), OtherKey = nameof(AccountDto.Id))]
        public AccountDto? ResolvedByAccount { get; set; }

        [Association(ThisKey = nameof(Id), OtherKey = nameof(ContentReportActionDto.ReportId))]
        public List<ContentReportActionDto>? Actions { get; set; }
    }
}
