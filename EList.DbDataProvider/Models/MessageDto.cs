using LinqToDB.Mapping;

namespace EList.DbDataProvider.Models
{
    [Table("message")]
    public class MessageDto
    {
        [Column("id"), PrimaryKey, Identity]
        public Guid Id { get; set; }

        [Column("conversation_id")]
        public Guid ConversationId { get; set; }

        [Column("message_text")]
        public string MessageText { get; set; }

        [Column("replied")]
        public bool Replied { get; set; }

        [Column("account_id")]
        public Guid? AccountId { get; set; }

        [Column("organization_id")]
        public Guid? OrganizationId { get; set; }

        [Column("reply_to")]
        public Guid? ReplyTo { get; set; }

        [Column("create_date")]
        public DateTimeOffset CreateDate { get; set; }

        [Column("update_date")]
        public DateTimeOffset UpdateDate { get; set; }

        [Column("hidden")]
        public bool Hidden { get; set; }

        [Column("hidden_at")]
        public DateTimeOffset? HiddenAt { get; set; }

        [Column("hidden_by")]
        public Guid? HiddenBy { get; set; }


        [Association (ThisKey = nameof (AccountId), OtherKey = nameof(AccountDto.Id))]
        public AccountDto Account { get; set; }

        [Association(ThisKey = nameof(HiddenBy), OtherKey = nameof(AccountDto.Id))]
        public AccountDto? HiddenByAccount { get; set; }

        [Association(ThisKey = nameof(OrganizationId), OtherKey = nameof(OrganizationDto.Id))]
        public OrganizationDto Organization { get; set; }

        [Association(ThisKey = nameof(ConversationId), OtherKey = nameof(ConversationDto.Id))]
        public ConversationDto Conversation { get; set; }
    }
}
