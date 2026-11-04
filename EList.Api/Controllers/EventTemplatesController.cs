using EList.Common.Logger;
using EList.Common.Models;
using EList.DbDataProvider.Interfaces;
using EList.Models.EventTemplates;
using EList.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EList.Api.Controllers
{
    [Authorize]
    [ApiController]
    [Route("/api/eventTemplates")]
    [LoggerHandlerWebApiFilter]
    public class EventTemplatesController : ControllerBase
    {
        private readonly IEventTemplatesService _eventTemplatesService;
        private readonly IDataConnectionProvider _connectionProvider;

        public EventTemplatesController(IEventTemplatesService eventTemplatesService, IDataConnectionProvider connectionProvider)
        {
            _eventTemplatesService = eventTemplatesService;
            _connectionProvider = connectionProvider;
        }

        /// <summary>
        /// Создать шаблон мероприятия
        /// </summary>
        [HttpPost("create")]
        public async Task<CommandResult<Guid?>> CreateTemplateAsync([FromBody] CreateEventTemplateRequest request)
        {
            try
            {
                await _connectionProvider.StartNewTransactionAsync();

                var result = await _eventTemplatesService.CreateTemplateAsync(request);
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
        /// Получить шаблон мероприятия по id
        /// </summary>
        [HttpGet("get/{templateId}")]
        public async Task<CommandResult<EventTemplateResponse?>> GetTemplateAsync(Guid templateId)
        {
            var result = await _eventTemplatesService.GetTemplateAsync(templateId);
            return result;
        }

        /// <summary>
        /// Обновить шаблон мероприятия
        /// </summary>
        [HttpPut("update/{templateId}")]
        public async Task<CommandResult> UpdateTemplateAsync(Guid templateId, [FromBody] UpdateEventTemplateRequest request)
        {
            try
            {
                await _connectionProvider.StartNewTransactionAsync();

                var result = await _eventTemplatesService.UpdateTemplateAsync(templateId, request);
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
        /// Удалить шаблон мероприятия
        /// </summary>
        [HttpDelete("delete/{templateId}")]
        public async Task<CommandResult> DeleteTemplateAsync(Guid templateId)
        {
            try
            {
                await _connectionProvider.StartNewTransactionAsync();

                var result = await _eventTemplatesService.DeleteTemplateAsync(templateId);
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
        /// Поиск доступных шаблонов мероприятий
        /// </summary>
        [HttpPost("search")]
        public async Task<CommandResult<List<EventTemplateResponse>>> SearchTemplatesAsync([FromBody] EventTemplateSearchRequest request)
        {
            var result = await _eventTemplatesService.SearchTemplatesAsync(request);
            return result;
        }
    }
}
