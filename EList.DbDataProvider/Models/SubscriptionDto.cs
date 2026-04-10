using LinqToDB.Mapping;

namespace EList.DbDataProvider.Models
{
    [Table("public.subscriptions")]
    public class SubscriptionDto
    {
        [Column("id"), PrimaryKey, Identity]
        public Guid Id { get; set; }
        
        [Column("subscriber_id")]
        public Guid SubscriberId { get; set; }
        
        [Column("subscribed_to_id")]
        public Guid SubscribedToId { get; set; }

        [Column("notify_participated")]
        public bool NotifyParticipated { get; set; }

        [Column("notify_event_created")]
        public bool NotifyEventCreated { get; set; }

        [Column("notify_subscribed")]
        public bool NotifySubscribed { get; set; }

        [Association(ThisKey = nameof(SubscriberId), OtherKey = nameof(AccountDto.Id))]
        public AccountDto Subscriber { get; set; }
        
        [Association(ThisKey = nameof(SubscribedToId), OtherKey = nameof(AccountDto.Id))]
        public AccountDto SubscribedTo { get; set; }
    }
}
