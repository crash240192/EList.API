using EList.Models.Enums;
using EList.Models.Organizations;

namespace EList.Repositories.Interfaces
{
    public interface IOrganizationsRepository
    {
        #region organizations
        Task<Guid> CreateOrganizationAsync(Organization item);
        Task<Organization?> GetOrganizationAsync(Guid id);
        Task<Organization?> GetOrganizationFullAsync(Guid id);
        Task UpdateOrganizationAsync(Organization item);
        Task SetOrganizationActiveAsync(Guid organizationId, bool active);
        Task SetOrganizationWalletAsync(Guid organizationId, Guid? walletId);
        Task SetVerificationStatusAsync(Guid organizationId, OrganizationVerificationStatus status);
        Task SetCanSellTicketsAsync(Guid organizationId, bool canSellTickets);
        Task<List<Organization>> GetOrganizationsByAccountIdAsync(Guid accountId, bool onlyActiveMembers = true);
        Task<List<Organization>> GetOrganizationsByCreatedByAsync(Guid accountId);
        #endregion

        #region members
        Task<Guid> AddMemberAsync(OrganizationMember item);
        Task<OrganizationMember?> GetMemberAsync(Guid organizationId, Guid accountId);
        Task<OrganizationMember?> GetMemberByIdAsync(Guid id);
        Task<List<OrganizationMember>> GetMembersByOrganizationIdAsync(Guid organizationId, bool onlyActive = true);
        Task UpdateMemberRoleAsync(Guid organizationId, Guid accountId, OrganizationMemberRole role);
        Task SetMemberActiveAsync(Guid organizationId, Guid accountId, bool active);
        Task RemoveMemberAsync(Guid organizationId, Guid accountId);
        Task<bool> IsActiveMemberAsync(Guid organizationId, Guid accountId);
        Task<bool> IsOwnerAsync(Guid organizationId, Guid accountId);
        Task<bool> IsOwnerOrManagerAsync(Guid organizationId, Guid accountId);
        Task TransferOwnershipAsync(Guid organizationId, Guid currentOwnerAccountId, Guid newOwnerAccountId);
        #endregion

        #region legal
        Task UpsertLegalAsync(OrganizationLegal item);
        Task<OrganizationLegal?> GetLegalAsync(Guid organizationId);
        Task DeleteLegalAsync(Guid organizationId);
        #endregion

        #region payout
        Task UpsertPayoutAsync(OrganizationPayout item);
        Task<OrganizationPayout?> GetPayoutAsync(Guid organizationId);
        Task SetProviderOnboardingAsync(Guid organizationId, PaymentProvider? provider, string? providerSellerId, ProviderOnboardingStatus status);
        Task DeletePayoutAsync(Guid organizationId);
        #endregion
    }
}
