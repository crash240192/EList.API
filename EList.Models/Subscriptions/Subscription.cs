namespace EList.Models.Subscriptions
{
    public class Subscription
    {
        public Guid Id { get; set; }

        public Guid SubscriberId { get; set; }

        public Guid SubscribedToId { get; set; }

        public bool NotifyParticipated { get; set; }

        public bool NotifyEventCreated { get; set; }

        public bool NotifySubscribed { get; set; }
    }
}
