using EList.Api.Extensions;
using EList.Common.CorrelationId;
using EList.Common.Logger;
using EList.Common.Models;
using EList.DbDataProvider.Interfaces;
using EList.Models.Accounts;
using EList.Models.Location;
using EList.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NLog;
using Org.BouncyCastle.Asn1.Ocsp;
using System.Diagnostics;
using TM.Schedule.API.Attributes;

namespace EList.Api.Controllers
{
    [Authorize]
    [ApiController]
    [Route("/api/accounts")]
    [LoggerHandlerWebApiFilter]
    public class AccountsController : Controller
    {
        #region logger
        private static readonly NLog.ILogger log = LogManager.GetCurrentClassLogger();
        private static readonly ILoggerWrapper logger = new NLogLoggerWrapper(log);
        private const string LOGGER_NAME = "EList.Api.Controllers.AccountsController.";
        #endregion

        private readonly IAccountsService _accountsService;
        private readonly ICorrelationIdProvider _correlationIdProvider;
        private readonly IDataConnectionProvider _connectionProvider;
        private readonly IMediaService _mediaService;

        public AccountsController(IAccountsService accountsService,
            ICorrelationIdProvider correlationIdProvider,
            IDataConnectionProvider connectionProvider,
            IMediaService mediaService)
        {
            _accountsService = accountsService;
            _correlationIdProvider = correlationIdProvider;
            _connectionProvider = connectionProvider;
            _mediaService = mediaService;
        }
        

        /// <summary>
        /// Создание аккаунта
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        [AllowAnonymous]
        [HttpPost("create")]
        public async Task<CommandResult> CreateAccountAsync(CreateAccountRequest request)
        {
            var correlationId = _correlationIdProvider.Get();
            var execTime = Stopwatch.StartNew();
            var methodName = $"{LOGGER_NAME}{nameof(CreateAccountAsync)}";

            try
            {
                await _connectionProvider.StartNewTransactionAsync();
                logger.Debug(correlationId, null, methodName, $"Method started", null);

                //var clientHash = this.GetClientHash();

                var result = await _accountsService.CreateAccountAsync(request, clientHash);
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
        /// Получить информацию о текущем аккаунте
        /// </summary>
        /// <returns></returns>
        [HttpGet("getData")]
        public async Task<CommandResult<Account?>> GetAccountAsync()
        {
            var correlationId = _correlationIdProvider.Get();
            var execTime = Stopwatch.StartNew();
            var methodName = $"{LOGGER_NAME}{nameof(GetAccountAsync)}";

            try
            {
                logger.Debug(correlationId, null, methodName, $"Method started", null);

                var result = await _accountsService.GetAccountByTokenAsync();

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
        /// Обновление координат пользователя
        /// </summary>
        /// <param name="location"></param>
        /// <returns></returns>
        [HttpPost("updateLocation")]
        public async Task<CommandResult> UpdateLocationAsync(Location location)
        {
            var correlationId = _correlationIdProvider.Get();
            var execTime = Stopwatch.StartNew();
            var methodName = $"{LOGGER_NAME}{nameof(GetAccountAsync)}";

            try
            {
                logger.Debug(correlationId, null, methodName, $"Method started", null);

                await _connectionProvider.StartNewTransactionAsync();

                var result = await _accountsService.UpdateLocationAsync(location.Latitude, location.Longitude);
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
        /// Получить информацию об аккаунте
        /// </summary>
        /// <returns></returns>
        [HttpGet("getData/{accountId}")]
        public async Task<CommandResult<Account?>> GetAccountAsync(Guid accountId)
        {
            var correlationId = _correlationIdProvider.Get();
            var execTime = Stopwatch.StartNew();
            var methodName = $"{LOGGER_NAME}{nameof(GetAccountAsync)}";

            try
            {
                logger.Debug(correlationId, null, methodName, $"Method started", null);

                var result = await _accountsService.GetAccountAsync(accountId);

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
