using AutoMapper;
using EList.DbDataProvider.Interfaces;
using EList.DbDataProvider.Models;
using EList.Models.Accounts;
using EList.Models.Enums;
using EList.Models.PlatformRoles;
using EList.Repositories.Interfaces;

namespace EList.Repositories.Impl
{
    public class AccountPlatformRolesRepository : IAccountPlatformRolesRepository
    {
        private readonly IAccountPlatformRolesDataProvider _dataProvider;
        private readonly IMapper _mapper;

        public AccountPlatformRolesRepository(IAccountPlatformRolesDataProvider dataProvider, IMapper mapper)
        {
            _dataProvider = dataProvider;
            _mapper = mapper;
        }

        public async Task<AccountPlatformRole?> GetByAccountIdAsync(Guid accountId, bool onlyActive = true)
        {
            var item = await _dataProvider.GetByAccountIdAsync(accountId, onlyActive);
            return Map(item);
        }

        public async Task<AccountPlatformRole?> GetByIdAsync(Guid id)
        {
            var item = await _dataProvider.GetByIdAsync(id);
            return Map(item);
        }

        public async Task<List<AccountPlatformRole>> GetAllAsync(PlatformRole? role = null, bool onlyActive = true)
        {
            var dbRole = role == null
                ? (DbDataProvider.Models.Enums.PlatformRole?)null
                : _mapper.Map<DbDataProvider.Models.Enums.PlatformRole>(role.Value);

            var items = await _dataProvider.GetAllAsync(dbRole, onlyActive);
            return items.Select(Map).Where(i => i != null).Cast<AccountPlatformRole>().ToList();
        }

        public async Task<Guid> AssignRoleAsync(Guid accountId, PlatformRole role, Guid? assignedBy)
        {
            var dbRole = _mapper.Map<DbDataProvider.Models.Enums.PlatformRole>(role);
            return await _dataProvider.AssignRoleAsync(accountId, dbRole, assignedBy);
        }

        public async Task UpdateRoleAsync(Guid accountId, PlatformRole role, Guid? assignedBy)
        {
            var dbRole = _mapper.Map<DbDataProvider.Models.Enums.PlatformRole>(role);
            await _dataProvider.UpdateRoleAsync(accountId, dbRole, assignedBy);
        }

        public async Task SetActiveAsync(Guid accountId, bool active)
        {
            await _dataProvider.SetActiveAsync(accountId, active);
        }

        public async Task DeleteAsync(Guid accountId)
        {
            await _dataProvider.DeleteAsync(accountId);
        }

        public async Task<bool> HasActiveRoleAsync(Guid accountId)
        {
            return await _dataProvider.HasActiveRoleAsync(accountId);
        }

        public async Task<bool> IsPlatformStaffAsync(Guid accountId)
        {
            return await _dataProvider.IsPlatformStaffAsync(accountId);
        }

        private AccountPlatformRole? Map(AccountPlatformRoleDto? item)
        {
            if (item == null)
                return null;

            var result = _mapper.Map<AccountPlatformRole>(item);
            if (item.Account != null)
                result.Account = _mapper.Map<AccountPublicData>(item.Account);
            if (item.AssignedByAccount != null)
                result.AssignedByAccount = _mapper.Map<AccountPublicData>(item.AssignedByAccount);
            return result;
        }
    }
}
