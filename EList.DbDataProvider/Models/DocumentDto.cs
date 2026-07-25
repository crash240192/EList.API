using EList.DbDataProvider.Models.Enums;
using LinqToDB;
using LinqToDB.Mapping;

namespace EList.DbDataProvider.Models
{
    [Table("documents")]
    public class DocumentDto
    {
        [Column("id"), PrimaryKey, Identity]
        public Guid Id { get; set; }

        [Column("header")]
        public string Header { get; set; }

        [Column("text")]
        public string Text { get; set; }

        [Column("hash")]
        public string Hash { get; set; }

        [Column("type", DataType = DataType.Enum)]
        public DocumentType Type { get; set; }

        [Column("version")]
        public string Version { get; set; }

        [Column("creation_date")]
        public DateTimeOffset CreationDate { get; set; }
    }
}
