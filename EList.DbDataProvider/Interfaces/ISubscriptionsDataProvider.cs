using EList.DbDataProvider.Models;
using EList.DbDataProvider.Models.SearchRequests;

namespace EList.DbDataProvider.Interfaces
{
    public interface ISubscriptionsDataProvider
    {
        Task<ListResponse<SubscriptionDto>> GetSubscriptionsAsync(SubscriptionsSearchRequest request);
        Task<ListResponse<SubscriptionDto>> GetSubscribersAsync(SubscriptionsSearchRequest request);
        Task<int> GetSubscriptionsCountAsync(Guid accountId);
        Task<int> GetSubscribersCountAsync(Guid accountId);
        Task<Guid> SubscribeToAccountAsync(Guid subscriberId, Guid subscribeToId);
        Task UpdateSubscriptionAsync(SubscriptionDto request);
        Task DeleteSubscriptionAsync(Guid subscriberId, Guid subscribedToId);
        Task<bool> IsSubscriptionExistAsync(Guid subscriberId, Guid subscribeToId);
    }
}
