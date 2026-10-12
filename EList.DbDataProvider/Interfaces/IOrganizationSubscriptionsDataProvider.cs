using EList.DbDataProvider.Models;
using EList.DbDataProvider.Models.SearchRequests;
using EList.Models.Events;

namespace EList.DbDataProvider.Interfaces
{
    public interface IOrganizationSubscriptionsDataProvider
    {
        Task<Guid> SubscribeAsync(Guid subscriberId, Guid organizationId);
        Task UpdateAsync(Guid subscriberId, Guid organizationId, bool notifyEventCreated);
        Task DeleteAsync(Guid subscriberId, Guid organizationId);
        Task DeleteAllForAccountAsync(Guid accountId);
        Task DeleteAllForOrganizationAsync(Guid organizationId);
        Task<bool> IsSubscriptionExistAsync(Guid subscriberId, Guid organizationId);
        Task<ListResponse<OrganizationSubscriptionDto>> GetSubscriptionsAsync(OrganizationSubscriptionsSearchRequest request);
        Task<ListResponse<OrganizationSubscriptionDto>> GetSubscribersAsync(OrganizationSubscriptionsSearchRequest request);
        Task<List<Guid>> GetSubscriberIdsAsync(OrganizationSubscriptionsSearchRequest request);
        Task<int> GetSubscriptionsCountAsync(Guid accountId);
        Task<int> GetSubscribersCountAsync(Guid organizationId);
    }
}
