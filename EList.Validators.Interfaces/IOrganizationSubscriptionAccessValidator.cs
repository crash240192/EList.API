using EList.Common.Models;

namespace EList.Validators.Interfaces
{
    public interface IOrganizationSubscriptionAccessValidator
    {
        Task<CommandResult> AssertCanSubscribeAsync(Guid subscriberId, Guid organizationId);
        Task<CommandResult> AssertCanManageOwnSubscriptionAsync(Guid subscriberId, Guid organizationId);
        Task<CommandResult> AssertCanViewOrganizationSubscribersAsync(Guid organizationId);
        Task<CommandResult> AssertCanViewAccountOrgSubscriptionsAsync(Guid accountId);
    }
}
