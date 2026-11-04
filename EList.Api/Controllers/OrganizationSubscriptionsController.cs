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
    [Route("/api/organizationSubscriptions")]
    [LoggerHandlerWebApiFilter]
    public class OrganizationSubscriptionsController : ControllerBase
    {
        private readonly IOrganizationSubscriptionsService _organizationSubscriptionsService;
        private readonly IDataConnectionProvider _connectionProvider;

        public OrganizationSubscriptionsController(
            IOrganizationSubscriptionsService organizationSubscriptionsService, IDataConnectionProvider connectionProvider)
        {
            _organizationSubscriptionsService = organizationSubscriptionsService;
            _connectionProvider = connectionProvider;
        }

        /// <summary>Подписаться на организацию</summary>
        [HttpGet("subscribe/{organizationId}")]
        public async Task<CommandResult> SubscribeAsync(Guid organizationId)
        {
            try
            {
                await _connectionProvider.StartNewTransactionAsync();

                var result = await _organizationSubscriptionsService.SubscribeAsync(organizationId);
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

        /// <summary>Обновить параметры подписки на организацию</summary>
        [HttpPut("update/{organizationId}")]
        public async Task<CommandResult> UpdateAsync(
            [FromRoute] Guid organizationId,
            [FromBody] UpdateOrganizationSubscriptionRequest request)
        {
            try
            {
                await _connectionProvider.StartNewTransactionAsync();

                var result = await _organizationSubscriptionsService.UpdateAsync(organizationId, request);
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

        /// <summary>Подписки пользователя на организации</summary>
        [HttpPost("getSubscriptions")]
        public async Task<CommandResult<PagedList<OrganizationSubscription>>> GetSubscriptionsAsync(
            OrganizationSubscriptionsSearchRequest request)
        {
            var result = await _organizationSubscriptionsService.GetSubscriptionsAsync(request);
            return result;
        }

        /// <summary>Количество подписок пользователя на организации</summary>
        [HttpGet("getSubscriptionsCount/{accountId}")]
        public async Task<CommandResult<int>> GetSubscriptionsCountAsync(Guid accountId)
        {
            var result = await _organizationSubscriptionsService.GetSubscriptionsCountAsync(accountId);
            return result;

        }

        /// <summary>Подписан ли текущий пользователь на организацию</summary>
        [HttpGet("isSubscribed/{organizationId}")]
        public async Task<CommandResult<bool>> IsSubscribedAsync(Guid organizationId)
        {
            var result = await _organizationSubscriptionsService.IsSubscribedAsync(organizationId);
            return result;

        }

        /// <summary>Подписчики организации</summary>
        [HttpPost("getSubscribers")]
        public async Task<CommandResult<PagedList<OrganizationSubscription>>> GetSubscribersAsync(
            OrganizationSubscriptionsSearchRequest request)
        {
            var result = await _organizationSubscriptionsService.GetSubscribersAsync(request);
            return result;

        }

        /// <summary>Количество подписчиков организации</summary>
        [HttpGet("getSubscribersCount/{organizationId}")]
        public async Task<CommandResult<int>> GetSubscribersCountAsync(Guid organizationId)
        {
            var result = await _organizationSubscriptionsService.GetSubscribersCountAsync(organizationId);
            return result;

        }

        /// <summary>Отписаться от организации</summary>
        [HttpDelete("deleteSubscription/{organizationId}")]
        public async Task<CommandResult> DeleteAsync(Guid organizationId)
        {
            try
            {
                await _connectionProvider.StartNewTransactionAsync();

                var result = await _organizationSubscriptionsService.DeleteAsync(organizationId);
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