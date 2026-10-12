using AutoMapper;
using EList.Common.Models;
using EList.DbDataProvider.Interfaces;
using EList.Models.Accounts;
using EList.Models.Person;
using EList.Models.Subscriptions;
using EList.Repositories.Interfaces;

namespace EList.Repositories.Impl
{
    public class OrganizationSubscriptionsRepository : IOrganizationSubscriptionsRepository
    {
        private readonly IOrganizationSubscriptionsDataProvider _dataProvider;
        private readonly IMapper _mapper;

        public OrganizationSubscriptionsRepository(
            IOrganizationSubscriptionsDataProvider dataProvider,
            IMapper mapper)
        {
            _dataProvider = dataProvider;
            _mapper = mapper;
        }

        public Task SubscribeAsync(Guid subscriberId, Guid organizationId)
            => _dataProvider.SubscribeAsync(subscriberId, organizationId);

        public Task UpdateAsync(Guid subscriberId, Guid organizationId, bool notifyEventCreated)
            => _dataProvider.UpdateAsync(subscriberId, organizationId, notifyEventCreated);

        public Task DeleteAsync(Guid subscriberId, Guid organizationId)
            => _dataProvider.DeleteAsync(subscriberId, organizationId);

        public Task DeleteAllForAccountAsync(Guid accountId)
            => _dataProvider.DeleteAllForAccountAsync(accountId);

        public Task<bool> IsSubscriptionExistAsync(Guid subscriberId, Guid organizationId)
            => _dataProvider.IsSubscriptionExistAsync(subscriberId, organizationId);

        public async Task<PagedList<OrganizationSubscription>?> GetSubscriptionsAsync(
            OrganizationSubscriptionsSearchRequest request)
        {
            var mapped = MapRequest(request);
            var result = await _dataProvider.GetSubscriptionsAsync(mapped);
            var list = result.Items?.Select(i => new OrganizationSubscription
            {
                Id = i.Id,
                NotifyEventCreated = i.NotifyEventCreated,
                Organization = i.Organization == null ? null : new OrganizationSubscriptionTarget
                {
                    Id = i.Organization.Id,
                    Name = i.Organization.Name,
                    Active = i.Organization.Active
                }
            }).ToList();

            return new PagedList<OrganizationSubscription>(result.TotalCount, list, 0, 0);
        }

        public async Task<PagedList<OrganizationSubscription>?> GetSubscribersAsync(
            OrganizationSubscriptionsSearchRequest request)
        {
            var mapped = MapRequest(request);
            var result = await _dataProvider.GetSubscribersAsync(mapped);
            var list = result.Items?.Select(i => new OrganizationSubscription
            {
                Id = i.Id,
                NotifyEventCreated = i.NotifyEventCreated,
                Subscriber = new Subscriber
                {
                    Account = _mapper.Map<AccountPublicData>(i.Subscriber),
                    PersonInfo = _mapper.Map<PersonInfo>(i.Subscriber?.PersonInfo)
                }
            }).ToList();

            return new PagedList<OrganizationSubscription>(result.TotalCount, list, 0, 0);
        }

        public async Task<List<Guid>> GetSubscriberIdsAsync(OrganizationSubscriptionsSearchRequest request)
        {
            return await _dataProvider.GetSubscriberIdsAsync(MapRequest(request));
        }

        public Task<int> GetSubscriptionsCountAsync(Guid accountId)
            => _dataProvider.GetSubscriptionsCountAsync(accountId);

        public Task<int> GetSubscribersCountAsync(Guid organizationId)
            => _dataProvider.GetSubscribersCountAsync(organizationId);

        private static DbDataProvider.Models.SearchRequests.OrganizationSubscriptionsSearchRequest MapRequest(
            OrganizationSubscriptionsSearchRequest request)
        {
            return new DbDataProvider.Models.SearchRequests.OrganizationSubscriptionsSearchRequest
            {
                AccountId = request.AccountId,
                OrganizationId = request.OrganizationId,
                Name = request.Name,
                NotifyEventCreated = request.NotifyEventCreated,
                PageIndes = request.PageIndes,
                PageSize = request.PageSize
            };
        }
    }
}
