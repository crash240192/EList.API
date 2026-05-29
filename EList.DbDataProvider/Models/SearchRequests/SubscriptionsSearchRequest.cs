namespace EList.DbDataProvider.Models.SearchRequests
{
    public class SubscriptionsSearchRequest
    {
        public Guid AccountId { get; set; }
        public string Name { get; set; }

        public bool? NotifyParticipated { get; set; }
        public bool? NotifyEventCreated { get; set; }
        public bool? NotifySubscribed { get; set; }

        public int? PageIndes { get; set; }
        public int? PageSize { get; set; }
    }
}
