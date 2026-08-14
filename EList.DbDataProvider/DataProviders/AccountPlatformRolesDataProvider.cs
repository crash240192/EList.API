using EList.DbDataProvider.Interfaces;
using EList.DbDataProvider.Models;
using EList.DbDataProvider.Models.Enums;
using LinqToDB;
using LinqToDB.Async;

namespace EList.DbDataProvider.DataProviders
{
    public class AccountPlatformRolesDataProvider : DataProviderBase, IAccountPlatformRolesDataProvider
    {
        public AccountPlatformRolesDataProvider(IDataConnectionProvider dataConnectionProvider)
            : base(dataConnectionProvider)
        {
        }

        public async Task<AccountPlatformRoleDto?> GetByAccountIdAsync(Guid accountId, bool onlyActive = true)
        {
            var query = _connection.AccountPlatformRoles
                .LoadWith(i => i.Account)
                .ThenLoad(a => a.PersonInfo)
                .Where(i => i.AccountId == accountId);

            if (onlyActive)
                query = query.Where(i => i.Active);

            return await query.FirstOrDefaultAsync();
        }

        public async Task<AccountPlatformRoleDto?> GetByIdAsync(Guid id)
        {
            return await _connection.AccountPlatformRoles
                .LoadWith(i => i.Account)
                .ThenLoad(a => a.PersonInfo)
                .FirstOrDefaultAsync(i => i.Id == id);
        }

        public async Task<List<AccountPlatformRoleDto>> GetAllAsync(PlatformRole? role = null, bool onlyActive = true)
        {
            var query = _connection.AccountPlatformRoles
                .LoadWith(i => i.Account)
                .ThenLoad(a => a.PersonInfo)
                .AsQueryable();

            if (onlyActive)
                query = query.Where(i => i.Active);

            if (role != null)
                query = query.Where(i => i.Role == role);

            return await query.OrderBy(i => i.Role).ThenBy(i => i.AssignedAt).ToListAsync();
        }

        public async Task<Guid> AssignRoleAsync(Guid accountId, PlatformRole role, Guid? assignedBy)
        {
            var existing = await _connection.AccountPlatformRoles
                .FirstOrDefaultAsync(i => i.AccountId == accountId);

            if (existing != null)
            {
                await _connection.AccountPlatformRoles.Where(i => i.AccountId == accountId)
                    .Set(i => i.Role, role)
                    .Set(i => i.Active, true)
                    .Set(i => i.AssignedAt, DateTimeOffset.UtcNow)
                    .Set(i => i.AssignedBy, assignedBy)
                    .UpdateAsync();
                return existing.Id;
            }

            var item = new AccountPlatformRoleDto
            {
                AccountId = accountId,
                Role = role,
                Active = true,
                AssignedAt = DateTimeOffset.UtcNow,
                AssignedBy = assignedBy
            };
            return (Guid)await _connection.InsertWithIdentityAsync(item);
        }

        public async Task UpdateRoleAsync(Guid accountId, PlatformRole role, Guid? assignedBy)
        {
            await _connection.AccountPlatformRoles.Where(i => i.AccountId == accountId)
                .Set(i => i.Role, role)
                .Set(i => i.AssignedAt, DateTimeOffset.UtcNow)
                .Set(i => i.AssignedBy, assignedBy)
                .UpdateAsync();
        }

        public async Task SetActiveAsync(Guid accountId, bool active)
        {
            await _connection.AccountPlatformRoles.Where(i => i.AccountId == accountId)
                .Set(i => i.Active, active)
                .UpdateAsync();
        }

        public async Task DeleteAsync(Guid accountId)
        {
            await _connection.AccountPlatformRoles.Where(i => i.AccountId == accountId).DeleteAsync();
        }

        public async Task<bool> HasActiveRoleAsync(Guid accountId)
        {
            return await _connection.AccountPlatformRoles
                .AnyAsync(i => i.AccountId == accountId && i.Active);
        }

        public async Task<bool> HasAnyOfRolesAsync(Guid accountId, params PlatformRole[] roles)
        {
            if (roles == null || roles.Length == 0)
                return false;

            return await _connection.AccountPlatformRoles
                .AnyAsync(i => i.AccountId == accountId && i.Active && roles.Contains(i.Role));
        }

        public async Task<bool> IsPlatformStaffAsync(Guid accountId)
        {
            return await HasAnyOfRolesAsync(
                accountId,
                PlatformRole.Superuser,
                PlatformRole.Admin,
                PlatformRole.Moderator);
        }
    }
}
