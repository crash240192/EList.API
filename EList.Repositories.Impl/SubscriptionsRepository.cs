using AutoMapper;
using EList.Common.Models;
using EList.DbDataProvider.Interfaces;
using EList.DbDataProvider.Models;
using EList.Models.Accounts;
using EList.Models.Person;
using EList.Models.Subscriptions;
using EList.Repositories.Interfaces;

namespace EList.Repositories.Impl
{
    public class SubscriptionsRepository : ISubscriptionsRepository
    {
        private readonly ISubscriptionsDataProvider _subscriptionsDataProvider;
        private readonly IMapper _mapper;

        public SubscriptionsRepository(ISubscriptionsDataProvider subscriptionsDataProvider,
            IMapper mapper)
        {
            _subscriptionsDataProvider = subscriptionsDataProvider;
            _mapper = mapper;
        }

        public async Task SubscribeToAccountAsync(Guid subscriberId, Guid subscribeToId)
        {
            await _subscriptionsDataProvider.SubscribeToAccountAsync(subscriberId, subscribeToId);
        }

        public async Task<PagedList<Subscription>?> GetSubscriptionsAsync(Guid accountId)
        {
            var subscriptionsResult = await _subscriptionsDataProvider.GetSubscriptionsAsync(accountId);
            var subscriptionsList = subscriptionsResult.Item2?.Select(i => new Subscription
            {
                NotifyEventCreated = i.NotifyEventCreated,
                NotifyParticipated = i.NotifyParticipated,
                NotifySubscribed = i.NotifySubscribed,
                Subscriber = null,
                SubscribedTo = new Subscriber
                {
                    Account = _mapper.Map<Account>(i.SubscribedTo),
                    PersonInfo = _mapper.Map<PersonInfo>(i.SubscribedTo.PersonInfo)
                }
            })?.ToList();

            var result = new PagedList<Subscription>(subscriptionsResult.Item1, subscriptionsList, 0, 0);

            return result;
        }

        public async Task<int> GetSubscriptionsCountAsync(Guid accountId)
        {
            var result = await _subscriptionsDataProvider.GetSubscriptionsCountAsync(accountId);

            return result;
        }

        public async Task<bool> IsSubscriptionExistAsync(Guid subscriberId, Guid subscribedToId)
        {
            var result = await _subscriptionsDataProvider.IsSubscriptionExistAsync(subscriberId, subscribedToId);
            return result;
        }

        public async Task<PagedList<Subscription>?> GetSubscribersAsync(Guid accountId, bool? notifyParticipated = null, bool? notifyEventCreated = false, bool? notifySubscribed = false)
        { 
            var subscriptionsResult = await _subscriptionsDataProvider.GetSubscribersAsync(accountId);

            var subscriptionsList = subscriptionsResult.Item2?.Select(i => new Subscription
            {
                NotifyEventCreated = i.NotifyEventCreated,
                NotifyParticipated = i.NotifyParticipated,
                NotifySubscribed = i.NotifySubscribed,
                Subscriber = new Subscriber
                {
                    Account = _mapper.Map<Account>(i.Subscriber),
                    PersonInfo = _mapper.Map<PersonInfo>(i.Subscriber.PersonInfo)
                },
                SubscribedTo = null
            })?.ToList();
            var result = new PagedList<Subscription>(subscriptionsResult.Item1, subscriptionsList, 0, 0);

            return result;
        }

        public async Task<int> GetSubscribersCountAsync(Guid accountId)
        { 
            var result = await _subscriptionsDataProvider.GetSubscribersCountAsync(accountId);
            return result;
        }

        public async Task DeleteSubscriptionAsync(Guid subscriberId, Guid subscribedToId)
        {
            await _subscriptionsDataProvider.DeleteSubscriptionAsync(subscriberId, subscribedToId);
        }

        public async Task UpdateSubscriptionAsync(UpdateSubscriptionRequest request)
        {
            var mappedRequest = new SubscriptionDto
            { 
                NotifyEventCreated = request.NotifyEventCreated,
                NotifyParticipated = request.NotifyParticipated,
                NotifySubscribed = request.NotifySubscribed,
            };
            await _subscriptionsDataProvider.UpdateSubscriptionAsync(mappedRequest);
        }
    }
}
