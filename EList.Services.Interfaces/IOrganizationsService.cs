using EList.Common.Models;
using EList.Models.Organizations;

namespace EList.Services.Interfaces
{
    public interface IOrganizationsService
    {
        Task<CommandResult<Guid?>> CreateOrganizationAsync(OrganizationRequest request);
        Task<CommandResult<OrganizationResponse?>> GetOrganizationAsync(Guid organizationId);
        Task<CommandResult<List<OrganizationResponse>?>> GetMyOrganizationsAsync();
        Task<CommandResult> UpdateOrganizationAsync(Guid organizationId, OrganizationRequest request);
        Task<CommandResult> SetOrganizationActiveAsync(Guid organizationId, bool active);

        Task<CommandResult<List<OrganizationMemberResponse>?>> GetMembersAsync(Guid organizationId);
        Task<CommandResult<Guid?>> AddManagerAsync(Guid organizationId, AddOrganizationMemberRequest request);
        Task<CommandResult> RemoveMemberAsync(Guid organizationId, Guid accountId);
        Task<CommandResult> SetMemberActiveAsync(Guid organizationId, Guid accountId, bool active);
        Task<CommandResult> TransferOwnershipAsync(Guid organizationId, TransferOwnershipRequest request);

        Task<CommandResult> UpsertLegalAsync(Guid organizationId, OrganizationLegalRequest request);
        Task<CommandResult<OrganizationLegalResponse?>> GetLegalAsync(Guid organizationId);
        Task<CommandResult> UpsertPayoutAsync(Guid organizationId, OrganizationPayoutRequest request);
        Task<CommandResult<OrganizationPayoutResponse?>> GetPayoutAsync(Guid organizationId);

        Task<CommandResult> SubmitVerificationAsync(Guid organizationId);
        Task<CommandResult> SetCanSellTicketsAsync(Guid organizationId, bool canSellTickets);
    }
}
