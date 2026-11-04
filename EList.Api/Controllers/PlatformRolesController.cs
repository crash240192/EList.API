using EList.Common.Logger;
using EList.Common.Models;
using EList.DbDataProvider.Interfaces;
using EList.Models.Enums;
using EList.Models.PlatformRoles;
using EList.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EList.Api.Controllers
{
    [Authorize]
    [ApiController]
    [Route("/api/platformRoles")]
    [LoggerHandlerWebApiFilter]
    public class PlatformRolesController : ControllerBase
    {
        private readonly IAccountPlatformRolesService _platformRolesService;
        private readonly IDataConnectionProvider _connectionProvider;

        public PlatformRolesController(
            IAccountPlatformRolesService platformRolesService, IDataConnectionProvider connectionProvider)
        {
            _platformRolesService = platformRolesService;
            _connectionProvider = connectionProvider;
        }

        /// <summary>
        /// Роль площадки текущего пользователя (null / отсутствие = обычный пользователь)
        /// </summary>
        [HttpGet("my")]
        public async Task<CommandResult<AccountPlatformRole?>> GetMyRoleAsync()
        {
            var result = await _platformRolesService.GetMyRoleAsync();
            return result;

        }

        /// <summary>
        /// Список ролей площадки (admin/superuser)
        /// </summary>
        [HttpGet("all")]
        public async Task<CommandResult<List<AccountPlatformRole>>> GetAllAsync(
            [FromQuery] PlatformRole? role = null,
            [FromQuery] bool onlyActive = true)
        {
            var result = await _platformRolesService.GetAllAsync(role, onlyActive);
            return result;

        }

        /// <summary>
        /// Роль площадки аккаунта (admin/superuser)
        /// </summary>
        [HttpGet("byAccount/{accountId}")]
        public async Task<CommandResult<AccountPlatformRole?>> GetByAccountIdAsync(Guid accountId)
        {
            var result = await _platformRolesService.GetByAccountIdAsync(accountId);
            return result;

        }

        /// <summary>
        /// Назначить / обновить роль площадки (admin/superuser)
        /// </summary>
        [HttpPost("assign")]
        public async Task<CommandResult<Guid?>> AssignRoleAsync([FromBody] AssignPlatformRoleRequest request)
        {
            try
            {
                await _connectionProvider.StartNewTransactionAsync();

                var result = await _platformRolesService.AssignRoleAsync(request);
                if (!result.Success)
                    await _connectionProvider.RollbackTransactionAsync();

                return result;
            }
            catch
            {
                await _connectionProvider.RollbackTransactionAsync();
                throw;
            }
        }

        /// <summary>
        /// Активировать / деактивировать роль (admin/superuser)
        /// </summary>
        [HttpPut("setActive/{accountId}")]
        public async Task<CommandResult> SetActiveAsync(Guid accountId, [FromQuery] bool active)
        {
            try
            {
                await _connectionProvider.StartNewTransactionAsync();

                var result = await _platformRolesService.SetActiveAsync(accountId, active);
                if (!result.Success)
                    await _connectionProvider.RollbackTransactionAsync();

                return result;
            }
            catch
            {
                await _connectionProvider.RollbackTransactionAsync();
                throw;
            }
        }

        /// <summary>
        /// Удалить роль площадки (пользователь снова обычный)
        /// </summary>
        [HttpDelete("delete/{accountId}")]
        public async Task<CommandResult> DeleteRoleAsync(Guid accountId)
        {
            try
            {
                await _connectionProvider.StartNewTransactionAsync();

                var result = await _platformRolesService.DeleteRoleAsync(accountId);
                if (!result.Success)
                    await _connectionProvider.RollbackTransactionAsync();

                return result;
            }
            catch
            {
                await _connectionProvider.RollbackTransactionAsync();
                throw;
            }
        }
    }
}
