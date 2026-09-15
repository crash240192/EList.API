using LinqToDB.Mapping;

namespace EList.DbDataProvider.Models
{
    [Table("message_files")]
    public class MessageFileDto
    {
        [Column("id"), PrimaryKey, Identity]
        public Guid Id { get; set; }

        [Column("message_id")]
        public Guid MessageId { get; set; }

        [Column("file_id")]
        public Guid FileId { get; set; }

        [Column("sort_order")]
        public int SortOrder { get; set; }

        [Association(ThisKey = nameof(MessageId), OtherKey = nameof(MessageDto.Id))]
        public MessageDto Message { get; set; }
    }
}
