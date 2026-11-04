using EList.Common.Logger;
using EList.Common.Models;
using EList.DbDataProvider.Interfaces;
using EList.Models.Notifications;
using EList.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EList.Api.Controllers
{
    [Authorize]
    [ApiController]
    [Route("/api/systemNotifications")]
    [LoggerHandlerWebApiFilter]
    public class SystemNotificationsController : ControllerBase
    {
        private readonly ISystemNotificationsService _service;
        private readonly IDataConnectionProvider _connectionProvider;

        public SystemNotificationsController(
            ISystemNotificationsService service, IDataConnectionProvider connectionProvider)
        {
            _service = service;
            _connectionProvider = connectionProvider;
        }

        /// <summary>
        /// Список всех системных уведомлений (admin/superuser)
        /// </summary>
        [HttpGet("getAll")]
        public async Task<CommandResult<List<SystemNotification>>> GetAllAsync()
        {
            return await _service.GetAllAsync();
        }

        /// <summary>
        /// Получить системное уведомление по id (admin/superuser)
        /// </summary>
        [HttpGet("get/{id}")]
        public async Task<CommandResult<SystemNotification?>> GetByIdAsync(Guid id)
        {
            return await _service.GetByIdAsync(id);
        }

        /// <summary>
        /// Создать системное уведомление (admin/superuser)
        /// </summary>
        [HttpPost("create")]
        public async Task<CommandResult<Guid>> CreateAsync([FromBody] SystemNotification request)
        {
            return await ExecuteTransactionalAsync(() => _service.CreateAsync(request));
        }

        /// <summary>
        /// Обновить системное уведомление (admin/superuser)
        /// </summary>
        [HttpPut("update/{id}")]
        public async Task<CommandResult> UpdateAsync(Guid id, [FromBody] SystemNotification request)
        {
            return await ExecuteTransactionalAsync(() => _service.UpdateAsync(id, request));
        }

        /// <summary>
        /// Удалить системное уведомление (admin/superuser)
        /// </summary>
        [HttpDelete("delete/{id}")]
        public async Task<CommandResult> DeleteAsync(Guid id)
        {
            return await ExecuteTransactionalAsync(() => _service.DeleteAsync(id));
        }

        private async Task<T> ExecuteTransactionalAsync<T>(Func<Task<T>> action)
            where T : CommandResult
        {
            try
            {
                await _connectionProvider.StartNewTransactionAsync();
                var result = await action();
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
