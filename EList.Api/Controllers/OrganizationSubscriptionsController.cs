using System.Diagnostics;
using EList.Common.CorrelationId;
using EList.Common.Logger;
using EList.Common.Models;
using EList.DbDataProvider.Interfaces;
using EList.Models.Subscriptions;
using EList.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NLog;
using TM.Schedule.API.Attributes;

namespace EList.Api.Controllers
{
    [Authorize]
    [ApiController]
    [Route("/api/organizationSubscriptions")]
    [LoggerHandlerWebApiFilter]
    public class OrganizationSubscriptionsController : ControllerBase
    {
        #region logger
        private static readonly NLog.ILogger log = LogManager.GetCurrentClassLogger();
        private static readonly ILoggerWrapper logger = new NLogLoggerWrapper(log);
        private const string LOGGER_NAME = "EList.Api.Controllers.OrganizationSubscriptionsController.";
        #endregion

        private readonly IOrganizationSubscriptionsService _organizationSubscriptionsService;
        private readonly ICorrelationIdProvider _correlationIdProvider;
        private readonly IDataConnectionProvider _connectionProvider;

        public OrganizationSubscriptionsController(
            IOrganizationSubscriptionsService organizationSubscriptionsService,
            ICorrelationIdProvider correlationIdProvider,
            IDataConnectionProvider connectionProvider)
        {
            _organizationSubscriptionsService = organizationSubscriptionsService;
            _correlationIdProvider = correlationIdProvider;
            _connectionProvider = connectionProvider;
        }

        /// <summary>Подписаться на организацию</summary>
        [HttpGet("subscribe/{organizationId}")]
        public async Task<CommandResult> SubscribeAsync(Guid organizationId)
        {
            var correlationId = _correlationIdProvider.Get();
            var execTime = Stopwatch.StartNew();
            var methodName = $"{LOGGER_NAME}{nameof(SubscribeAsync)}";

            try
            {
                await _connectionProvider.StartNewTransactionAsync();
                logger.Debug(correlationId, null, methodName, "Method started", null);

                var result = await _organizationSubscriptionsService.SubscribeAsync(organizationId);
                if (!result.Success)
                    await _connectionProvider.RollbackTransactionAsync();

                logger.Debug(correlationId, null, methodName, "Method finished", null, execTime.Elapsed);
                return result;
            }
            catch (Exception ex)
            {
                await _connectionProvider.RollbackTransactionAsync();
                ExceptionLogger.LogException(logger, correlationId, methodName, "Method failed", execTime.Elapsed, ex);
                throw;
            }
        }

        /// <summary>Обновить параметры подписки на организацию</summary>
        [HttpPut("update/{organizationId}")]
        public async Task<CommandResult> UpdateAsync(
            [FromRoute] Guid organizationId,
            [FromBody] UpdateOrganizationSubscriptionRequest request)
        {
            var correlationId = _correlationIdProvider.Get();
            var execTime = Stopwatch.StartNew();
            var methodName = $"{LOGGER_NAME}{nameof(UpdateAsync)}";

            try
            {
                await _connectionProvider.StartNewTransactionAsync();
                logger.Debug(correlationId, null, methodName, "Method started", null);

                var result = await _organizationSubscriptionsService.UpdateAsync(organizationId, request);
                if (!result.Success)
                    await _connectionProvider.RollbackTransactionAsync();

                logger.Debug(correlationId, null, methodName, "Method finished", null, execTime.Elapsed);
                return result;
            }
            catch (Exception ex)
            {
                await _connectionProvider.RollbackTransactionAsync();
                ExceptionLogger.LogException(logger, correlationId, methodName, "Method failed", execTime.Elapsed, ex);
                throw;
            }
        }

        /// <summary>Подписки пользователя на организации</summary>
        [HttpPost("getSubscriptions")]
        public async Task<CommandResult<PagedList<OrganizationSubscription>>> GetSubscriptionsAsync(
            OrganizationSubscriptionsSearchRequest request)
        {
            var correlationId = _correlationIdProvider.Get();
            var execTime = Stopwatch.StartNew();
            var methodName = $"{LOGGER_NAME}{nameof(GetSubscriptionsAsync)}";

            try
            {
                logger.Debug(correlationId, null, methodName, "Method started", null);
                var result = await _organizationSubscriptionsService.GetSubscriptionsAsync(request);
                logger.Debug(correlationId, null, methodName, "Method finished", null, execTime.Elapsed);
                return result;
            }
            catch (Exception ex)
            {
                ExceptionLogger.LogException(logger, correlationId, methodName, "Method failed", execTime.Elapsed, ex);
                throw;
            }
        }

        /// <summary>Количество подписок пользователя на организации</summary>
        [HttpGet("getSubscriptionsCount/{accountId}")]
        public async Task<CommandResult<int>> GetSubscriptionsCountAsync(Guid accountId)
        {
            var correlationId = _correlationIdProvider.Get();
            var execTime = Stopwatch.StartNew();
            var methodName = $"{LOGGER_NAME}{nameof(GetSubscriptionsCountAsync)}";

            try
            {
                logger.Debug(correlationId, null, methodName, "Method started", null);
                var result = await _organizationSubscriptionsService.GetSubscriptionsCountAsync(accountId);
                logger.Debug(correlationId, null, methodName, "Method finished", null, execTime.Elapsed);
                return result;
            }
            catch (Exception ex)
            {
                ExceptionLogger.LogException(logger, correlationId, methodName, "Method failed", execTime.Elapsed, ex);
                throw;
            }
        }

        /// <summary>Подписан ли текущий пользователь на организацию</summary>
        [HttpGet("isSubscribed/{organizationId}")]
        public async Task<CommandResult<bool>> IsSubscribedAsync(Guid organizationId)
        {
            var correlationId = _correlationIdProvider.Get();
            var execTime = Stopwatch.StartNew();
            var methodName = $"{LOGGER_NAME}{nameof(IsSubscribedAsync)}";

            try
            {
                logger.Debug(correlationId, null, methodName, "Method started", null);
                var result = await _organizationSubscriptionsService.IsSubscribedAsync(organizationId);
                logger.Debug(correlationId, null, methodName, "Method finished", null, execTime.Elapsed);
                return result;
            }
            catch (Exception ex)
            {
                ExceptionLogger.LogException(logger, correlationId, methodName, "Method failed", execTime.Elapsed, ex);
                throw;
            }
        }

        /// <summary>Подписчики организации</summary>
        [HttpPost("getSubscribers")]
        public async Task<CommandResult<PagedList<OrganizationSubscription>>> GetSubscribersAsync(
            OrganizationSubscriptionsSearchRequest request)
        {
            var correlationId = _correlationIdProvider.Get();
            var execTime = Stopwatch.StartNew();
            var methodName = $"{LOGGER_NAME}{nameof(GetSubscribersAsync)}";

            try
            {
                logger.Debug(correlationId, null, methodName, "Method started", null);
                var result = await _organizationSubscriptionsService.GetSubscribersAsync(request);
                logger.Debug(correlationId, null, methodName, "Method finished", null, execTime.Elapsed);
                return result;
            }
            catch (Exception ex)
            {
                ExceptionLogger.LogException(logger, correlationId, methodName, "Method failed", execTime.Elapsed, ex);
                throw;
            }
        }

        /// <summary>Количество подписчиков организации</summary>
        [HttpGet("getSubscribersCount/{organizationId}")]
        public async Task<CommandResult<int>> GetSubscribersCountAsync(Guid organizationId)
        {
            var correlationId = _correlationIdProvider.Get();
            var execTime = Stopwatch.StartNew();
            var methodName = $"{LOGGER_NAME}{nameof(GetSubscribersCountAsync)}";

            try
            {
                logger.Debug(correlationId, null, methodName, "Method started", null);
                var result = await _organizationSubscriptionsService.GetSubscribersCountAsync(organizationId);
                logger.Debug(correlationId, null, methodName, "Method finished", null, execTime.Elapsed);
                return result;
            }
            catch (Exception ex)
            {
                ExceptionLogger.LogException(logger, correlationId, methodName, "Method failed", execTime.Elapsed, ex);
                throw;
            }
        }

        /// <summary>Отписаться от организации</summary>
        [HttpDelete("deleteSubscription/{organizationId}")]
        public async Task<CommandResult> DeleteAsync(Guid organizationId)
        {
            var correlationId = _correlationIdProvider.Get();
            var execTime = Stopwatch.StartNew();
            var methodName = $"{LOGGER_NAME}{nameof(DeleteAsync)}";

            try
            {
                await _connectionProvider.StartNewTransactionAsync();
                logger.Debug(correlationId, null, methodName, "Method started", null);

                var result = await _organizationSubscriptionsService.DeleteAsync(organizationId);
                if (!result.Success)
                    await _connectionProvider.RollbackTransactionAsync();

                logger.Debug(correlationId, null, methodName, "Method finished", null, execTime.Elapsed);
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