using EList.Common.Encryption;
using EList.DbDataProvider.Interfaces;
using EList.DbDataProvider.Models;
using EList.DbDataProvider.Models.Enums;
using EList.DbDataProvider.Security;
using LinqToDB;
using LinqToDB.Async;

namespace EList.DbDataProvider.DataProviders
{
    public class OrganizationsDataProvider : DataProviderBase, IOrganizationsDataProvider
    {
        private readonly IFieldEncryptor _fieldEncryptor;

        public OrganizationsDataProvider(
            IDataConnectionProvider dataConnectionProvider,
            IFieldEncryptor fieldEncryptor) : base(dataConnectionProvider)
        {
            _fieldEncryptor = fieldEncryptor;
        }

        #region organizations
        public async Task<Guid> CreateOrganizationAsync(OrganizationDto item)
        {
            item.CreateDate = DateTimeOffset.Now.ToUniversalTime();
            item.UpdateDate = DateTimeOffset.Now.ToUniversalTime();
            var id = (Guid)await _connection.InsertWithIdentityAsync(item);
            return id;
        }

        public async Task<OrganizationDto?> GetOrganizationAsync(Guid id)
        {
            var result = await _connection.Organizations.FirstOrDefaultAsync(i => i.Id == id);
            return result;
        }

        public async Task<OrganizationDto?> GetOrganizationFullAsync(Guid id)
        {
            var result = await _connection.Organizations
                .LoadWith(i => i.Members)
                .ThenLoad(i => i.Account)
                .ThenLoad(i => i.PersonInfo)
                .LoadWith(i => i.Legal)
                .LoadWith(i => i.Payout)
                .LoadWith(i => i.Wallet)
                .FirstOrDefaultAsync(i => i.Id == id);

            if (result?.Legal != null)
                PersonalDataCrypto.DecryptLegal(result.Legal, _fieldEncryptor);
            if (result?.Members != null)
            {
                foreach (var member in result.Members)
                    PersonalDataCrypto.DecryptPerson(member.Account?.PersonInfo, _fieldEncryptor);
            }

            return result;
        }

        public async Task UpdateOrganizationAsync(OrganizationDto item)
        {
            await _connection.Organizations.Where(i => i.Id == item.Id)
                .Set(i => i.Name, item.Name)
                .Set(i => i.Description, item.Description)
                .Set(i => i.Address, item.Address)
                .Set(i => i.Latitude, item.Latitude)
                .Set(i => i.Longitude, item.Longitude)
                .Set(i => i.UpdateDate, DateTimeOffset.Now.ToUniversalTime())
                .UpdateAsync();
        }

        public async Task SetOrganizationActiveAsync(Guid organizationId, bool active)
        {
            await _connection.Organizations.Where(i => i.Id == organizationId)
                .Set(i => i.Active, active)
                .Set(i => i.UpdateDate, DateTimeOffset.Now.ToUniversalTime())
                .UpdateAsync();
        }

        public async Task SetOrganizationWalletAsync(Guid organizationId, Guid? walletId)
        {
            await _connection.Organizations.Where(i => i.Id == organizationId)
                .Set(i => i.WalletId, walletId)
                .Set(i => i.UpdateDate, DateTimeOffset.Now.ToUniversalTime())
                .UpdateAsync();
        }

        public async Task SetVerificationStatusAsync(Guid organizationId, OrganizationVerificationStatus status, string? rejectReason = null)
        {
            var reason = status == OrganizationVerificationStatus.Rejected ? rejectReason : null;

            await _connection.Organizations.Where(i => i.Id == organizationId)
                .Set(i => i.VerificationStatus, status)
                .Set(i => i.VerificationRejectReason, reason)
                .Set(i => i.UpdateDate, DateTimeOffset.Now.ToUniversalTime())
                .UpdateAsync();

            if (status == OrganizationVerificationStatus.Verified)
            {
                await _connection.OrganizationLegal.Where(i => i.OrganizationId == organizationId)
                    .Set(i => i.VerifiedAt, DateTimeOffset.Now.ToUniversalTime())
                    .UpdateAsync();
            }
            else if (status == OrganizationVerificationStatus.Rejected
                || status == OrganizationVerificationStatus.Unverified
                || status == OrganizationVerificationStatus.Pending)
            {
                await _connection.OrganizationLegal.Where(i => i.OrganizationId == organizationId)
                    .Set(i => i.VerifiedAt, (DateTimeOffset?)null)
                    .UpdateAsync();
            }
        }

        public async Task<List<OrganizationDto>> GetPendingVerificationOrganizationsAsync(int limit = 100)
        {
            if (limit <= 0)
                limit = 100;

            var result = await _connection.Organizations
                .LoadWith(i => i.Legal)
                .Where(i => i.Active && i.VerificationStatus == OrganizationVerificationStatus.Pending)
                .OrderBy(i => i.UpdateDate)
                .Take(limit)
                .ToListAsync();
            return result;
        }

        public async Task SetCanSellTicketsAsync(Guid organizationId, bool canSellTickets)
        {
            await _connection.Organizations.Where(i => i.Id == organizationId)
                .Set(i => i.CanSellTickets, canSellTickets)
                .Set(i => i.UpdateDate, DateTimeOffset.Now.ToUniversalTime())
                .UpdateAsync();
        }

        public async Task<List<OrganizationDto>> GetOrganizationsByAccountIdAsync(Guid accountId, bool onlyActiveMembers = true)
        {
            var membersQuery = _connection.OrganizationMembers.Where(i => i.AccountId == accountId);
            if (onlyActiveMembers)
                membersQuery = membersQuery.Where(i => i.Active);

            var organizationIds = await membersQuery.Select(i => i.OrganizationId).ToListAsync();
            var result = await _connection.Organizations
                .Where(i => organizationIds.Contains(i.Id))
                .OrderBy(i => i.Name)
                .ToListAsync();
            return result;
        }

        public async Task<List<OrganizationDto>> GetOrganizationsByCreatedByAsync(Guid accountId)
        {
            var result = await _connection.Organizations
                .Where(i => i.CreatedByAccountId == accountId)
                .OrderByDescending(i => i.CreateDate)
                .ToListAsync();
            return result;
        }
        #endregion

        #region members
        public async Task<Guid> AddMemberAsync(OrganizationAccountRelationDto item)
        {
            item.JoinedAt = DateTimeOffset.Now.ToUniversalTime();
            var id = (Guid)await _connection.InsertWithIdentityAsync(item);
            return id;
        }

        public async Task<OrganizationAccountRelationDto?> GetMemberAsync(Guid organizationId, Guid accountId)
        {
            var result = await _connection.OrganizationMembers
                .LoadWith(i => i.Account)
                .ThenLoad(i => i.PersonInfo)
                .FirstOrDefaultAsync(i => i.OrganizationId == organizationId && i.AccountId == accountId);
            return result;
        }

        public async Task<OrganizationAccountRelationDto?> GetMemberByIdAsync(Guid id)
        {
            var result = await _connection.OrganizationMembers
                .LoadWith(i => i.Account)
                .ThenLoad(i => i.PersonInfo)
                .FirstOrDefaultAsync(i => i.Id == id);
            return result;
        }

        public async Task<List<OrganizationAccountRelationDto>> GetMembersByOrganizationIdAsync(Guid organizationId, bool onlyActive = true)
        {
            var query = _connection.OrganizationMembers
                .LoadWith(i => i.Account)
                .ThenLoad(i => i.PersonInfo)
                .Where(i => i.OrganizationId == organizationId);

            if (onlyActive)
                query = query.Where(i => i.Active);

            var result = await query
                .OrderBy(i => i.Role)
                .ThenBy(i => i.JoinedAt)
                .ToListAsync();
            return result;
        }

        public async Task UpdateMemberRoleAsync(Guid organizationId, Guid accountId, OrganizationMemberRole role)
        {
            await _connection.OrganizationMembers
                .Where(i => i.OrganizationId == organizationId && i.AccountId == accountId)
                .Set(i => i.Role, role)
                .UpdateAsync();
        }

        public async Task SetMemberActiveAsync(Guid organizationId, Guid accountId, bool active)
        {
            await _connection.OrganizationMembers
                .Where(i => i.OrganizationId == organizationId && i.AccountId == accountId)
                .Set(i => i.Active, active)
                .UpdateAsync();
        }

        public async Task RemoveMemberAsync(Guid organizationId, Guid accountId)
        {
            await _connection.OrganizationMembers
                .Where(i => i.OrganizationId == organizationId && i.AccountId == accountId)
                .DeleteAsync();
        }

        public async Task<bool> IsActiveMemberAsync(Guid organizationId, Guid accountId)
        {
            return await _connection.OrganizationMembers
                .AnyAsync(i => i.OrganizationId == organizationId && i.AccountId == accountId && i.Active);
        }

        public async Task<bool> IsOwnerAsync(Guid organizationId, Guid accountId)
        {
            return await _connection.OrganizationMembers
                .AnyAsync(i => i.OrganizationId == organizationId
                    && i.AccountId == accountId
                    && i.Active
                    && i.Role == OrganizationMemberRole.Owner);
        }

        public async Task<bool> IsOwnerOrManagerAsync(Guid organizationId, Guid accountId)
        {
            return await _connection.OrganizationMembers
                .AnyAsync(i => i.OrganizationId == organizationId
                    && i.AccountId == accountId
                    && i.Active
                    && (i.Role == OrganizationMemberRole.Owner || i.Role == OrganizationMemberRole.Manager));
        }

        public async Task TransferOwnershipAsync(Guid organizationId, Guid currentOwnerAccountId, Guid newOwnerAccountId)
        {
            await _connection.OrganizationMembers
                .Where(i => i.OrganizationId == organizationId && i.AccountId == currentOwnerAccountId)
                .Set(i => i.Role, OrganizationMemberRole.Manager)
                .UpdateAsync();

            var newOwnerExists = await _connection.OrganizationMembers
                .AnyAsync(i => i.OrganizationId == organizationId && i.AccountId == newOwnerAccountId);

            if (newOwnerExists)
            {
                await _connection.OrganizationMembers
                    .Where(i => i.OrganizationId == organizationId && i.AccountId == newOwnerAccountId)
                    .Set(i => i.Role, OrganizationMemberRole.Owner)
                    .Set(i => i.Active, true)
                    .UpdateAsync();
            }
            else
            {
                await AddMemberAsync(new OrganizationAccountRelationDto
                {
                    OrganizationId = organizationId,
                    AccountId = newOwnerAccountId,
                    Role = OrganizationMemberRole.Owner,
                    Active = true,
                    InvitedBy = currentOwnerAccountId
                });
            }
        }
        #endregion

        #region legal
        public async Task UpsertLegalAsync(OrganizationLegalDto item)
        {
            PersonalDataCrypto.EncryptLegal(item, _fieldEncryptor);
            var exists = await _connection.OrganizationLegal.AnyAsync(i => i.OrganizationId == item.OrganizationId);
            if (exists)
            {
                await _connection.OrganizationLegal.Where(i => i.OrganizationId == item.OrganizationId)
                    .Set(i => i.LegalForm, item.LegalForm)
                    .Set(i => i.Inn, item.Inn)
                    .Set(i => i.InnHash, item.InnHash)
                    .Set(i => i.Ogrn, item.Ogrn)
                    .Set(i => i.Kpp, item.Kpp)
                    .Set(i => i.LegalAddress, item.LegalAddress)
                    .Set(i => i.HeadName, item.HeadName)
                    .Set(i => i.HeadBasis, item.HeadBasis)
                    .Set(i => i.VerifiedAt, item.VerifiedAt)
                    .UpdateAsync();
            }
            else
            {
                await _connection.InsertAsync(item);
            }
        }

        public async Task<OrganizationLegalDto?> GetLegalAsync(Guid organizationId)
        {
            var result = await _connection.OrganizationLegal.FirstOrDefaultAsync(i => i.OrganizationId == organizationId);
            PersonalDataCrypto.DecryptLegal(result, _fieldEncryptor);
            return result;
        }

        public async Task DeleteLegalAsync(Guid organizationId)
        {
            await _connection.OrganizationLegal.Where(i => i.OrganizationId == organizationId).DeleteAsync();
        }
        #endregion

        #region payout
        public async Task UpsertPayoutAsync(OrganizationPayoutDto item)
        {
            item.UpdateDate = DateTimeOffset.Now.ToUniversalTime();
            var exists = await _connection.OrganizationPayout.AnyAsync(i => i.OrganizationId == item.OrganizationId);
            if (exists)
            {
                await _connection.OrganizationPayout.Where(i => i.OrganizationId == item.OrganizationId)
                    .Set(i => i.BankAccount, item.BankAccount)
                    .Set(i => i.Bik, item.Bik)
                    .Set(i => i.BankName, item.BankName)
                    .Set(i => i.TaxRegime, item.TaxRegime)
                    .Set(i => i.Provider, item.Provider)
                    .Set(i => i.ProviderSellerId, item.ProviderSellerId)
                    .Set(i => i.OnboardingStatus, item.OnboardingStatus)
                    .Set(i => i.UpdatedBy, item.UpdatedBy)
                    .Set(i => i.UpdateDate, item.UpdateDate)
                    .UpdateAsync();
            }
            else
            {
                await _connection.InsertAsync(item);
            }
        }

        public async Task<OrganizationPayoutDto?> GetPayoutAsync(Guid organizationId)
        {
            var result = await _connection.OrganizationPayout.FirstOrDefaultAsync(i => i.OrganizationId == organizationId);
            return result;
        }

        public async Task SetProviderOnboardingAsync(Guid organizationId, PaymentProvider? provider, string? providerSellerId, ProviderOnboardingStatus status)
        {
            await _connection.OrganizationPayout.Where(i => i.OrganizationId == organizationId)
                .Set(i => i.Provider, provider)
                .Set(i => i.ProviderSellerId, providerSellerId)
                .Set(i => i.OnboardingStatus, status)
                .Set(i => i.UpdateDate, DateTimeOffset.Now.ToUniversalTime())
                .UpdateAsync();
        }

        public async Task DeletePayoutAsync(Guid organizationId)
        {
            await _connection.OrganizationPayout.Where(i => i.OrganizationId == organizationId).DeleteAsync();
        }
        #endregion
    }
}
