using EList.Models.Accounts;

namespace EList.Models.Subscriptions
{
    public class Subscription
    {
        public Guid Id { get; set; }

        public Subscriber Subscriber { get; set; }

        public Subscriber SubscribedTo { get; set; }

        public bool NotifyParticipated { get; set; }

        public bool NotifyEventCreated { get; set; }

        public bool NotifySubscribed { get; set; }
    }
}
