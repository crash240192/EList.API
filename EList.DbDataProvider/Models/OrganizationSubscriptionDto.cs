using LinqToDB.Mapping;

namespace EList.DbDataProvider.Models
{
    [Table("organization_subscriptions")]
    public class OrganizationSubscriptionDto
    {
        [Column("id"), PrimaryKey, Identity]
        public Guid Id { get; set; }

        [Column("subscriber_id")]
        public Guid SubscriberId { get; set; }

        [Column("organization_id")]
        public Guid OrganizationId { get; set; }

        [Column("notify_event_created")]
        public bool NotifyEventCreated { get; set; }

        [Association(ThisKey = nameof(SubscriberId), OtherKey = nameof(AccountDto.Id))]
        public AccountDto Subscriber { get; set; }

        [Association(ThisKey = nameof(OrganizationId), OtherKey = nameof(OrganizationDto.Id))]
        public OrganizationDto Organization { get; set; }
    }
}
