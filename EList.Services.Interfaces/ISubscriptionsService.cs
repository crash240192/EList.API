using EList.Common.Models;
using EList.Models.Subscriptions;

namespace EList.Services.Interfaces
{
    public interface ISubscriptionsService
    {
        Task<CommandResult> SubscribeToAccountAsync(Guid subscribedToId);
        Task<CommandResult<PagedList<Subscription>?>> GetSubscriptionsAsync(SubscriptionsSearchRequest request);
        Task<CommandResult<int>> GetSubscriptionsCountAsync(Guid accountId);
        Task<CommandResult<PagedList<Subscription>?>> GetSubscribersAsync(SubscriptionsSearchRequest request);
        Task<CommandResult<int>> GetSubscribersCountAsync(Guid accountId);
        Task<CommandResult> UpdateSubscriptionAsync(Guid subscribedToId, UpdateSubscriptionRequestBase request);        
        Task<CommandResult> DeleteSubscriptionAsync(Guid subscribedToId);
    }
}
