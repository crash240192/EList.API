using EList.Common.CorrelationId;
using EList.Common.Logger;
using EList.Common.Models;
using EList.DbDataProvider.Interfaces;
using EList.Models.Notifications;
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
    [Route("/api/systemNotifications")]
    [LoggerHandlerWebApiFilter]
    public class SystemNotificationsController : ControllerBase
    {
        #region logger
        private static readonly NLog.ILogger log = LogManager.GetCurrentClassLogger();
        private static readonly ILoggerWrapper logger = new NLogLoggerWrapper(log);
        private const string LOGGER_NAME = "EList.Api.Controllers.SystemNotificationsController.";
        #endregion

        private readonly ISystemNotificationsService _service;
        private readonly ICorrelationIdProvider _correlationIdProvider;
        private readonly IDataConnectionProvider _connectionProvider;

        public SystemNotificationsController(
            ISystemNotificationsService service,
            ICorrelationIdProvider correlationIdProvider,
            IDataConnectionProvider connectionProvider)
        {
            _service = service;
            _correlationIdProvider = correlationIdProvider;
            _connectionProvider = connectionProvider;
        }

        /// <summary>
        /// Список всех системных уведомлений (admin/superuser)
        /// </summary>
        [HttpGet("getAll")]
        public async Task<CommandResult<List<SystemNotification>>> GetAllAsync()
        {
            return await ExecuteAsync(nameof(GetAllAsync), () => _service.GetAllAsync());
        }

        /// <summary>
        /// Получить системное уведомление по id (admin/superuser)
        /// </summary>
        [HttpGet("get/{id}")]
        public async Task<CommandResult<SystemNotification?>> GetByIdAsync(Guid id)
        {
            return await ExecuteAsync(nameof(GetByIdAsync), () => _service.GetByIdAsync(id));
        }

        /// <summary>
        /// Создать системное уведомление (admin/superuser)
        /// </summary>
        [HttpPost("create")]
        public async Task<CommandResult<Guid>> CreateAsync([FromBody] SystemNotification request)
        {
            return await ExecuteTransactionalAsync(nameof(CreateAsync), () => _service.CreateAsync(request));
        }

        /// <summary>
        /// Обновить системное уведомление (admin/superuser)
        /// </summary>
        [HttpPut("update/{id}")]
        public async Task<CommandResult> UpdateAsync(Guid id, [FromBody] SystemNotification request)
        {
            return await ExecuteTransactionalAsync(nameof(UpdateAsync), () => _service.UpdateAsync(id, request));
        }

        /// <summary>
        /// Удалить системное уведомление (admin/superuser)
        /// </summary>
        [HttpDelete("delete/{id}")]
        public async Task<CommandResult> DeleteAsync(Guid id)
        {
            return await ExecuteTransactionalAsync(nameof(DeleteAsync), () => _service.DeleteAsync(id));
        }

        private async Task<T> ExecuteAsync<T>(string methodShortName, Func<Task<T>> action)
        {
            var correlationId = _correlationIdProvider.Get();
            var execTime = Stopwatch.StartNew();
            var methodName = $"{LOGGER_NAME}{methodShortName}";

            try
            {
                logger.Debug(correlationId, null, methodName, "Method started", null);
                var result = await action();
                logger.Debug(correlationId, null, methodName, "Method finished", null, execTime.Elapsed);
                return result;
            }
            catch (Exception ex)
            {
                ExceptionLogger.LogException(logger, correlationId, methodName, "Method failed", execTime.Elapsed, ex);
                throw;
            }
        }

        private async Task<T> ExecuteTransactionalAsync<T>(string methodShortName, Func<Task<T>> action)
            where T : CommandResult
        {
            var correlationId = _correlationIdProvider.Get();
            var execTime = Stopwatch.StartNew();
            var methodName = $"{LOGGER_NAME}{methodShortName}";

            try
            {
                await _connectionProvider.StartNewTransactionAsync();
                logger.Debug(correlationId, null, methodName, "Method started", null);

                var result = await action();
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
