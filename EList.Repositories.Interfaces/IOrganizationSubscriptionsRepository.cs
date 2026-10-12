using EList.Common.Models;
using EList.Models.Subscriptions;

namespace EList.Repositories.Interfaces
{
    public interface IOrganizationSubscriptionsRepository
    {
        Task SubscribeAsync(Guid subscriberId, Guid organizationId);
        Task UpdateAsync(Guid subscriberId, Guid organizationId, bool notifyEventCreated);
        Task DeleteAsync(Guid subscriberId, Guid organizationId);
        Task DeleteAllForAccountAsync(Guid accountId);
        Task<bool> IsSubscriptionExistAsync(Guid subscriberId, Guid organizationId);
        Task<PagedList<OrganizationSubscription>?> GetSubscriptionsAsync(OrganizationSubscriptionsSearchRequest request);
        Task<PagedList<OrganizationSubscription>?> GetSubscribersAsync(OrganizationSubscriptionsSearchRequest request);
        Task<List<Guid>> GetSubscriberIdsAsync(OrganizationSubscriptionsSearchRequest request);
        Task<int> GetSubscriptionsCountAsync(Guid accountId);
        Task<int> GetSubscribersCountAsync(Guid organizationId);
    }
}
