using EList.Models.Enums;
using EList.Models.PlatformRoles;

namespace EList.Repositories.Interfaces
{
    public interface IAccountPlatformRolesRepository
    {
        Task<AccountPlatformRole?> GetByAccountIdAsync(Guid accountId, bool onlyActive = true);
        Task<AccountPlatformRole?> GetByIdAsync(Guid id);
        Task<List<AccountPlatformRole>> GetAllAsync(PlatformRole? role = null, bool onlyActive = true);
        Task<Guid> AssignRoleAsync(Guid accountId, PlatformRole role, Guid? assignedBy);
        Task UpdateRoleAsync(Guid accountId, PlatformRole role, Guid? assignedBy);
        Task SetActiveAsync(Guid accountId, bool active);
        Task DeleteAsync(Guid accountId);
        Task<bool> HasActiveRoleAsync(Guid accountId);
        Task<bool> IsPlatformStaffAsync(Guid accountId);
    }
}
