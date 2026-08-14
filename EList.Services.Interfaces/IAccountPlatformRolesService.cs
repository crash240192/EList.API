using EList.Common.Models;
using EList.Models.Enums;
using EList.Models.PlatformRoles;

namespace EList.Services.Interfaces
{
    public interface IAccountPlatformRolesService
    {
        /// <summary>
        /// Активная роль площадки аккаунта (без ACL). Для заполнения AccountDataHolder при авторизации.
        /// </summary>
        Task<PlatformRole?> ResolveActiveRoleAsync(Guid accountId);

        Task<CommandResult<AccountPlatformRole?>> GetMyRoleAsync();
        Task<CommandResult<AccountPlatformRole?>> GetByAccountIdAsync(Guid accountId);
        Task<CommandResult<List<AccountPlatformRole>>> GetAllAsync(PlatformRole? role = null, bool onlyActive = true);
        Task<CommandResult<Guid?>> AssignRoleAsync(AssignPlatformRoleRequest request);
        Task<CommandResult> SetActiveAsync(Guid accountId, bool active);
        Task<CommandResult> DeleteRoleAsync(Guid accountId);
    }
}
