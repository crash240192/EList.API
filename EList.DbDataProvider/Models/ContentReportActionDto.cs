using EList.DbDataProvider.Models.Enums;
using LinqToDB;
using LinqToDB.Mapping;

namespace EList.DbDataProvider.Models
{
    [Table("content_report_actions")]
    public class ContentReportActionDto
    {
        [Column("id"), PrimaryKey, Identity]
        public Guid Id { get; set; }

        [Column("report_id")]
        public Guid ReportId { get; set; }

        [Column("actor_account_id")]
        public Guid? ActorAccountId { get; set; }

        [Column("actor_context", DataType = DataType.Enum)]
        public ReportActorContext ActorContext { get; set; }

        [Column("action")]
        public string Action { get; set; }

        [Column("details"), DataType("jsonb")]
        public string? Details { get; set; }

        [Column("created_at")]
        public DateTimeOffset CreatedAt { get; set; }


        [Association(ThisKey = nameof(ReportId), OtherKey = nameof(ContentReportDto.Id))]
        public ContentReportDto? Report { get; set; }

        [Association(ThisKey = nameof(ActorAccountId), OtherKey = nameof(AccountDto.Id))]
        public AccountDto? ActorAccount { get; set; }
    }
}
