using EList.Common.CorrelationId;
using EList.Common.Logger;
using EList.Common.Models;
using EList.DbDataProvider.Interfaces;
using EList.Models.EventOrganizators;
using EList.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NLog;
using System.Diagnostics;
using TM.Schedule.API.Attributes;

namespace EList.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    [LoggerHandlerWebApiFilter]
    public class EventOrganizatorsController : ControllerBase
    {
        #region logger
        private static readonly NLog.ILogger log = LogManager.GetCurrentClassLogger();
        private static readonly ILoggerWrapper logger = new NLogLoggerWrapper(log);
        private const string LOGGER_NAME = "EList.Api.Controllers.EventOrganizatorsController.";
        #endregion

        private readonly IEventOrganizatorsService _organizatorsService;
        private readonly ICorrelationIdProvider _correlationIdProvider;
        private readonly IDataConnectionProvider _connectionProvider;

        public EventOrganizatorsController(ICorrelationIdProvider correlationIdProvider,
            IDataConnectionProvider connectionProvider,
            IEventOrganizatorsService organizatorsService
            )
        {
            _organizatorsService = organizatorsService;
            _correlationIdProvider = correlationIdProvider;
            _connectionProvider = connectionProvider;
        }

        /// <summary>
        /// список организаторов мероприятия
        /// </summary>
        /// <param name="eventId"></param>
        /// <returns></returns>
        [HttpGet("getByEventId/{eventId}")]
        public async Task<CommandResult<List<EventOrganizator>>> GetByEventIdAsync(Guid eventId)
        {
            var correlationId = _correlationIdProvider.Get();
            var execTime = Stopwatch.StartNew();
            var methodName = $"{LOGGER_NAME}{nameof(GetByEventIdAsync)}";

            try
            {
                logger.Debug(correlationId, null, methodName, $"Method started", null);

                var result = await _organizatorsService.GetByEventIdAsync(eventId);

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
        /// Добавить мероприятию организаторов
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        [HttpPost("assign")]
        public async Task<CommandResult> AssignEventOrganizatorsAsync(EventOrganizatorsListRequest request)
        {
            var correlationId = _correlationIdProvider.Get();
            var execTime = Stopwatch.StartNew();
            var methodName = $"{LOGGER_NAME}{nameof(AssignEventOrganizatorsAsync)}";

            try
            {
                logger.Debug(correlationId, null, methodName, $"Method started", null);
                await _connectionProvider.StartNewTransactionAsync();

                var result = await _organizatorsService.AssignEventOrganizatorsAsync(request.EventId, request.AccountIds, request.OrganizationIds);

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
        /// Проверяет, является ли текущий пользователь организатором мероприятия
        /// (напрямую или как активный участник организации-организатора)
        /// </summary>
        /// <param name="eventId">Идентификатор мероприятия</param>
        [HttpGet("isOrganizator/{eventId}")]
        public async Task<CommandResult<bool>> IsCurrentUserEventOrganizatorAsync(Guid eventId)
        {
            var correlationId = _correlationIdProvider.Get();
            var execTime = Stopwatch.StartNew();
            var methodName = $"{LOGGER_NAME}{nameof(IsCurrentUserEventOrganizatorAsync)}";

            try
            {
                logger.Debug(correlationId, null, methodName, $"Method started", null);

                var result = await _organizatorsService.IsCurrentUserEventOrganizatorAsync(eventId);

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
