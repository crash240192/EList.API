namespace EList.Models.Subscriptions
{
    public class SubscriptionsSearchRequest
    {
        public Guid AccountId { get; set; }
        public string Name { get; set; }

        public bool? notifyParticipated { get; set; }
        public bool? notifyEventCreated { get; set; } 
        public bool? notifySubscribed { get; set; }

        public int? PageIndes { get; set; }
        public int? PageSize { get; set; }
    }
}
