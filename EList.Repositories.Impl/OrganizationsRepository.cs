using AutoMapper;
using EList.DbDataProvider.Interfaces;
using EList.DbDataProvider.Models;
using EList.Models.Accounts;
using EList.Models.Enums;
using EList.Models.Organizations;
using EList.Models.Person;
using EList.Repositories.Interfaces;

namespace EList.Repositories.Impl
{
    public class OrganizationsRepository : IOrganizationsRepository
    {
        private readonly IOrganizationsDataProvider _organizationsDataProvider;
        private readonly IMapper _mapper;

        public OrganizationsRepository(IOrganizationsDataProvider organizationsDataProvider,
            IMapper mapper)
        {
            _organizationsDataProvider = organizationsDataProvider;
            _mapper = mapper;
        }

        #region organizations
        public async Task<Guid> CreateOrganizationAsync(Organization item)
        {
            var mappedItem = _mapper.Map<OrganizationDto>(item);
            var result = await _organizationsDataProvider.CreateOrganizationAsync(mappedItem);
            return result;
        }

        public async Task<Organization?> GetOrganizationAsync(Guid id)
        {
            var item = await _organizationsDataProvider.GetOrganizationAsync(id);
            var result = _mapper.Map<Organization>(item);
            return result;
        }

        public async Task<Organization?> GetOrganizationFullAsync(Guid id)
        {
            var item = await _organizationsDataProvider.GetOrganizationFullAsync(id);
            if (item == null)
                return null;

            var result = _mapper.Map<Organization>(item);
            result.Members = MapMembers(item.Members);
            return result;
        }

        public async Task UpdateOrganizationAsync(Organization item)
        {
            var mappedItem = _mapper.Map<OrganizationDto>(item);
            await _organizationsDataProvider.UpdateOrganizationAsync(mappedItem);
        }

        public async Task SetOrganizationActiveAsync(Guid organizationId, bool active)
        {
            await _organizationsDataProvider.SetOrganizationActiveAsync(organizationId, active);
        }

        public async Task SetOrganizationWalletAsync(Guid organizationId, Guid? walletId)
        {
            await _organizationsDataProvider.SetOrganizationWalletAsync(organizationId, walletId);
        }

        public async Task SetVerificationStatusAsync(Guid organizationId, OrganizationVerificationStatus status, string? rejectReason = null)
        {
            var mappedStatus = _mapper.Map<DbDataProvider.Models.Enums.OrganizationVerificationStatus>(status);
            await _organizationsDataProvider.SetVerificationStatusAsync(organizationId, mappedStatus, rejectReason);
        }

        public async Task SetCanSellTicketsAsync(Guid organizationId, bool canSellTickets)
        {
            await _organizationsDataProvider.SetCanSellTicketsAsync(organizationId, canSellTickets);
        }

        public async Task<List<Organization>> GetOrganizationsByAccountIdAsync(Guid accountId, bool onlyActiveMembers = true)
        {
            var items = await _organizationsDataProvider.GetOrganizationsByAccountIdAsync(accountId, onlyActiveMembers);
            var result = _mapper.Map<List<Organization>>(items);
            return result;
        }

        public async Task<List<Organization>> GetOrganizationsByCreatedByAsync(Guid accountId)
        {
            var items = await _organizationsDataProvider.GetOrganizationsByCreatedByAsync(accountId);
            var result = _mapper.Map<List<Organization>>(items);
            return result;
        }

        public async Task<List<Organization>> GetPendingVerificationOrganizationsAsync(int limit = 100)
        {
            var items = await _organizationsDataProvider.GetPendingVerificationOrganizationsAsync(limit);
            var result = new List<Organization>();
            foreach (var item in items)
            {
                var organization = _mapper.Map<Organization>(item);
                organization.Legal = _mapper.Map<OrganizationLegal>(item.Legal);
                result.Add(organization);
            }
            return result;
        }
        #endregion

        #region members
        public async Task<Guid> AddMemberAsync(OrganizationMember item)
        {
            var mappedItem = _mapper.Map<OrganizationAccountRelationDto>(item);
            var result = await _organizationsDataProvider.AddMemberAsync(mappedItem);
            return result;
        }

        public async Task<OrganizationMember?> GetMemberAsync(Guid organizationId, Guid accountId)
        {
            var item = await _organizationsDataProvider.GetMemberAsync(organizationId, accountId);
            return MapMember(item);
        }

        public async Task<OrganizationMember?> GetMemberByIdAsync(Guid id)
        {
            var item = await _organizationsDataProvider.GetMemberByIdAsync(id);
            return MapMember(item);
        }

        public async Task<List<OrganizationMember>> GetMembersByOrganizationIdAsync(Guid organizationId, bool onlyActive = true)
        {
            var items = await _organizationsDataProvider.GetMembersByOrganizationIdAsync(organizationId, onlyActive);
            return MapMembers(items);
        }

        public async Task UpdateMemberRoleAsync(Guid organizationId, Guid accountId, OrganizationMemberRole role)
        {
            var mappedRole = _mapper.Map<DbDataProvider.Models.Enums.OrganizationMemberRole>(role);
            await _organizationsDataProvider.UpdateMemberRoleAsync(organizationId, accountId, mappedRole);
        }

        public async Task SetMemberActiveAsync(Guid organizationId, Guid accountId, bool active)
        {
            await _organizationsDataProvider.SetMemberActiveAsync(organizationId, accountId, active);
        }

        public async Task RemoveMemberAsync(Guid organizationId, Guid accountId)
        {
            await _organizationsDataProvider.RemoveMemberAsync(organizationId, accountId);
        }

        public async Task<bool> IsActiveMemberAsync(Guid organizationId, Guid accountId)
        {
            return await _organizationsDataProvider.IsActiveMemberAsync(organizationId, accountId);
        }

        public async Task<bool> IsOwnerAsync(Guid organizationId, Guid accountId)
        {
            return await _organizationsDataProvider.IsOwnerAsync(organizationId, accountId);
        }

        public async Task<bool> IsOwnerOrManagerAsync(Guid organizationId, Guid accountId)
        {
            return await _organizationsDataProvider.IsOwnerOrManagerAsync(organizationId, accountId);
        }

        public async Task TransferOwnershipAsync(Guid organizationId, Guid currentOwnerAccountId, Guid newOwnerAccountId)
        {
            await _organizationsDataProvider.TransferOwnershipAsync(organizationId, currentOwnerAccountId, newOwnerAccountId);
        }
        #endregion

        #region legal
        public async Task UpsertLegalAsync(OrganizationLegal item)
        {
            var mappedItem = _mapper.Map<OrganizationLegalDto>(item);
            await _organizationsDataProvider.UpsertLegalAsync(mappedItem);
        }

        public async Task<OrganizationLegal?> GetLegalAsync(Guid organizationId)
        {
            var item = await _organizationsDataProvider.GetLegalAsync(organizationId);
            var result = _mapper.Map<OrganizationLegal>(item);
            return result;
        }

        public async Task DeleteLegalAsync(Guid organizationId)
        {
            await _organizationsDataProvider.DeleteLegalAsync(organizationId);
        }
        #endregion

        #region payout
        public async Task UpsertPayoutAsync(OrganizationPayout item)
        {
            var mappedItem = _mapper.Map<OrganizationPayoutDto>(item);
            await _organizationsDataProvider.UpsertPayoutAsync(mappedItem);
        }

        public async Task<OrganizationPayout?> GetPayoutAsync(Guid organizationId)
        {
            var item = await _organizationsDataProvider.GetPayoutAsync(organizationId);
            var result = _mapper.Map<OrganizationPayout>(item);
            return result;
        }

        public async Task SetProviderOnboardingAsync(Guid organizationId, PaymentProvider? provider, string? providerSellerId, ProviderOnboardingStatus status)
        {
            var mappedProvider = provider != null
                ? _mapper.Map<DbDataProvider.Models.Enums.PaymentProvider?>(provider)
                : null;
            var mappedStatus = _mapper.Map<DbDataProvider.Models.Enums.ProviderOnboardingStatus>(status);
            await _organizationsDataProvider.SetProviderOnboardingAsync(organizationId, mappedProvider, providerSellerId, mappedStatus);
        }

        public async Task DeletePayoutAsync(Guid organizationId)
        {
            await _organizationsDataProvider.DeletePayoutAsync(organizationId);
        }
        #endregion

        private OrganizationMember? MapMember(OrganizationAccountRelationDto? item)
        {
            if (item == null)
                return null;

            var result = _mapper.Map<OrganizationMember>(item);
            result.Account = item.Account != null ? _mapper.Map<AccountPublicData>(item.Account) : null;
            result.PersonInfo = item.Account?.PersonInfo != null ? _mapper.Map<PersonInfo>(item.Account.PersonInfo) : null;
            return result;
        }

        private List<OrganizationMember> MapMembers(List<OrganizationAccountRelationDto>? items)
        {
            return items?.Select(MapMember).Where(i => i != null).Cast<OrganizationMember>().ToList()
                ?? new List<OrganizationMember>();
        }
    }
}
