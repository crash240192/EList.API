using EList.Api.Extensions;
using EList.Common.CorrelationId;
using EList.Common.Logger;
using EList.Common.Models;
using EList.DbDataProvider.Interfaces;
using EList.Models.Accounts;
using EList.Services.Impl;
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

                var clientHash = this.GetClientHash();

                var result = await _accountsService.CreateAccountAsync(request, clientHash);
                if (!result.Success)
                {
                    await _connectionProvider.RollbackTransactionAsync();
                    return CommandResult.Fail(result.ErrorCode, result.Message);
                }

                logger.Debug(correlationId, null, methodName, $"Method finished", null, execTime.Elapsed);
                return CommandResult.OK;

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

        /// <summary>
        /// Изменение пароля
        /// </summary>
        /// <returns></returns>
        [HttpPost("changePassword")]
        public async Task<CommandResult> ChangePasswordAsync(ChangePasswordRequest request)
        {
            var correlationId = _correlationIdProvider.Get();
            var execTime = Stopwatch.StartNew();
            var methodName = $"{LOGGER_NAME}{nameof(ChangePasswordAsync)}";

            try
            {
                await _connectionProvider.StartNewTransactionAsync();
                logger.Debug(correlationId, null, methodName, $"Method started", null);

                var result = await _accountsService.ChangePasswordAsync(request);
                if (!result.Success)
                {
                    await _connectionProvider.RollbackTransactionAsync();
                    return CommandResult.Fail(result.ErrorCode, result.Message);
                }
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
