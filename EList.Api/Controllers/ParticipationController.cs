using EList.Api.Extensions;
using EList.Common.CorrelationId;
using EList.Common.Logger;
using EList.Common.Models;
using EList.DbDataProvider.Interfaces;
using EList.Models.Accounts;
using EList.Models.Participation;
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
    [Route("/api/participations")]
    [LoggerHandlerWebApiFilter]
    public class ParticipationController : ControllerBase
    {
        #region logger
        private static readonly NLog.ILogger log = LogManager.GetCurrentClassLogger();
        private static readonly ILoggerWrapper logger = new NLogLoggerWrapper(log);
        private const string LOGGER_NAME = "EList.Api.Controllers.ParticipationController.";
        #endregion

        private readonly IParticipationsService _participationService;
        private readonly ICorrelationIdProvider _correlationIdProvider;
        private readonly IDataConnectionProvider _connectionProvider;

        public ParticipationController(ICorrelationIdProvider correlationIdProvider,
            IDataConnectionProvider connectionProvider,
            IParticipationsService participationService)
        {
            _participationService = participationService;
            _correlationIdProvider = correlationIdProvider;
            _connectionProvider = connectionProvider;
        }

        /// <summary>
        /// Участвовать в событии
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        [HttpGet("participate/{id}")]
        public async Task<CommandResult> ParticipateAsync(Guid id)
        {
            var correlationId = _correlationIdProvider.Get();
            var execTime = Stopwatch.StartNew();
            var methodName = $"{LOGGER_NAME}{nameof(ParticipateAsync)}";

            try
            {
                await _connectionProvider.StartNewTransactionAsync();
                logger.Debug(correlationId, null, methodName, $"Method started", null);

                var result = await _participationService.ParticipateAsync(id);

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
        /// Покинуть событие
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        [HttpGet("leave/{id}")]
        public async Task<CommandResult> LeaveEventAsync(Guid id)
        {
            var correlationId = _correlationIdProvider.Get();
            var execTime = Stopwatch.StartNew();
            var methodName = $"{LOGGER_NAME}{nameof(LeaveEventAsync)}";

            try
            {
                await _connectionProvider.StartNewTransactionAsync();
                logger.Debug(correlationId, null, methodName, $"Method started", null);

                var result = await _participationService.LeaveEventAsync(id);

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
        /// Получить список участников события
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        [HttpGet("eventParticipants/{id}")]
        public async Task<CommandResult<List<Participant>>> GetEventParticipantsAsync(Guid id)
        {
            var correlationId = _correlationIdProvider.Get();
            var execTime = Stopwatch.StartNew();
            var methodName = $"{LOGGER_NAME}{nameof(GetEventParticipantsAsync)}";

            try
            {
                //await _connectionProvider.StartNewTransactionAsync();
                logger.Debug(correlationId, null, methodName, $"Method started", null);

                var result = await _participationService.GetEventParticipantsAsync(id);

                logger.Debug(correlationId, null, methodName, $"Method finished", null, execTime.Elapsed);
                return result;
            }
            catch (Exception ex)
            {
                ExceptionLogger.LogException(logger, correlationId, methodName, "Method failed", execTime.Elapsed, ex);
                throw;
            }
        }

        // ??? получить количество участников события
    }
}
