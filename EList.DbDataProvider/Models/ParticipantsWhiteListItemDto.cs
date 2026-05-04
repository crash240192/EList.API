using LinqToDB.Mapping;

namespace EList.DbDataProvider.Models
{
    [Table("participants_white_list")]
    public class ParticipantsWhiteListItemDto
    {
        [Column("id"), PrimaryKey, Identity]
        public Guid Id { get; set; }

        [Column("event_id")]
        public Guid EventId { get; set; }

        [Column("account_id")]
        public Guid AccountId { get; set; }

        [Association(ThisKey = nameof(Id), OtherKey = nameof(AccountDto.Id))]
        public AccountDto Account { get; set; }
    }
}
