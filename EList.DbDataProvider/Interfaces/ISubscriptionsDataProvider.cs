using EList.DbDataProvider.Models;

namespace EList.DbDataProvider.Interfaces
{
    public interface ISubscriptionsDataProvider
    {
        Task<List<SubscriptionDto>> GetSubscriptionsAsync(Guid accountId);
        Task<List<SubscriptionDto>> GetSubscribersAsync(Guid accountId, bool? notifyParticipated = null, bool? notifyEventCreated = false, bool? notifySubscribed = false);
        Task<Guid> SubscribeToAccountAsync(Guid subscriberId, Guid subscribeToId);
        Task UpdateSubscriptionAsync(SubscriptionDto request);
        Task DeleteSubscriptionAsync(Guid subscriberId, Guid subscribedToId);
        Task<bool> IsSubscriptionExistAsync(Guid subscriberId, Guid subscribeToId);
    }
}
