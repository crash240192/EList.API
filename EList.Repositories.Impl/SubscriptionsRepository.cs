using AutoMapper;
using EList.DbDataProvider.Interfaces;
using EList.DbDataProvider.Models;
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

        public async Task<List<Subscription>?> GetSubscriptionsAsync(Guid accountId)
        {
            var result = await _subscriptionsDataProvider.GetSubscriptionsAsync(accountId);
            var subscriptions = result?.Select(i => _mapper.Map<Subscription>(i))?.ToList();
            return subscriptions;
        }

        public async Task<bool> IsSubscriptionExistAsync(Guid subscriberId, Guid subscribedToId)
        {
            var result = await _subscriptionsDataProvider.IsSubscriptionExistAsync(subscriberId, subscribedToId);
            return result;
        }

        public async Task<List<Subscription>?> GetSubscribersAsync(Guid accountId, bool? notifyParticipated = null, bool? notifyEventCreated = false, bool? notifySubscribed = false)
        { 
            var result = await _subscriptionsDataProvider.GetSubscribersAsync(accountId);
            var subscriptions = result?.Select(i => _mapper.Map<Subscription>(i))?.ToList();
            return subscriptions;
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
