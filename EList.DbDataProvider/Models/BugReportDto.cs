using EList.DbDataProvider.Models.Enums;
using LinqToDB;
using LinqToDB.Mapping;

namespace EList.DbDataProvider.Models
{
    [Table(Schema = "bugreports", Name = "reports")]
    public class BugReportDto
    {
        [Column("id"), PrimaryKey, Identity]
        public Guid Id { get; set; }

        [Column("reporter_account_id")]
        public Guid ReporterAccountId { get; set; }

        [Column("category_id")]
        public Guid CategoryId { get; set; }

        [Column("description")]
        public string Description { get; set; }

        [Column("status", DataType = DataType.Enum)]
        public BugReportStatus Status { get; set; }

        [Column("create_date")]
        public DateTimeOffset CreateDate { get; set; }

        [Column("update_date")]
        public DateTimeOffset UpdateDate { get; set; }


        [Association(ThisKey = nameof(ReporterAccountId), OtherKey = nameof(AccountDto.Id))]
        public AccountDto? ReporterAccount { get; set; }

        [Association(ThisKey = nameof(CategoryId), OtherKey = nameof(BugReportCategoryDto.Id))]
        public BugReportCategoryDto? Category { get; set; }

        [Association(ThisKey = nameof(Id), OtherKey = nameof(BugReportFileDto.ReportId))]
        public List<BugReportFileDto> Files { get; set; }
    }
}
