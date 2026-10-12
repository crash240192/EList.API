namespace EList.DbDataProvider.Models.SearchRequests
{
    public class OrganizationSubscriptionsSearchRequest
    {
        /// <summary>Для списка подписок пользователя — accountId подписчика.</summary>
        public Guid? AccountId { get; set; }

        /// <summary>Для списка подписчиков организации — organizationId.</summary>
        public Guid? OrganizationId { get; set; }

        public string? Name { get; set; }

        public bool? NotifyEventCreated { get; set; }

        public int? PageIndes { get; set; }
        public int? PageSize { get; set; }
    }
}
