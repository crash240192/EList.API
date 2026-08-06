using EList.Common.CorrelationId;
using EList.Common.Logger;
using EList.Common.Models;
using EList.DbDataProvider.Interfaces;
using EList.Models.EventTemplates;
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
    [Route("/api/eventTemplates")]
    [LoggerHandlerWebApiFilter]
    public class EventTemplatesController : ControllerBase
    {
        #region logger
        private static readonly NLog.ILogger log = LogManager.GetCurrentClassLogger();
        private static readonly ILoggerWrapper logger = new NLogLoggerWrapper(log);
        private const string LOGGER_NAME = "EList.Api.Controllers.EventTemplatesController.";
        #endregion

        private readonly IEventTemplatesService _eventTemplatesService;
        private readonly ICorrelationIdProvider _correlationIdProvider;
        private readonly IDataConnectionProvider _connectionProvider;

        public EventTemplatesController(IEventTemplatesService eventTemplatesService,
            ICorrelationIdProvider correlationIdProvider,
            IDataConnectionProvider connectionProvider)
        {
            _eventTemplatesService = eventTemplatesService;
            _correlationIdProvider = correlationIdProvider;
            _connectionProvider = connectionProvider;
        }

        /// <summary>
        /// Создать шаблон мероприятия
        /// </summary>
        [HttpPost("create")]
        public async Task<CommandResult<Guid?>> CreateTemplateAsync([FromBody] CreateEventTemplateRequest request)
        {
            var correlationId = _correlationIdProvider.Get();
            var execTime = Stopwatch.StartNew();
            var methodName = $"{LOGGER_NAME}{nameof(CreateTemplateAsync)}";

            try
            {
                await _connectionProvider.StartNewTransactionAsync();
                logger.Debug(correlationId, null, methodName, $"Method started", null);

                var result = await _eventTemplatesService.CreateTemplateAsync(request);
                if (!result.Success)
                    await _connectionProvider.RollbackTransactionAsync();

                logger.Debug(correlationId, null, methodName, $"Method finished", null, execTime.Elapsed);
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
        /// Получить шаблон мероприятия по id
        /// </summary>
        [HttpGet("get/{templateId}")]
        public async Task<CommandResult<EventTemplateResponse?>> GetTemplateAsync(Guid templateId)
        {
            var correlationId = _correlationIdProvider.Get();
            var execTime = Stopwatch.StartNew();
            var methodName = $"{LOGGER_NAME}{nameof(GetTemplateAsync)}";

            try
            {
                logger.Debug(correlationId, null, methodName, $"Method started", null);

                var result = await _eventTemplatesService.GetTemplateAsync(templateId);

                logger.Debug(correlationId, null, methodName, $"Method finished", null, execTime.Elapsed);
                return result;
            }
            catch (Exception ex)
            {
                ExceptionLogger.LogException(logger, correlationId, methodName, "Method failed", execTime.Elapsed, ex);
                throw;
            }
        }

        /// <summary>
        /// Обновить шаблон мероприятия
        /// </summary>
        [HttpPut("update/{templateId}")]
        public async Task<CommandResult> UpdateTemplateAsync(Guid templateId, [FromBody] UpdateEventTemplateRequest request)
        {
            var correlationId = _correlationIdProvider.Get();
            var execTime = Stopwatch.StartNew();
            var methodName = $"{LOGGER_NAME}{nameof(UpdateTemplateAsync)}";

            try
            {
                await _connectionProvider.StartNewTransactionAsync();
                logger.Debug(correlationId, null, methodName, $"Method started", null);

                var result = await _eventTemplatesService.UpdateTemplateAsync(templateId, request);
                if (!result.Success)
                    await _connectionProvider.RollbackTransactionAsync();

                logger.Debug(correlationId, null, methodName, $"Method finished", null, execTime.Elapsed);
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
        /// Удалить шаблон мероприятия
        /// </summary>
        [HttpDelete("delete/{templateId}")]
        public async Task<CommandResult> DeleteTemplateAsync(Guid templateId)
        {
            var correlationId = _correlationIdProvider.Get();
            var execTime = Stopwatch.StartNew();
            var methodName = $"{LOGGER_NAME}{nameof(DeleteTemplateAsync)}";

            try
            {
                await _connectionProvider.StartNewTransactionAsync();
                logger.Debug(correlationId, null, methodName, $"Method started", null);

                var result = await _eventTemplatesService.DeleteTemplateAsync(templateId);
                if (!result.Success)
                    await _connectionProvider.RollbackTransactionAsync();

                logger.Debug(correlationId, null, methodName, $"Method finished", null, execTime.Elapsed);
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
        /// Поиск доступных шаблонов мероприятий
        /// </summary>
        [HttpPost("search")]
        public async Task<CommandResult<List<EventTemplateResponse>>> SearchTemplatesAsync([FromBody] EventTemplateSearchRequest request)
        {
            var correlationId = _correlationIdProvider.Get();
            var execTime = Stopwatch.StartNew();
            var methodName = $"{LOGGER_NAME}{nameof(SearchTemplatesAsync)}";

            try
            {
                logger.Debug(correlationId, null, methodName, $"Method started", null);

                var result = await _eventTemplatesService.SearchTemplatesAsync(request);

                logger.Debug(correlationId, null, methodName, $"Method finished", null, execTime.Elapsed);
                return result;
            }
            catch (Exception ex)
            {
                ExceptionLogger.LogException(logger, correlationId, methodName, "Method failed", execTime.Elapsed, ex);
                throw;
            }
        }
    }
}
