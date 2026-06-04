using EList.Api.Extensions;
using EList.Common.CorrelationId;
using EList.Common.Logger;
using EList.Common.Models;
using EList.DbDataProvider.Interfaces;
using EList.Models.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NLog;
using System.Diagnostics;
using TM.Schedule.API.Attributes;
using Authorization = EList.Models.Authorization.Authorization;
using IAuthorizationService = EList.Services.Interfaces.IAuthorizationService;

namespace EList.Api.Controllers
{
    [Authorize]
    [ApiController]
    [Route("/api/authorization")]
    [LoggerHandlerWebApiFilter]
    public class AuthorizationController : ControllerBase
    {
        #region logger
        private static readonly NLog.ILogger log = LogManager.GetCurrentClassLogger();
        private static readonly ILoggerWrapper logger = new NLogLoggerWrapper(log);
        private const string LOGGER_NAME = "EList.Api.Controllers.AuthorizationController.";
        #endregion

        private readonly IAuthorizationService _authorizationService;
        private readonly ICorrelationIdProvider _correlationIdProvider;
        private readonly IDataConnectionProvider _connectionProvider;

        public AuthorizationController(IAuthorizationService authorizationService,
            ICorrelationIdProvider correlationIdProvider,
            IDataConnectionProvider connectionProvider)
        {
            _authorizationService = authorizationService;
            _correlationIdProvider = correlationIdProvider;
            _connectionProvider = connectionProvider;
        }

        /// <summary>
        /// Авторизация пользователя
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        [AllowAnonymous]
        [HttpPost]
        public async Task<CommandResult<AuthorizationResponse>> AuthorizeAsync(AuthorizationRequest request)
        {
            var correlationId = _correlationIdProvider.Get();
            var execTime = Stopwatch.StartNew();
            var methodName = $"{LOGGER_NAME}{nameof(AuthorizeAsync)}";

            try
            {
                logger.Debug(correlationId, null, methodName, $"Method started", null);

                var clientHash = this.GetClientHash(); //TODO: Реализовать получение информации о клиенте

                var result = await _authorizationService.AuthorizeAsync(request.Login, request.Password, clientHash);

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
        /// Активация зарегистрированного токена
        /// </summary>
        /// <param name="activationKey"></param>
        /// <returns></returns>
        [HttpGet("activate")]
        public async Task<CommandResult> ActivateTokenAsync(string activationKey)
        {
            var correlationId = _correlationIdProvider.Get();
            var execTime = Stopwatch.StartNew();
            var methodName = $"{LOGGER_NAME}{nameof(AuthorizeAsync)}";

            try
            {
                await _connectionProvider.StartNewTransactionAsync();
                logger.Debug(correlationId, null, methodName, $"Method started", null);

                var clientHash = this.GetClientHash();

                var result = await _authorizationService.ActivateTokenAsync(activationKey, clientHash);

                if (!result.Success)
                    await _connectionProvider.RollbackTransactionAsync()

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
        /// Проверка авторизации
        /// </summary>
        /// <returns></returns>
        [HttpGet("check")]
        public async Task<CommandResult> CheckAuthorizationAsync()
        {
            var correlationId = _correlationIdProvider.Get();
            var execTime = Stopwatch.StartNew();
            var methodName = $"{LOGGER_NAME}{nameof(CheckAuthorizationAsync)}";

            try
            {
                logger.Debug(correlationId, null, methodName, $"Method started", null);

                var result = CommandResult.OK;

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
        /// Выслать код активации
        /// </summary>
        /// <returns></returns>
        [HttpGet("sendActivationCode")]
        public async Task<CommandResult> SendActivationCodeAsync()
        {
            var correlationId = _correlationIdProvider.Get();
            var execTime = Stopwatch.StartNew();
            var methodName = $"{LOGGER_NAME}{nameof(SendActivationCodeAsync)}";

            try
            {
                logger.Debug(correlationId, null, methodName, $"Method started", null);

                var result = await _authorizationService.SendActivationCodeAsync();

                logger.Debug(correlationId, null, methodName, $"Method finished", null, execTime.Elapsed);
                return result;
            }
            catch (Exception ex)
            {
                ExceptionLogger.LogException(logger, correlationId, methodName, "Method failed", execTime.Elapsed, ex);
                throw;
            }
        }

        /*public async Task<CommandResult> DeactivateTokenAsync(Guid token)
        {
            var correlationId = _correlationIdProvider.Get();
            var execTime = Stopwatch.StartNew();
            var methodName = $"{LOGGER_NAME}{nameof(AuthorizeAsync)}";

            try
            {
                logger.Debug(correlationId, null, methodName, $"Method started", null);

                var result = await _authorizationService.AuthorizeAsync(login, passwordHash, clientHash);

                logger.Debug(correlationId, null, methodName, $"Method finished", null, execTime.Elapsed);
                return result;
            }
            catch (Exception ex)
            {
                ExceptionLogger.LogException(logger, correlationId, methodName, "Method failed", execTime.Elapsed, ex);
                throw;
            }
        }*/

        /*
        [HttpPost("create")]
        public async Task<CommandResult<Guid>> CreateTokenAsync(Guid accountId)
        {
            var correlationId = _correlationIdProvider.Get();
            var execTime = Stopwatch.StartNew();
            var methodName = $"{LOGGER_NAME}{nameof(AuthorizeAsync)}";

            try
            {
                logger.Debug(correlationId, null, methodName, $"Method started", null);

                var clientHash = this.GetClientHash();

                var result = await _authorizationService.CreateTokenAsync(accountId, clientHash);

                logger.Debug(correlationId, null, methodName, $"Method finished", null, execTime.Elapsed);
                return result;
            }
            catch (Exception ex)
            {
                ExceptionLogger.LogException(logger, correlationId, methodName, "Method failed", execTime.Elapsed, ex);
                throw;
            }
        }*/

        /*public async Task<CommandResult<Authorization?>> GetAuthorizationDataAsync()
        {
            var correlationId = _correlationIdProvider.Get();
            var execTime = Stopwatch.StartNew();
            var methodName = $"{LOGGER_NAME}{nameof(GetAuthorizationDataAsync)}";

            try
            {
                logger.Debug(correlationId, null, methodName, $"Method started", null);

                var token = this.GetToken();

                var result = await _authorizationService.GetAuthorizationDataAsync(token);

                logger.Debug(correlationId, null, methodName, $"Method finished", null, execTime.Elapsed);
                return result;
            }
            catch (Exception ex)
            {
                ExceptionLogger.LogException(logger, correlationId, methodName, "Method failed", execTime.Elapsed, ex);
                throw;
            }
        }*/


        /// <summary>
        /// Отдаёт токен для текущего устрйства (не особо
        /// </summary>
        /// <returns></returns>
        //[AllowAnonymous]
        [HttpPost("getData")]
        public async Task<CommandResult<Authorization?>> GetAuthorizationDataAsync()
        {
            var correlationId = _correlationIdProvider.Get();
            var execTime = Stopwatch.StartNew();
            var methodName = $"{LOGGER_NAME}{nameof(GetAuthorizationDataAsync)}";

            try
            {
                logger.Debug(correlationId, null, methodName, $"Method started", null);

                var clientHash = this.GetClientHash();

                var result = await _authorizationService.GetAuthorizationDataAsync(clientHash);

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
