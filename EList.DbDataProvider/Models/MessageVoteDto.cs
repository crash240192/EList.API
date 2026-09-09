using EList.DbDataProvider.Models.Enums;
using LinqToDB;
using LinqToDB.Mapping;

namespace EList.DbDataProvider.Models
{
    [Table("message_votes")]
    public class MessageVoteDto
    {
        [Column("id"), PrimaryKey, Identity]
        public Guid Id { get; set; }

        [Column("message_id")]
        public Guid MessageId { get; set; }

        [Column("account_id")]
        public Guid AccountId { get; set; }

        [Column("value", DataType = DataType.Enum)]
        public MessageVoteValue Value { get; set; }

        [Column("create_date")]
        public DateTimeOffset CreateDate { get; set; }

        [Column("update_date")]
        public DateTimeOffset UpdateDate { get; set; }

        [Association(ThisKey = nameof(MessageId), OtherKey = nameof(MessageDto.Id))]
        public MessageDto Message { get; set; }

        [Association(ThisKey = nameof(AccountId), OtherKey = nameof(AccountDto.Id))]
        public AccountDto Account { get; set; }
    }
}
