using EList.DbDataProvider.Models;
using EList.DbDataProvider.Models.Enums;

namespace EList.DbDataProvider.Interfaces
{
    public interface IOrganizationsDataProvider
    {
        #region organizations
        Task<Guid> CreateOrganizationAsync(OrganizationDto item);
        Task<OrganizationDto?> GetOrganizationAsync(Guid id);
        Task<OrganizationDto?> GetOrganizationFullAsync(Guid id);
        Task UpdateOrganizationAsync(OrganizationDto item);
        Task SetOrganizationActiveAsync(Guid organizationId, bool active);
        Task SetOrganizationWalletAsync(Guid organizationId, Guid? walletId);
        Task SetVerificationStatusAsync(Guid organizationId, OrganizationVerificationStatus status);
        Task SetCanSellTicketsAsync(Guid organizationId, bool canSellTickets);
        Task<List<OrganizationDto>> GetOrganizationsByAccountIdAsync(Guid accountId, bool onlyActiveMembers = true);
        Task<List<OrganizationDto>> GetOrganizationsByCreatedByAsync(Guid accountId);
        #endregion

        #region members
        Task<Guid> AddMemberAsync(OrganizationAccountRelationDto item);
        Task<OrganizationAccountRelationDto?> GetMemberAsync(Guid organizationId, Guid accountId);
        Task<OrganizationAccountRelationDto?> GetMemberByIdAsync(Guid id);
        Task<List<OrganizationAccountRelationDto>> GetMembersByOrganizationIdAsync(Guid organizationId, bool onlyActive = true);
        Task UpdateMemberRoleAsync(Guid organizationId, Guid accountId, OrganizationMemberRole role);
        Task SetMemberActiveAsync(Guid organizationId, Guid accountId, bool active);
        Task RemoveMemberAsync(Guid organizationId, Guid accountId);
        Task<bool> IsActiveMemberAsync(Guid organizationId, Guid accountId);
        Task<bool> IsOwnerAsync(Guid organizationId, Guid accountId);
        Task<bool> IsOwnerOrManagerAsync(Guid organizationId, Guid accountId);
        Task TransferOwnershipAsync(Guid organizationId, Guid currentOwnerAccountId, Guid newOwnerAccountId);
        #endregion

        #region legal
        Task UpsertLegalAsync(OrganizationLegalDto item);
        Task<OrganizationLegalDto?> GetLegalAsync(Guid organizationId);
        Task DeleteLegalAsync(Guid organizationId);
        #endregion

        #region payout
        Task UpsertPayoutAsync(OrganizationPayoutDto item);
        Task<OrganizationPayoutDto?> GetPayoutAsync(Guid organizationId);
        Task SetProviderOnboardingAsync(Guid organizationId, PaymentProvider? provider, string? providerSellerId, ProviderOnboardingStatus status);
        Task DeletePayoutAsync(Guid organizationId);
        #endregion
    }
}
