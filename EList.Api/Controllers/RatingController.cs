using System.Diagnostics;
using EList.Common.CorrelationId;
using EList.Common.Logger;
using EList.Common.Models;
using EList.DbDataProvider.Interfaces;
using EList.Models.Enums;
using EList.Models.EventsRating;
using EList.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NLog;
using Org.BouncyCastle.Asn1.Ocsp;
using TM.Schedule.API.Attributes;

namespace EList.Api.Controllers
{
    /// <summary>
    /// Контроллер для работы с рейтингом мероприятий и организаторов
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    [LoggerHandlerWebApiFilter]
    public class RatingController : ControllerBase
    {
        #region logger
        private static readonly NLog.ILogger log = LogManager.GetCurrentClassLogger();
        private static readonly ILoggerWrapper logger = new NLogLoggerWrapper(log);
        private const string LOGGER_NAME = "EList.Api.Controllers.RatingController.";
        #endregion

        private readonly ICorrelationIdProvider _correlationIdProvider;
        private readonly IDataConnectionProvider _connectionProvider;
        private readonly IEventsRatingService _eventsRatingService;

        /// <summary>
        /// Конструктор контроллера рейтинга мероприятий и организаторов
        /// </summary>
        /// <param name="correlationIdProvider"></param>
        /// <param name="connectionProvider"></param>
        /// <param name="eventsRatingService"></param>
        public RatingController(ICorrelationIdProvider correlationIdProvider, 
            IDataConnectionProvider connectionProvider, 
            IEventsRatingService eventsRatingService)
        {
            _correlationIdProvider = correlationIdProvider;
            _connectionProvider = connectionProvider;
            _eventsRatingService = eventsRatingService;
        }
        
        /// <summary>
        /// Проголосовать за мероприятие
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        [HttpPost("events/vote")]
        public async Task<CommandResult<Guid>> VoteAsync(EventsRatingItem request)
        {
            var correlationId = _correlationIdProvider.Get();
            var execTime = Stopwatch.StartNew();
            var methodName = $"{LOGGER_NAME}{nameof(VoteAsync)}";

            try
            {
                logger.Debug(correlationId, null, methodName, $"Method started", null);
                await _connectionProvider.StartNewTransactionAsync();

                var result = await _eventsRatingService.VoteAsync(request);

                if (!result.Success)
                    await _connectionProvider.RollbackTransactionAsync();
                else
                    await _connectionProvider.CommitTransactionAsync();

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
        /// Рейтинг мероприятия
        /// </summary>
        /// <param name="eventId"></param>
        /// <param name="eventRatingType"></param>
        /// <param name="pageIndex"></param>
        /// <param name="pageSize"></param>
        /// <returns></returns>
        [HttpGet("events/getRating")]
        public async Task<CommandResult<EventRating>> GetEventRatingAsync(Guid eventId, EventRatingType eventRatingType, int? pageIndex = 0, int? pageSize = 20)
        {
            var correlationId = _correlationIdProvider.Get();
            var execTime = Stopwatch.StartNew();
            var methodName = $"{LOGGER_NAME}{nameof(GetEventRatingAsync)}";

            try
            {
                logger.Debug(correlationId, null, methodName, $"Method started", null);

                var result = await _eventsRatingService.GetEventRatingAsync(eventId, eventRatingType, pageIndex, pageSize);

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
        /// Рейтинг организатора
        /// </summary>
        /// <param name="accountId"></param>
        /// <returns></returns>
        [HttpGet("organizators/{accountId}")]
        public async Task<CommandResult<int?>> GetOrganizatorRatingAsync(Guid accountId)
        {
            var correlationId = _correlationIdProvider.Get();
            var execTime = Stopwatch.StartNew();
            var methodName = $"{LOGGER_NAME}{nameof(GetOrganizatorRatingAsync)}";

            try
            {
                logger.Debug(correlationId, null, methodName, $"Method started", null);

                var result = await _eventsRatingService.GetOrganizatorRatingAsync(accountId);

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
        /// Удалить оценку
        /// </summary>
        /// <param name="itemId"></param>
        /// <returns></returns>
        [HttpDelete("events/delete/{eventId}")]
        public async Task<CommandResult> DeleteAsync(Guid itemId)
        {
            var correlationId = _correlationIdProvider.Get();
            var execTime = Stopwatch.StartNew();
            var methodName = $"{LOGGER_NAME}{nameof(DeleteAsync)}";

            try
            {
                logger.Debug(correlationId, null, methodName, $"Method started", null);
                await _connectionProvider.StartNewTransactionAsync();

                var result = await _eventsRatingService.DeleteAsync(itemId);

                if (!result.Success)
                    await _connectionProvider.RollbackTransactionAsync();
                else
                    await _connectionProvider.CommitTransactionAsync();

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
    }
}
