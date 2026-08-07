using LinqToDB.Mapping;

namespace EList.DbDataProvider.Models
{
    [Table(Schema = "bugreports", Name = "report_files")]
    public class BugReportFileDto
    {
        [Column("id"), PrimaryKey, Identity]
        public Guid Id { get; set; }

        [Column("report_id")]
        public Guid ReportId { get; set; }

        [Column("file_id")]
        public Guid FileId { get; set; }


        [Association(ThisKey = nameof(ReportId), OtherKey = nameof(BugReportDto.Id))]
        public BugReportDto? Report { get; set; }
    }
}
