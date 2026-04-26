using EList.DbDataProvider.Interfaces;
using EList.DbDataProvider.Models;
using LinqToDB;
using LinqToDB.Async;

namespace EList.DbDataProvider.DataProviders
{
    public class SubscriptionsDataProvider : DataProviderBase, ISubscriptionsDataProvider
    {
        public SubscriptionsDataProvider(IDataConnectionProvider dataConnectionProvider) : base(dataConnectionProvider)
        {
        }

        public async Task<(int, List<SubscriptionDto>)> GetSubscriptionsAsync(Guid accountId)
        {
            var request = _connection.Subscriptions
                //.LoadWith(i => i.Subscriber)
                //.ThenLoad(i => i.PersonInfo)
                .LoadWith(i => i.SubscribedTo)
                .ThenLoad(i => i.PersonInfo)
                .OrderBy(i => i.SubscribedTo.Login)
                .Where(i => i.SubscriberId == accountId);

            var count = await request.CountAsync();

            var result = await request.ToListAsync();
            return (count, result);
        }

        public async Task<int> GetSubscriptionsCountAsync(Guid accountId)
        {
            var result = await _connection.Subscriptions
                .Where(i => i.SubscriberId == accountId)
                .CountAsync();

            return result;
        }

        public async Task<bool> IsSubscriptionExistAsync(Guid subscriberId, Guid subscribedToId)
        {
            var result = await _connection.Subscriptions
                .AnyAsync(i => i.SubscribedToId == subscribedToId && i.SubscriberId == subscriberId);
            return result;
        }

        public async Task<(int, List<SubscriptionDto>)> GetSubscribersAsync(Guid accountId, bool? notifyParticipated = null, bool? notifyEventCreated = false, bool? notifySubscribed = false)
        {
            var request = _connection.Subscriptions
                .LoadWith(i => i.Subscriber)
                .ThenLoad(i => i.PersonInfo)
                //.LoadWith(i => i.SubscribedTo)
                //.ThenLoad(i => i.PersonInfo)                
                .OrderBy(i => i.Subscriber.Login)
                .Where(i => i.SubscribedToId == accountId);

            if (notifyParticipated != null) 
                request = request.Where(i => i.NotifyParticipated == notifyParticipated);
            
            if (notifyEventCreated != null)
                request = request.Where(i => i.NotifyEventCreated == notifyEventCreated);

            if (notifySubscribed != null)
                request = request.Where(i => i.NotifySubscribed == notifySubscribed);

            var count = await request.CountAsync();

            var result = await request.ToListAsync();
            return (count, result);
        }

        public async Task<int> GetSubscribersCountAsync(Guid accountId)
        {
            var result = await _connection.Subscriptions
                .Where(i => i.SubscribedToId == accountId)
                .CountAsync();
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
