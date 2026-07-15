using System.Diagnostics;
using EList.Common.CorrelationId;
using EList.Common.Logger;
using EList.Common.Models;
using EList.DbDataProvider.Interfaces;
using EList.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NLog;
using TM.Schedule.API.Attributes;

namespace EList.Api.Controllers
{
    [Authorize]
    [ApiController]
    [Route("/api/agreements")]
    [LoggerHandlerWebApiFilter]
    public class AgreementsController : Controller
    {
        #region logger
        private static readonly NLog.ILogger log = LogManager.GetCurrentClassLogger();
        private static readonly ILoggerWrapper logger = new NLogLoggerWrapper(log);
        private const string LOGGER_NAME = "EList.Api.Controllers.AgreementsController.";
        #endregion

        private readonly IAgreementService _agreementService;
        private readonly ICorrelationIdProvider _correlationIdProvider;
        private readonly IDataConnectionProvider _connectionProvider;

        public AgreementsController(IAgreementService agreementService,
            ICorrelationIdProvider correlationIdProvider,
            IDataConnectionProvider connectionProvider)
        {
            
            _correlationIdProvider = correlationIdProvider;
            _connectionProvider = connectionProvider;
            _agreementService = agreementService;
        }


        /// <summary>
        /// Соглашение что анонимному пользователю есть 18+
        /// </summary>
        /// <returns></returns>
        [AllowAnonymous]
        [HttpGet("age/anonymous/agree")]
        public async Task<CommandResult> SaveAnonymousAgeAgreementAsync()
        {
            var correlationId = _correlationIdProvider.Get();
            var execTime = Stopwatch.StartNew();
            var methodName = $"{LOGGER_NAME}{nameof(SaveAnonymousAgeAgreementAsync)}";

            try
            {
                await _connectionProvider.StartNewTransactionAsync();
                logger.Debug(correlationId, null, methodName, $"Method started", null);

                var result = await _agreementService.SaveAnonymousAgeAgreementAsync();
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
        /// Проставил ли пользователь галочку "мне есть 18"
        /// </summary>
        /// <returns></returns>
        [AllowAnonymous]
        [HttpGet("age/anonymous/get")]
        public async Task<CommandResult> GetAnonymousAgeAgreementAsync()
        {
            var correlationId = _correlationIdProvider.Get();
            var execTime = Stopwatch.StartNew();
            var methodName = $"{LOGGER_NAME}{nameof(GetAnonymousAgeAgreementAsync)}";

            try
            {
                logger.Debug(correlationId, null, methodName, $"Method started", null);

                var result = await _agreementService.GetAnonymousAgeAgreementAsync();

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
