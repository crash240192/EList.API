using EList.DbDataProvider.Interfaces;
using EList.DbDataProvider.Models;
using EList.DbDataProvider.Models.SearchRequests;
using EList.Models.Events;
using LinqToDB;
using LinqToDB.Async;

namespace EList.DbDataProvider.DataProviders
{
    public class SubscriptionsDataProvider : DataProviderBase, ISubscriptionsDataProvider
    {
        public SubscriptionsDataProvider(IDataConnectionProvider dataConnectionProvider) : base(dataConnectionProvider)
        {
        }

        public async Task<ListResponse<SubscriptionDto>> GetSubscriptionsAsync(SubscriptionsSearchRequest request)
        {
            var subscriptionsRequest = _connection.Subscriptions
                //.LoadWith(i => i.Subscriber)
                //.ThenLoad(i => i.PersonInfo)
                .LoadWith(i => i.SubscribedTo)
                .ThenLoad(i => i.PersonInfo)
                .OrderBy(i => i.SubscribedTo.Login)
                .Where(i => i.SubscriberId == request.AccountId);

            if (!string.IsNullOrWhiteSpace(request.Name))
            {
                var nameSubstrings = request.Name?.Split(' ').Where(i => i.Length > 0).ToList() ?? null;

                if (nameSubstrings?.Count() > 0)
                    subscriptionsRequest = subscriptionsRequest.Where(i => nameSubstrings.All(name => i.Subscriber.Login.ToLower().Contains(name.ToLower())
                    || i.Subscriber.PersonInfo.LastName.ToLower().Contains(name.ToLower())
                    || i.Subscriber.PersonInfo.Patronymic.ToLower().Contains(name.ToLower())
                    || i.Subscriber.PersonInfo.FirstName.ToLower().Contains(name.ToLower())));
            }


            var count = await subscriptionsRequest.CountAsync();

            List<SubscriptionDto> result = null;
            if (request.PageIndes != null && request.PageSize != null)
                result = await subscriptionsRequest.Skip(request.PageSize.Value * request.PageIndes.Value).Take(request.PageSize.Value).ToListAsync();
            else
                result = await subscriptionsRequest.ToListAsync();

            return new ListResponse<SubscriptionDto>(count, result);
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

        public async Task<ListResponse<SubscriptionDto>> GetSubscribersAsync(SubscriptionsSearchRequest request)
        {
            var subscriptionsRequest = _connection.Subscriptions
                .LoadWith(i => i.Subscriber)
                .ThenLoad(i => i.PersonInfo)
                //.OrderBy(i => i.Subscriber.Login)
                .OrderBy(i => i.Subscriber.PersonInfo.Patronymic)
                .OrderBy(i => i.Subscriber.PersonInfo.LastName)
                .OrderBy(i => i.Subscriber.PersonInfo.FirstName)
                .Where(i => i.SubscribedToId == request.AccountId);

            if (!string.IsNullOrWhiteSpace(request.Name))
            {
                var nameSubstrings = request.Name?.Split(' ').Where(i => i.Length > 0).ToList() ?? null;

                if (nameSubstrings?.Count() > 0)
                    subscriptionsRequest = subscriptionsRequest.Where(i => nameSubstrings.All(name => i.Subscriber.Login.ToLower().Contains(name.ToLower())
                    || i.Subscriber.PersonInfo.LastName.ToLower().Contains(name.ToLower())
                    || i.Subscriber.PersonInfo.Patronymic.ToLower().Contains(name.ToLower())
                    || i.Subscriber.PersonInfo.FirstName.ToLower().Contains(name.ToLower())));
            }

            if (request.NotifyParticipated != null)
                subscriptionsRequest = subscriptionsRequest.Where(i => i.NotifyParticipated == request.NotifyParticipated);

            if (request.NotifyEventCreated != null)
                subscriptionsRequest = subscriptionsRequest.Where(i => i.NotifyEventCreated == request.NotifyEventCreated);

            if (request.NotifySubscribed != null)
                subscriptionsRequest = subscriptionsRequest.Where(i => i.NotifySubscribed == request.NotifySubscribed);

            var count = await subscriptionsRequest.CountAsync();

            List<SubscriptionDto> result = null;
            if (request.PageIndes != null && request.PageSize != null)
                result = await subscriptionsRequest.Skip(request.PageSize.Value * request.PageIndes.Value).Take(request.PageSize.Value).ToListAsync();
            else
                result = await subscriptionsRequest.ToListAsync();

            return new ListResponse<SubscriptionDto>(count, result);
        }

        public async Task<List<Guid>> GetSubscribersIdsAsync(SubscriptionsSearchRequest request)
        {
            var subscriptionsRequest = _connection.Subscriptions
                .Where(i => i.SubscribedToId == request.AccountId);

            if (request.NotifyParticipated != null)
                subscriptionsRequest = subscriptionsRequest.Where(i => i.NotifyParticipated == request.NotifyParticipated);

            if (request.NotifyEventCreated != null)
                subscriptionsRequest = subscriptionsRequest.Where(i => i.NotifyEventCreated == request.NotifyEventCreated);

            if (request.NotifySubscribed != null)
                subscriptionsRequest = subscriptionsRequest.Where(i => i.NotifySubscribed == request.NotifySubscribed);

            var result = await subscriptionsRequest.Select(i => i.SubscriberId).ToListAsync();

            return result;
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
