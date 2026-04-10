using EList.Api.Extensions;
using EList.Common.CorrelationId;
using EList.Common.Logger;
using EList.Common.Models;
using EList.DbDataProvider.Interfaces;
using EList.Models.Subscriptions;
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
    [Route("/api/subscriptions")]
    [LoggerHandlerWebApiFilter]
    public class SubscriptionsController : ControllerBase
    {
        #region logger
        private static readonly NLog.ILogger log = LogManager.GetCurrentClassLogger();
        private static readonly ILoggerWrapper logger = new NLogLoggerWrapper(log);
        private const string LOGGER_NAME = "EList.Api.Controllers.SubscriptionsController.";
        #endregion

        private readonly ISubscriptionsService _subscriptionsService;
        private readonly ICorrelationIdProvider _correlationIdProvider;
        private readonly IDataConnectionProvider _connectionProvider;

        public SubscriptionsController(ISubscriptionsService subscriptionsService,
            ICorrelationIdProvider correlationIdProvider,
            IDataConnectionProvider connectionProvider)
        {
            _subscriptionsService = subscriptionsService;
            _correlationIdProvider = correlationIdProvider;
            _connectionProvider = connectionProvider;
        }


        /// <summary>
        /// Подписаться на пользователя
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        [HttpGet("subscribe/{accountId}")]
        public async Task<CommandResult> SubscribeToAccountAsync(Guid accountId)
        {
            var correlationId = _correlationIdProvider.Get();
            var execTime = Stopwatch.StartNew();
            var methodName = $"{LOGGER_NAME}{nameof(SubscribeToAccountAsync)}";

            try
            {
                await _connectionProvider.StartNewTransactionAsync();
                logger.Debug(correlationId, null, methodName, $"Method started", null);
                
                var result = await _subscriptionsService.SubscribeToAccountAsync(accountId);
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
                ExceptionLogger.LogException(logger, correlationId, methodName, "Method failed", execTime.Elapsed, ex);
                throw;
            }
        }

        /// <summary>
        /// Обновить параметры подписки на пользователя
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        [HttpPut("update/{accountId}")]
        public async Task<CommandResult> UpdateSubscriptionAsync([FromRoute]Guid accountId, [FromBody] UpdateSubscriptionRequestBase request)
        {
            var correlationId = _correlationIdProvider.Get();
            var execTime = Stopwatch.StartNew();
            var methodName = $"{LOGGER_NAME}{nameof(UpdateSubscriptionAsync)}";

            try
            {
                await _connectionProvider.StartNewTransactionAsync();
                logger.Debug(correlationId, null, methodName, $"Method started", null);
                
                var result = await _subscriptionsService.UpdateSubscriptionAsync(accountId, request);
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
                ExceptionLogger.LogException(logger, correlationId, methodName, "Method failed", execTime.Elapsed, ex);
                throw;
            }
        }

        /// <summary>
        /// Отобразить подписки пользователя
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        [HttpGet("getSubscriptions")]
        public async Task<CommandResult> GetSubscriptionsAsync()
        {
            var correlationId = _correlationIdProvider.Get();
            var execTime = Stopwatch.StartNew();
            var methodName = $"{LOGGER_NAME}{nameof(GetSubscriptionsAsync)}";

            try
            {
                logger.Debug(correlationId, null, methodName, $"Method started", null);

                var result = await _subscriptionsService.GetSubscriptionsAsync();
                
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
        /// Отобразить подписчиков пользователя
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        [HttpGet("getSubscribers")]
        public async Task<CommandResult> GetSubscribersAsync()
        {
            var correlationId = _correlationIdProvider.Get();
            var execTime = Stopwatch.StartNew();
            var methodName = $"{LOGGER_NAME}{nameof(GetSubscribersAsync)}";

            try
            {
                logger.Debug(correlationId, null, methodName, $"Method started", null);

                var result = await _subscriptionsService.GetSubscribersAsync();

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
        /// Отобразить подписчиков пользователя
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        [HttpDelete("deleteSubscription/{accountId}")]
        public async Task<CommandResult> DeleteSubscriptionAsync(Guid accountId)
        {
            var correlationId = _correlationIdProvider.Get();
            var execTime = Stopwatch.StartNew();
            var methodName = $"{LOGGER_NAME}{nameof(DeleteSubscriptionAsync)}";

            try
            {
                await _connectionProvider.StartNewTransactionAsync();
                logger.Debug(correlationId, null, methodName, $"Method started", null);

                var result = await _subscriptionsService.DeleteSubscriptionAsync(accountId);
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
                ExceptionLogger.LogException(logger, correlationId, methodName, "Method failed", execTime.Elapsed, ex);
                throw;
            }
        }
    }
}
