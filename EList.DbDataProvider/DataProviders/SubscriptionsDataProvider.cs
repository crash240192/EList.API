using EList.DbDataProvider.Interfaces;
using EList.DbDataProvider.Models;
using LinqToDB;

namespace EList.DbDataProvider.DataProviders
{
    public class SubscriptionsDataProvider : DataProviderBase, ISubscriptionsDataProvider
    {
        public SubscriptionsDataProvider(IDataConnectionProvider dataConnectionProvider) : base(dataConnectionProvider)
        {
        }

        public async Task<List<SubscriptionDto>> GetSubscriptionsAsync(Guid accountId)
        {
            var result = await _connection.Subscriptions
                .Where(i => i.SubscriberId == accountId)
                .ToListAsync();
            return result;
        }

        public async Task<bool> IsSubscriptionExistAsync(Guid subscriberId, Guid subscribedToId)
        {
            var result = await _connection.Subscriptions
                .AnyAsync(i => i.SubscribedToId == subscribedToId && i.SubscriberId == subscriberId);
            return result;
        }

        public async Task<List<SubscriptionDto>> GetSubscribersAsync(Guid accountId, bool? notifyParticipated = null, bool? notifyEventCreated = false, bool? notifySubscribed = false)
        {
            var request = _connection.Subscriptions
                .Where(i => i.SubscribedToId == accountId);

            if (notifyParticipated != null) 
                request = request.Where(i => i.NotifyParticipated == notifyParticipated);

            if (notifyEventCreated != null)
                request = request.Where(i => i.NotifyEventCreated == notifyEventCreated);

            if (notifySubscribed != null)
                request = request.Where(i => i.NotifySubscribed == notifySubscribed);

            var result = await request.ToListAsync();
            return result;
        }

        public async Task<Guid> SubscribeToAccountAsync(Guid subscriberId, Guid subscribeToId)
        {
            var newSubscription = new SubscriptionDto
            {
                SubscriberId = subscriberId,
                SubscribedToId = subscribeToId
            };

            var subscriptionId = (Guid)await _connection.InsertWithIdentityAsync(newSubscription);
            return subscriptionId;
        }

        public async Task UpdateSubscriptionAsync(SubscriptionDto item)
        {
            var result = await _connection.Subscriptions
                .Where(i => i.SubscribedToId == item.SubscribedToId && i.SubscriberId == item.SubscriberId)
                .Set(i => i.NotifyEventCreated, item.NotifyEventCreated)
                .Set(i => i.NotifyParticipated, item.NotifyParticipated)
                .Set(i => i.NotifySubscribed, item.NotifySubscribed)
                .UpdateAsync();
        }

        public async Task DeleteSubscriptionAsync(Guid subscriberId, Guid subscribedToId)
        {
            var result = await _connection.Subscriptions
                .DeleteAsync(i => i.SubscribedToId == subscribedToId && i.SubscriberId == subscriberId);
        }
    }
}
