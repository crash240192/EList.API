using EList.DbDataProvider.Models.Enums;
using LinqToDB;
using LinqToDB.Mapping;

namespace EList.DbDataProvider.Models
{
    [Table("report_reasons")]
    public class ReportReasonDto
    {
        [Column("id"), PrimaryKey, Identity]
        public Guid Id { get; set; }

        [Column("code")]
        public string Code { get; set; }

        [Column("name")]
        public string Name { get; set; }

        [Column("description")]
        public string? Description { get; set; }

        [Column("target_scope", DataType = DataType.Enum)]
        public ReportTargetScope TargetScope { get; set; }

        [Column("severity", DataType = DataType.Enum)]
        public ReportSeverity Severity { get; set; }

        [Column("primary_queue", DataType = DataType.Enum)]
        public ReportQueue PrimaryQueue { get; set; }

        [Column("sort_order")]
        public int SortOrder { get; set; }

        [Column("active")]
        public bool Active { get; set; } = true;

        [Column("create_date")]
        public DateTimeOffset CreateDate { get; set; }
    }
}
