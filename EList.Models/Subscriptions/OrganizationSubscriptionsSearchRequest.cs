namespace EList.Models.Subscriptions
{
    public class OrganizationSubscriptionsSearchRequest
    {
        /// <summary>AccountId владельца списка подписок (getSubscriptions).</summary>
        public Guid? AccountId { get; set; }

        /// <summary>OrganizationId для списка подписчиков (getSubscribers).</summary>
        public Guid? OrganizationId { get; set; }

        public string? Name { get; set; }

        public bool? NotifyEventCreated { get; set; }

        public int? PageIndes { get; set; }
        public int? PageSize { get; set; }
    }
}
