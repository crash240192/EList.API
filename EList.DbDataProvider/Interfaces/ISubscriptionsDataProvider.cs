using EList.DbDataProvider.Models;

namespace EList.DbDataProvider.Interfaces
{
    public interface ISubscriptionsDataProvider
    {
        Task<(int, List<SubscriptionDto>)> GetSubscriptionsAsync(Guid accountId);
        Task<(int, List<SubscriptionDto>)> GetSubscribersAsync(Guid accountId, bool? notifyParticipated = null, bool? notifyEventCreated = false, bool? notifySubscribed = false);
        Task<int> GetSubscriptionsCountAsync(Guid accountId);
        Task<int> GetSubscribersCountAsync(Guid accountId);
        Task<Guid> SubscribeToAccountAsync(Guid subscriberId, Guid subscribeToId);
        Task UpdateSubscriptionAsync(SubscriptionDto request);
        Task DeleteSubscriptionAsync(Guid subscriberId, Guid subscribedToId);
        Task<bool> IsSubscriptionExistAsync(Guid subscriberId, Guid subscribeToId);
    }
}
