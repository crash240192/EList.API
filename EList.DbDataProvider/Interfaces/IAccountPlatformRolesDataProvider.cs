using EList.DbDataProvider.Models;
using EList.DbDataProvider.Models.Enums;

namespace EList.DbDataProvider.Interfaces
{
    public interface IAccountPlatformRolesDataProvider
    {
        Task<AccountPlatformRoleDto?> GetByAccountIdAsync(Guid accountId, bool onlyActive = true);
        Task<AccountPlatformRoleDto?> GetByIdAsync(Guid id);
        Task<List<AccountPlatformRoleDto>> GetAllAsync(PlatformRole? role = null, bool onlyActive = true);
        Task<Guid> AssignRoleAsync(Guid accountId, PlatformRole role, Guid? assignedBy);
        Task UpdateRoleAsync(Guid accountId, PlatformRole role, Guid? assignedBy);
        Task SetActiveAsync(Guid accountId, bool active);
        Task DeleteAsync(Guid accountId);
        Task<bool> HasActiveRoleAsync(Guid accountId);
        Task<bool> HasAnyOfRolesAsync(Guid accountId, params PlatformRole[] roles);
        Task<bool> IsPlatformStaffAsync(Guid accountId);
    }
}
