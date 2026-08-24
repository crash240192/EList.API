using EList.DbDataProvider.Interfaces;
using EList.DbDataProvider.Models;
using EList.DbDataProvider.Models.SearchRequests;
using EList.DbDataProvider.Security;
using EList.Models.Events;
using LinqToDB;
using LinqToDB.Async;

namespace EList.DbDataProvider.DataProviders
{
    public class SubscriptionsDataProvider : DataProviderBase, ISubscriptionsDataProvider
    {
        private readonly IFieldEncryptor _fieldEncryptor;

        public SubscriptionsDataProvider(
            IDataConnectionProvider dataConnectionProvider,
            IFieldEncryptor fieldEncryptor) : base(dataConnectionProvider)
        {
            _fieldEncryptor = fieldEncryptor;
        }

        public async Task<ListResponse<SubscriptionDto>> GetSubscriptionsAsync(SubscriptionsSearchRequest request)
        {
            var subscriptionsRequest = _connection.Subscriptions
                .LoadWith(i => i.SubscribedTo)
                .ThenLoad(i => i.PersonInfo)
                .OrderBy(i => i.SubscribedTo.Login)
                .Where(i => i.SubscriberId == request.AccountId);

            List<SubscriptionDto> result;
            int count;

            if (string.IsNullOrWhiteSpace(request.Name))
            {
                count = await subscriptionsRequest.CountAsync();
                if (request.PageIndes != null && request.PageSize != null)
                    result = await subscriptionsRequest.Skip(request.PageSize.Value * request.PageIndes.Value).Take(request.PageSize.Value).ToListAsync();
                else
                    result = await subscriptionsRequest.ToListAsync();
            }
            else
            {
                var all = await subscriptionsRequest.ToListAsync();
                foreach (var item in all)
                    PersonalDataCrypto.DecryptPerson(item.SubscribedTo?.PersonInfo, _fieldEncryptor);

                var nameSubstrings = request.Name.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                all = all.Where(i => nameSubstrings.All(name =>
                {
                    var n = name.ToLowerInvariant();
                    return (i.SubscribedTo?.Login?.ToLowerInvariant().Contains(n) ?? false)
                        || (i.SubscribedTo?.PersonInfo?.LastName?.ToLowerInvariant().Contains(n) ?? false)
                        || (i.SubscribedTo?.PersonInfo?.Patronymic?.ToLowerInvariant().Contains(n) ?? false)
                        || (i.SubscribedTo?.PersonInfo?.FirstName?.ToLowerInvariant().Contains(n) ?? false);
                })).ToList();

                count = all.Count;
                if (request.PageIndes != null && request.PageSize != null)
                    result = all.Skip(request.PageSize.Value * request.PageIndes.Value).Take(request.PageSize.Value).ToList();
                else
                    result = all;
            }

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
                .Where(i => i.SubscribedToId == request.AccountId);

            if (request.NotifyParticipated != null)
                subscriptionsRequest = subscriptionsRequest.Where(i => i.NotifyParticipated == request.NotifyParticipated);

            if (request.NotifyEventCreated != null)
                subscriptionsRequest = subscriptionsRequest.Where(i => i.NotifyEventCreated == request.NotifyEventCreated);

            if (request.NotifySubscribed != null)
                subscriptionsRequest = subscriptionsRequest.Where(i => i.NotifySubscribed == request.NotifySubscribed);

            List<SubscriptionDto> result;
            int count;

            if (string.IsNullOrWhiteSpace(request.Name))
            {
                count = await subscriptionsRequest.CountAsync();
                if (request.PageIndes != null && request.PageSize != null)
                    result = await subscriptionsRequest.Skip(request.PageSize.Value * request.PageIndes.Value).Take(request.PageSize.Value).ToListAsync();
                else
                    result = await subscriptionsRequest.ToListAsync();
            }
            else
            {
                var all = await subscriptionsRequest.ToListAsync();
                foreach (var item in all)
                    PersonalDataCrypto.DecryptPerson(item.Subscriber?.PersonInfo, _fieldEncryptor);

                var nameSubstrings = request.Name.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                all = all.Where(i => nameSubstrings.All(name =>
                {
                    var n = name.ToLowerInvariant();
                    return (i.Subscriber?.Login?.ToLowerInvariant().Contains(n) ?? false)
                        || (i.Subscriber?.PersonInfo?.LastName?.ToLowerInvariant().Contains(n) ?? false)
                        || (i.Subscriber?.PersonInfo?.Patronymic?.ToLowerInvariant().Contains(n) ?? false)
                        || (i.Subscriber?.PersonInfo?.FirstName?.ToLowerInvariant().Contains(n) ?? false);
                })).ToList();

                all = all
                    .OrderBy(i => i.Subscriber?.PersonInfo?.Patronymic)
                    .ThenBy(i => i.Subscriber?.PersonInfo?.LastName)
                    .ThenBy(i => i.Subscriber?.PersonInfo?.FirstName)
                    .ToList();

                count = all.Count;
                if (request.PageIndes != null && request.PageSize != null)
                    result = all.Skip(request.PageSize.Value * request.PageIndes.Value).Take(request.PageSize.Value).ToList();
                else
                    result = all;
            }

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
