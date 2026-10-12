using EList.Models.Organizations;

namespace EList.Models.Subscriptions
{
    public class OrganizationSubscription
    {
        public Guid Id { get; set; }

        public Subscriber? Subscriber { get; set; }

        public OrganizationSubscriptionTarget? Organization { get; set; }

        public bool NotifyEventCreated { get; set; }
    }

    /// <summary>Публичные данные организации в списках подписок.</summary>
    public class OrganizationSubscriptionTarget
    {
        public Guid Id { get; set; }

        public string Name { get; set; }

        public bool Active { get; set; }
    }
}
