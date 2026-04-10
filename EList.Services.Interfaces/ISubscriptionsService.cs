using EList.Common.Models;
using EList.Models.Subscriptions;

namespace EList.Services.Interfaces
{
    public interface ISubscriptionsService
    {
        Task<CommandResult> SubscribeToAccountAsync(Guid subscribedToId);
        Task<CommandResult<List<Subscription>?>> GetSubscriptionsAsync();
        Task<CommandResult> UpdateSubscriptionAsync(Guid subscribedToId, UpdateSubscriptionRequestBase request);
        Task<CommandResult<List<Subscription>?>> GetSubscribersAsync();
        Task<CommandResult> DeleteSubscriptionAsync(Guid subscribedToId);
    }
}
