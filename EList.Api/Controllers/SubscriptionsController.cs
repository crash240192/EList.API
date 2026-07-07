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
        /// <param name="accountId"></param>
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
        /// Обновить параметры подписки на пользователя
        /// </summary>
        /// <param name="accountId"></param>
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
        /// Отобразить подписки пользователя
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        [HttpPost("getSubscriptions")]
        public async Task<CommandResult<PagedList<Subscription>>> GetSubscriptionsAsync(SubscriptionsSearchRequest request)
        {
            var correlationId = _correlationIdProvider.Get();
            var execTime = Stopwatch.StartNew();
            var methodName = $"{LOGGER_NAME}{nameof(GetSubscriptionsAsync)}";

            try
            {
                logger.Debug(correlationId, null, methodName, $"Method started", null);

                var result = await _subscriptionsService.GetSubscriptionsAsync(request);
                
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
        /// Количество подписок пользователя
        /// </summary>
        /// <param name="accountId"></param>
        /// <returns></returns>
        [HttpGet("getSubscriptionsCount/{accountId}")]
        public async Task<CommandResult<int>> GetSubscriptionsCountAsync(Guid accountId)
        {
            var correlationId = _correlationIdProvider.Get();
            var execTime = Stopwatch.StartNew();
            var methodName = $"{LOGGER_NAME}{nameof(GetSubscriptionsCountAsync)}";

            try
            {
                logger.Debug(correlationId, null, methodName, $"Method started", null);

                var result = await _subscriptionsService.GetSubscriptionsCountAsync(accountId);

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
        [HttpPost("getSubscribers")]
        public async Task<CommandResult<PagedList<Subscription>>> GetSubscribersAsync(SubscriptionsSearchRequest request)
        {
            var correlationId = _correlationIdProvider.Get();
            var execTime = Stopwatch.StartNew();
            var methodName = $"{LOGGER_NAME}{nameof(GetSubscribersAsync)}";

            try
            {
                logger.Debug(correlationId, null, methodName, $"Method started", null);

                var result = await _subscriptionsService.GetSubscribersAsync(request);

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
        /// Количество подписчиков пользователя
        /// </summary>
        /// <param name="accountId"></param>
        /// <returns></returns>
        [HttpGet("getSubscribersCount/{accountId}")]
        public async Task<CommandResult<int>> GetSubscribersCountAsync(Guid accountId)
        {
            var correlationId = _correlationIdProvider.Get();
            var execTime = Stopwatch.StartNew();
            var methodName = $"{LOGGER_NAME}{nameof(GetSubscribersCountAsync)}";

            try
            {
                logger.Debug(correlationId, null, methodName, $"Method started", null);

                var result = await _subscriptionsService.GetSubscribersCountAsync(accountId);

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
        /// <param name="accountId"></param>
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
    }
}
