using EList.Common.Models;
using EList.Models.Subscriptions;

namespace EList.Repositories.Interfaces
{
    public interface ISubscriptionsRepository
    {
        Task SubscribeToAccountAsync(Guid subscriberId, Guid subscribeToId);
        Task<PagedList<Subscription>?> GetSubscriptionsAsync(Guid accountId);
        Task<int> GetSubscriptionsCountAsync(Guid accountId);
        Task<PagedList<Subscription>?> GetSubscribersAsync(Guid accountId, bool? notifyParticipated = null, bool? notifyEventCreated = false, bool? notifySubscribed = false);
        Task<int> GetSubscribersCountAsync(Guid accountId);
        Task<bool> IsSubscriptionExistAsync(Guid subscriberId, Guid subscribedToId);        
        Task DeleteSubscriptionAsync(Guid subscriberId, Guid subscribedToId);
        Task UpdateSubscriptionAsync(UpdateSubscriptionRequest request);
    }
}
