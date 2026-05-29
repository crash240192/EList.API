using LinqToDB.Mapping;

namespace EList.DbDataProvider.Models
{
    [Table("participants_black_list")]
    public class ParticipantsBlackListItemDto
    {
        [Column("id"), PrimaryKey, Identity]
        public Guid Id { get; set; }

        [Column("event_id")]
        public Guid EventId { get; set; }

        [Column("account_id")]
        public Guid AccountId { get; set; }

        [Association(ThisKey = nameof(AccountId), OtherKey = nameof(AccountDto.Id))]
        public AccountDto Account { get; set; }
    }
}
