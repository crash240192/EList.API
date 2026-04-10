namespace EList.Models.Subscriptions
{
    public class UpdateSubscriptionRequest : UpdateSubscriptionRequestBase
    {
        public Guid SubscriberId { get; set; }

        public Guid SubscribedToId { get; set; }
    }
}
