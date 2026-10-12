using EList.Common.Models;
using EList.Models.Subscriptions;

namespace EList.Services.Interfaces
{
    public interface IOrganizationSubscriptionsService
    {
        Task<CommandResult> SubscribeAsync(Guid organizationId);
        Task<CommandResult> UpdateAsync(Guid organizationId, UpdateOrganizationSubscriptionRequest request);
        Task<CommandResult> DeleteAsync(Guid organizationId);
        Task<CommandResult<bool>> IsSubscribedAsync(Guid organizationId);
        Task<CommandResult<PagedList<OrganizationSubscription>?>> GetSubscriptionsAsync(OrganizationSubscriptionsSearchRequest request);
        Task<CommandResult<int>> GetSubscriptionsCountAsync(Guid accountId);
        Task<CommandResult<PagedList<OrganizationSubscription>?>> GetSubscribersAsync(OrganizationSubscriptionsSearchRequest request);
        Task<CommandResult<int>> GetSubscribersCountAsync(Guid organizationId);
    }
}
