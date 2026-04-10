using LinqToDB.Mapping;

namespace EList.DbDataProvider.Models
{
    [Table("public.event_organizators")]
    public class EventOrganizatorDto
    {
        [Column("id"), PrimaryKey, Identity]
        public Guid Id { get; set; }

        [Column("event_id")]
        public Guid EventId { get; set; }

        [Column("account_id")]
        public Guid? AccountId { get; set; }

        [Column("organization_id")]
        public Guid? OrganizationId { get; set; }

        [Association(ThisKey = nameof(EventId), OtherKey = nameof(EventDto.Id))]
        public EventDto Event { get; set; }

        [Association(ThisKey = nameof(AccountId), OtherKey = nameof(AccountDto.Id))]
        public AccountDto Account { get; set; }

        [Association(ThisKey = nameof(OrganizationId), OtherKey = nameof(OrganizationDto.Id))]
        public OrganizationDto Organization { get; set; }
    }
}
