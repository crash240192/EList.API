namespace EList.Models.Subscriptions
{
    public class UpdateSubscriptionRequestBase
    {
        public bool NotifyParticipated { get; set; }

        public bool NotifyEventCreated { get; set; }

        public bool NotifySubscribed { get; set; }
    }
}
