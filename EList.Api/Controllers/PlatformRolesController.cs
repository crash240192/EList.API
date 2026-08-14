using EList.Common.CorrelationId;
using EList.Common.Logger;
using EList.Common.Models;
using EList.DbDataProvider.Interfaces;
using EList.Models.Enums;
using EList.Models.PlatformRoles;
using EList.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NLog;
using System.Diagnostics;
using TM.Schedule.API.Attributes;

namespace EList.Api.Controllers
{
    [Authorize]
    [ApiController]
    [Route("/api/platformRoles")]
    [LoggerHandlerWebApiFilter]
    public class PlatformRolesController : ControllerBase
    {
        #region logger
        private static readonly NLog.ILogger log = LogManager.GetCurrentClassLogger();
        private static readonly ILoggerWrapper logger = new NLogLoggerWrapper(log);
        private const string LOGGER_NAME = "EList.Api.Controllers.PlatformRolesController.";
        #endregion

        private readonly IAccountPlatformRolesService _platformRolesService;
        private readonly ICorrelationIdProvider _correlationIdProvider;
        private readonly IDataConnectionProvider _connectionProvider;

        public PlatformRolesController(
            IAccountPlatformRolesService platformRolesService,
            ICorrelationIdProvider correlationIdProvider,
            IDataConnectionProvider connectionProvider)
        {
            _platformRolesService = platformRolesService;
            _correlationIdProvider = correlationIdProvider;
            _connectionProvider = connectionProvider;
        }

        /// <summary>
        /// Роль площадки текущего пользователя (null / отсутствие = обычный пользователь)
        /// </summary>
        [HttpGet("my")]
        public async Task<CommandResult<AccountPlatformRole?>> GetMyRoleAsync()
        {
            var correlationId = _correlationIdProvider.Get();
            var execTime = Stopwatch.StartNew();
            var methodName = $"{LOGGER_NAME}{nameof(GetMyRoleAsync)}";

            try
            {
                logger.Debug(correlationId, null, methodName, "Method started", null);
                var result = await _platformRolesService.GetMyRoleAsync();
                logger.Debug(correlationId, null, methodName, "Method finished", null, execTime.Elapsed);
                return result;
            }
            catch (Exception ex)
            {
                ExceptionLogger.LogException(logger, correlationId, methodName, "Method failed", execTime.Elapsed, ex);
                throw;
            }
        }

        /// <summary>
        /// Список ролей площадки (admin/superuser)
        /// </summary>
        [HttpGet("all")]
        public async Task<CommandResult<List<AccountPlatformRole>>> GetAllAsync(
            [FromQuery] PlatformRole? role = null,
            [FromQuery] bool onlyActive = true)
        {
            var correlationId = _correlationIdProvider.Get();
            var execTime = Stopwatch.StartNew();
            var methodName = $"{LOGGER_NAME}{nameof(GetAllAsync)}";

            try
            {
                logger.Debug(correlationId, null, methodName, "Method started", null);
                var result = await _platformRolesService.GetAllAsync(role, onlyActive);
                logger.Debug(correlationId, null, methodName, "Method finished", null, execTime.Elapsed);
                return result;
            }
            catch (Exception ex)
            {
                ExceptionLogger.LogException(logger, correlationId, methodName, "Method failed", execTime.Elapsed, ex);
                throw;
            }
        }

        /// <summary>
        /// Роль площадки аккаунта (admin/superuser)
        /// </summary>
        [HttpGet("byAccount/{accountId}")]
        public async Task<CommandResult<AccountPlatformRole?>> GetByAccountIdAsync(Guid accountId)
        {
            var correlationId = _correlationIdProvider.Get();
            var execTime = Stopwatch.StartNew();
            var methodName = $"{LOGGER_NAME}{nameof(GetByAccountIdAsync)}";

            try
            {
                logger.Debug(correlationId, null, methodName, "Method started", null);
                var result = await _platformRolesService.GetByAccountIdAsync(accountId);
                logger.Debug(correlationId, null, methodName, "Method finished", null, execTime.Elapsed);
                return result;
            }
            catch (Exception ex)
            {
                ExceptionLogger.LogException(logger, correlationId, methodName, "Method failed", execTime.Elapsed, ex);
                throw;
            }
        }

        /// <summary>
        /// Назначить / обновить роль площадки (admin/superuser)
        /// </summary>
        [HttpPost("assign")]
        public async Task<CommandResult<Guid?>> AssignRoleAsync([FromBody] AssignPlatformRoleRequest request)
        {
            var correlationId = _correlationIdProvider.Get();
            var execTime = Stopwatch.StartNew();
            var methodName = $"{LOGGER_NAME}{nameof(AssignRoleAsync)}";

            try
            {
                await _connectionProvider.StartNewTransactionAsync();
                logger.Debug(correlationId, null, methodName, "Method started", null);

                var result = await _platformRolesService.AssignRoleAsync(request);
                if (!result.Success)
                    await _connectionProvider.RollbackTransactionAsync();

                logger.Debug(correlationId, null, methodName, "Method finished", null, execTime.Elapsed);
                return result;
            }
            catch (Exception ex)
            {
                await _connectionProvider.RollbackTransactionAsync();
                ExceptionLogger.LogException(logger, correlationId, methodName, "Method failed", execTime.Elapsed, ex);
                throw;
            }
        }

        /// <summary>
        /// Активировать / деактивировать роль (admin/superuser)
        /// </summary>
        [HttpPut("setActive/{accountId}")]
        public async Task<CommandResult> SetActiveAsync(Guid accountId, [FromQuery] bool active)
        {
            var correlationId = _correlationIdProvider.Get();
            var execTime = Stopwatch.StartNew();
            var methodName = $"{LOGGER_NAME}{nameof(SetActiveAsync)}";

            try
            {
                await _connectionProvider.StartNewTransactionAsync();
                logger.Debug(correlationId, null, methodName, "Method started", null);

                var result = await _platformRolesService.SetActiveAsync(accountId, active);
                if (!result.Success)
                    await _connectionProvider.RollbackTransactionAsync();

                logger.Debug(correlationId, null, methodName, "Method finished", null, execTime.Elapsed);
                return result;
            }
            catch (Exception ex)
            {
                await _connectionProvider.RollbackTransactionAsync();
                ExceptionLogger.LogException(logger, correlationId, methodName, "Method failed", execTime.Elapsed, ex);
                throw;
            }
        }

        /// <summary>
        /// Удалить роль площадки (пользователь снова обычный)
        /// </summary>
        [HttpDelete("delete/{accountId}")]
        public async Task<CommandResult> DeleteRoleAsync(Guid accountId)
        {
            var correlationId = _correlationIdProvider.Get();
            var execTime = Stopwatch.StartNew();
            var methodName = $"{LOGGER_NAME}{nameof(DeleteRoleAsync)}";

            try
            {
                await _connectionProvider.StartNewTransactionAsync();
                logger.Debug(correlationId, null, methodName, "Method started", null);

                var result = await _platformRolesService.DeleteRoleAsync(accountId);
                if (!result.Success)
                    await _connectionProvider.RollbackTransactionAsync();

                logger.Debug(correlationId, null, methodName, "Method finished", null, execTime.Elapsed);
                return result;
            }
            catch (Exception ex)
            {
                await _connectionProvider.RollbackTransactionAsync();
                ExceptionLogger.LogException(logger, correlationId, methodName, "Method failed", execTime.Elapsed, ex);
                throw;
            }
        }
    }
}
