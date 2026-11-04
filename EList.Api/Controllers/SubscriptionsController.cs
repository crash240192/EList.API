using EList.Api.Extensions;
using EList.Common.Logger;
using EList.Common.Models;
using EList.DbDataProvider.Interfaces;
using EList.Models.Subscriptions;
using EList.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EList.Api.Controllers
{
    [Authorize]
    [ApiController]
    [Route("/api/subscriptions")]
    [LoggerHandlerWebApiFilter]
    public class SubscriptionsController : ControllerBase
    {
        private readonly ISubscriptionsService _subscriptionsService;
        private readonly IDataConnectionProvider _connectionProvider;

        public SubscriptionsController(ISubscriptionsService subscriptionsService, IDataConnectionProvider connectionProvider)
        {
            _subscriptionsService = subscriptionsService;
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
            try
            {
                await _connectionProvider.StartNewTransactionAsync();
                
                var result = await _subscriptionsService.SubscribeToAccountAsync(accountId);
                if (!result.Success)
                    await _connectionProvider.RollbackTransactionAsync();

                return result;
            }
            catch
            {
                await _connectionProvider.RollbackTransactionAsync();
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
            try
            {
                await _connectionProvider.StartNewTransactionAsync();
                
                var result = await _subscriptionsService.UpdateSubscriptionAsync(accountId, request);
                if (!result.Success)
                    await _connectionProvider.RollbackTransactionAsync();

                return result;
            }
            catch
            {
                await _connectionProvider.RollbackTransactionAsync();
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
            var result = await _subscriptionsService.GetSubscriptionsAsync(request);
            return result;
        }

        /// <summary>
        /// Количество подписок пользователя
        /// </summary>
        /// <param name="accountId"></param>
        /// <returns></returns>
        [HttpGet("getSubscriptionsCount/{accountId}")]
        public async Task<CommandResult<int>> GetSubscriptionsCountAsync(Guid accountId)
        {
            var result = await _subscriptionsService.GetSubscriptionsCountAsync(accountId);
            return result;

        }

        /// <summary>
        /// Подписан ли текущий пользователь на указанный аккаунт
        /// </summary>
        [HttpGet("isSubscribed/{accountId}")]
        public async Task<CommandResult<bool>> IsSubscribedAsync(Guid accountId)
        {
            var result = await _subscriptionsService.IsSubscribedAsync(accountId);
            return result;

        }

        /// <summary>
        /// Отобразить подписчиков пользователя
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        [HttpPost("getSubscribers")]
        public async Task<CommandResult<PagedList<Subscription>>> GetSubscribersAsync(SubscriptionsSearchRequest request)
        {
            var result = await _subscriptionsService.GetSubscribersAsync(request);
            return result;

        }

        /// <summary>
        /// Количество подписчиков пользователя
        /// </summary>
        /// <param name="accountId"></param>
        /// <returns></returns>
        [HttpGet("getSubscribersCount/{accountId}")]
        public async Task<CommandResult<int>> GetSubscribersCountAsync(Guid accountId)
        {
            var result = await _subscriptionsService.GetSubscribersCountAsync(accountId);
            return result;

        }

        /// <summary>
        /// Отобразить подписчиков пользователя
        /// </summary>
        /// <param name="accountId"></param>
        /// <returns></returns>
        [HttpDelete("deleteSubscription/{accountId}")]
        public async Task<CommandResult> DeleteSubscriptionAsync(Guid accountId)
        {
            try
            {
                await _connectionProvider.StartNewTransactionAsync();

                var result = await _subscriptionsService.DeleteSubscriptionAsync(accountId);
                if (!result.Success)
                    await _connectionProvider.RollbackTransactionAsync();

                return result;
            }
            catch
            {
                await _connectionProvider.RollbackTransactionAsync();
                throw;
            }
        }
    }
}
