using EList.Models.Subscriptions;

namespace EList.Repositories.Interfaces
{
    public interface ISubscriptionsRepository
    {
        Task SubscribeToAccountAsync(Guid subscriberId, Guid subscribeToId);
        Task<List<Subscription>?> GetSubscriptionsAsync(Guid accountId);
        Task<bool> IsSubscriptionExistAsync(Guid subscriberId, Guid subscribedToId);
        Task<List<Subscription>?> GetSubscribersAsync(Guid accountId, bool? notifyParticipated = null, bool? notifyEventCreated = false, bool? notifySubscribed = false);
        Task DeleteSubscriptionAsync(Guid subscriberId, Guid subscribedToId);
        Task UpdateSubscriptionAsync(UpdateSubscriptionRequest request);
    }
}
