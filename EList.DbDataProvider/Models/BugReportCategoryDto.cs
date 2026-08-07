using EList.DbDataProvider.Models.Enums;
using LinqToDB;
using LinqToDB.Mapping;

namespace EList.DbDataProvider.Models
{
    [Table(Schema = "bugreports", Name = "categories")]
    public class BugReportCategoryDto
    {
        [Column("id"), PrimaryKey, Identity]
        public Guid Id { get; set; }

        [Column("code")]
        public string Code { get; set; }

        [Column("name")]
        public string Name { get; set; }

        [Column("active")]
        public bool Active { get; set; }

        [Column("sort_order")]
        public int SortOrder { get; set; }

        [Column("create_date")]
        public DateTimeOffset CreateDate { get; set; }
    }
}
