using EList.DbDataProvider.Interfaces;
using EList.DbDataProvider.Models;
using EList.DbDataProvider.Models.SearchRequests;
using EList.DbDataProvider.Security;
using EList.Models.Events;
using LinqToDB;
using LinqToDB.Async;

namespace EList.DbDataProvider.DataProviders
{
    public class OrganizationSubscriptionsDataProvider : DataProviderBase, IOrganizationSubscriptionsDataProvider
    {
        private readonly IFieldEncryptor _fieldEncryptor;

        public OrganizationSubscriptionsDataProvider(
            IDataConnectionProvider dataConnectionProvider,
            IFieldEncryptor fieldEncryptor) : base(dataConnectionProvider)
        {
            _fieldEncryptor = fieldEncryptor;
        }

        public async Task<Guid> SubscribeAsync(Guid subscriberId, Guid organizationId)
        {
            var item = new OrganizationSubscriptionDto
            {
                SubscriberId = subscriberId,
                OrganizationId = organizationId,
                NotifyEventCreated = true
            };
            return (Guid)await _connection.InsertWithIdentityAsync(item);
        }

        public async Task UpdateAsync(Guid subscriberId, Guid organizationId, bool notifyEventCreated)
        {
            await _connection.OrganizationSubscriptions
                .Where(i => i.SubscriberId == subscriberId && i.OrganizationId == organizationId)
                .Set(i => i.NotifyEventCreated, notifyEventCreated)
                .UpdateAsync();
        }

        public async Task DeleteAsync(Guid subscriberId, Guid organizationId)
        {
            await _connection.OrganizationSubscriptions
                .DeleteAsync(i => i.SubscriberId == subscriberId && i.OrganizationId == organizationId);
        }

        public async Task DeleteAllForAccountAsync(Guid accountId)
        {
            await _connection.OrganizationSubscriptions
                .DeleteAsync(i => i.SubscriberId == accountId);
        }

        public async Task DeleteAllForOrganizationAsync(Guid organizationId)
        {
            await _connection.OrganizationSubscriptions
                .DeleteAsync(i => i.OrganizationId == organizationId);
        }

        public async Task<bool> IsSubscriptionExistAsync(Guid subscriberId, Guid organizationId)
        {
            return await _connection.OrganizationSubscriptions
                .AnyAsync(i => i.SubscriberId == subscriberId && i.OrganizationId == organizationId);
        }

        public async Task<ListResponse<OrganizationSubscriptionDto>> GetSubscriptionsAsync(
            OrganizationSubscriptionsSearchRequest request)
        {
            var query = _connection.OrganizationSubscriptions
                .LoadWith(i => i.Organization)
                .Where(i => i.SubscriberId == request.AccountId);

            if (request.NotifyEventCreated != null)
                query = query.Where(i => i.NotifyEventCreated == request.NotifyEventCreated);

            query = query.OrderBy(i => i.Organization.Name);

            List<OrganizationSubscriptionDto> result;
            int count;

            if (string.IsNullOrWhiteSpace(request.Name))
            {
                count = await query.CountAsync();
                result = await ApplyPagingAsync(query, request);
            }
            else
            {
                var all = await query.ToListAsync();
                var nameSubstrings = request.Name.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                all = all.Where(i => nameSubstrings.All(name =>
                {
                    var n = name.ToLowerInvariant();
                    return i.Organization?.Name?.ToLowerInvariant().Contains(n) ?? false;
                })).ToList();

                count = all.Count;
                result = ApplyPagingInMemory(all, request);
            }

            return new ListResponse<OrganizationSubscriptionDto>(count, result);
        }

        public async Task<ListResponse<OrganizationSubscriptionDto>> GetSubscribersAsync(
            OrganizationSubscriptionsSearchRequest request)
        {
            var query = _connection.OrganizationSubscriptions
                .LoadWith(i => i.Subscriber)
                .ThenLoad(i => i.PersonInfo)
                .Where(i => i.OrganizationId == request.OrganizationId);

            if (request.NotifyEventCreated != null)
                query = query.Where(i => i.NotifyEventCreated == request.NotifyEventCreated);

            List<OrganizationSubscriptionDto> result;
            int count;

            if (string.IsNullOrWhiteSpace(request.Name))
            {
                count = await query.CountAsync();
                result = await ApplyPagingAsync(query, request);
                foreach (var item in result)
                    PersonalDataCrypto.DecryptPerson(item.Subscriber?.PersonInfo, _fieldEncryptor);
            }
            else
            {
                var all = await query.ToListAsync();
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
                }))
                .OrderBy(i => i.Subscriber?.PersonInfo?.LastName)
                .ThenBy(i => i.Subscriber?.PersonInfo?.FirstName)
                .ThenBy(i => i.Subscriber?.Login)
                .ToList();

                count = all.Count;
                result = ApplyPagingInMemory(all, request);
            }

            return new ListResponse<OrganizationSubscriptionDto>(count, result);
        }

        public async Task<List<Guid>> GetSubscriberIdsAsync(OrganizationSubscriptionsSearchRequest request)
        {
            var query = _connection.OrganizationSubscriptions
                .Where(i => i.OrganizationId == request.OrganizationId);

            if (request.NotifyEventCreated != null)
                query = query.Where(i => i.NotifyEventCreated == request.NotifyEventCreated);

            return await query.Select(i => i.SubscriberId).ToListAsync();
        }

        public async Task<int> GetSubscriptionsCountAsync(Guid accountId)
        {
            return await _connection.OrganizationSubscriptions
                .Where(i => i.SubscriberId == accountId)
                .CountAsync();
        }

        public async Task<int> GetSubscribersCountAsync(Guid organizationId)
        {
            return await _connection.OrganizationSubscriptions
                .Where(i => i.OrganizationId == organizationId)
                .CountAsync();
        }

        private static async Task<List<OrganizationSubscriptionDto>> ApplyPagingAsync(
            IQueryable<OrganizationSubscriptionDto> query,
            OrganizationSubscriptionsSearchRequest request)
        {
            if (request.PageIndes != null && request.PageSize != null)
                query = query.Skip(request.PageSize.Value * request.PageIndes.Value).Take(request.PageSize.Value);
            return await query.ToListAsync();
        }

        private static List<OrganizationSubscriptionDto> ApplyPagingInMemory(
            List<OrganizationSubscriptionDto> all,
            OrganizationSubscriptionsSearchRequest request)
        {
            if (request.PageIndes != null && request.PageSize != null)
                return all.Skip(request.PageSize.Value * request.PageIndes.Value).Take(request.PageSize.Value).ToList();
            return all;
        }
    }
}
