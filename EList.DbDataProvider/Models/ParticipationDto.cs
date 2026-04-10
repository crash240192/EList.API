using LinqToDB.Mapping;

namespace EList.DbDataProvider.Models
{
    [Table("participations")]
    public class ParticipationDto
    {
        [Column("id"), PrimaryKey, Identity]
        public Guid Id { get; set; }

        [Column("account_id")]
        public Guid AccountId { get; set; }

        [Column("event_id")]
        public Guid EventId { get; set; }

        [Association(ThisKey = nameof(AccountId), OtherKey = nameof(AccountDto.Id))]
        public AccountDto Account { get; set; }

        [Association(ThisKey = nameof(EventId), OtherKey = nameof(EventDto.Id))]
        public EventDto Event { get; set; }
    }
}
