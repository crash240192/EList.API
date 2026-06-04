using LinqToDB.Mapping;
using Newtonsoft.Json.Linq;


namespace EList.DbDataProvider.Models
{
    [Table("notifications")]
    public class NotificationDto
    {
        [Column("id"), PrimaryKey, Identity]
        public Guid Id { get; set; }

        [Column("event_id")]
        public Guid? EventId { get; set; }

        [Column("account_id")]
        public Guid AccountId { get; set; }

        [Column("related_account_id")]
        public Guid? RelatedAccountId { get; set; }

        [Column("type")]
        public int? Type { get; set; }

        [Column("title")]
        public string? Title { get; set; }

        [Column("message")]
        public string? Message { get; set; }

        [Column("created_at")]
        public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.Now;

        [Column("read_at")]
        public DateTimeOffset? ReadAt { get; set; }

        [Column("data"), DataType("jsonb")]
        public string? Data { get; set; }


        [Association(ThisKey = nameof(EventId), OtherKey = nameof(EventDto.Id))]
        public EventDto Event { get; set; }

        [Association(ThisKey = nameof(AccountId), OtherKey = nameof(AccountDto.Id))]
        public AccountDto Account { get; set; }

        [Association(ThisKey = nameof(RelatedAccountId), OtherKey = nameof(AccountDto.Id))]
        public AccountDto RelatedAccount { get; set; }
    }
}

