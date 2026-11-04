using EList.Common.Logger;
using EList.Common.Models;
using EList.DbDataProvider.Interfaces;
using EList.Models.Accounts;
using EList.Models.Notifications;
using EList.Services.Impl;
using EList.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EList.Api.Controllers
{
    /// <summary>
    /// Контроллер для работы с WebSocket-уведомлениями.
    ///
    /// Содержит:
    /// 1. WebSocket-эндпоинт для установки постоянного соединения с клиентом
    /// 2. REST-эндпоинты для отправки уведомлений подключённым клиентам
    ///
    /// Пример подключения из JavaScript:
    /// <code>
    /// const ws = new WebSocket("wss://localhost:7020/eList/ws/notifications?accountId=YOUR_ACCOUNT_GUID");
    /// ws.onmessage = (event) => console.log("Получено:", event.data);
    /// ws.onopen = () => ws.send(JSON.stringify({ type: "ping" }));
    /// </code>
    /// </summary>
    [ApiController]
    [Route("/api/notifications")]
    [Authorize]
    [LoggerHandlerWebApiFilter]
    public class NotificationHubController : ControllerBase
    {
        private readonly INotificationsService _notificationService;
        private readonly IAccountDataHolder _accountDataHolder;
        private readonly IDataConnectionProvider _connectionProvider;

        public NotificationHubController(
            INotificationsService notificationService, IAccountDataHolder accountDataHolder,
            INotificationsService notificationsService,
            IDataConnectionProvider connectionProvider)
        {
            _notificationService = notificationService;
            _accountDataHolder = accountDataHolder;
            _connectionProvider = connectionProvider;
        }

        /// <summary>
        /// Установить WebSocket-соединение для получения уведомлений.
        ///
        /// URL: ws(s)://host/eList/ws/notifications?authorization={...}&authorization-jwt={...}
        ///
        /// После подключения сервер будет отправлять JSON-сообщения вида:
        /// { "type": "notification", "payload": { ... } }
        ///
        /// Клиент может отправлять:
        /// { "type": "ping" }  — сервер ответит { "type": "pong" }
        /// </summary>
        //[AllowAnonymous]
        [Route("/ws/notifications")]
        [ApiExplorerSettings(IgnoreApi = true)]
        public async Task ConnectWebSocket()
        {
            if (!HttpContext.WebSockets.IsWebSocketRequest)
            {
                HttpContext.Response.StatusCode = StatusCodes.Status400BadRequest;
                await HttpContext.Response.WriteAsync("Ожидается WebSocket-соединение");
                return;
            }

            var socket = await HttpContext.WebSockets.AcceptWebSocketAsync();
            await _notificationService.AddConnectionAsync(socket);

        }

        /// <summary>
        /// Отправить уведомление конкретному пользователю по accountId.
        /// Сообщение будет доставлено во все активные WebSocket-соединения этого аккаунта.
        /// </summary>
        /// <param name="accountId">Идентификатор аккаунта-получателя</param>
        [HttpPost("send/{accountId}")]
        public async Task<CommandResult> SendToUserAsync(Guid accountId, Notification request)
        {
            if (!_accountDataHolder.IsPlatformAdminOrAbove)
                return CommandResult.Fail(EList.Common.Support.ErrorCode.AccessError, "Отправка уведомлений доступна только администраторам площадки");

            var result = await _notificationService.SendToUserAsync(accountId, request);

            return result;
        }

        
        /// <summary>
        /// Отправить уведомление всем подключённым WebSocket-клиентам (broadcast).
        /// </summary>
        /// <param name="request">Тело уведомления</param>
        [HttpPost("broadcast")]
        public async Task<CommandResult> BroadcastAsync(Notification request)
        {
            if (!_accountDataHolder.IsPlatformAdminOrAbove)
                return CommandResult.Fail(EList.Common.Support.ErrorCode.AccessError, "Broadcast уведомлений доступен только администраторам площадки");
            
            var result = await _notificationService.BroadcastAsync(request);

            return result;
        }

        /// <summary>
        /// Получить статистику по активным WebSocket-соединениям
        /// </summary>
        [HttpGet("connections/stats")]
        public CommandResult<ConnectionStats> GetConnectionStats()
        {
            if (!_accountDataHolder.IsPlatformAdminOrAbove)
                return CommandResult<ConnectionStats>.Fail(EList.Common.Support.ErrorCode.AccessError, "Статистика соединений доступна только администраторам площадки");

            var result = _notificationService.GetConnectionStats();

            return result;
        }

        /// <summary>
        /// Какие из переданных аккаунтов сейчас онлайн (есть открытый notifications WebSocket)
        /// </summary>
        [HttpGet("online")]
        public CommandResult<OnlineAccountsResponse> GetOnlineAccounts([FromQuery] string? ids = null)
        {
            var parsed = new List<Guid>();
            if (!string.IsNullOrWhiteSpace(ids))
            {
                foreach (var part in ids.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
                {
                    if (Guid.TryParse(part, out var id))
                        parsed.Add(id);
                }
            }

            var result = _notificationService.GetOnlineAccounts(parsed);

            return result;
        }

        /// <summary>
        /// История уведомлений текущего пользователя (входящие, включая замечания модерации)
        /// </summary>
        [HttpGet("my")]
        public async Task<CommandResult<PagedList<Notification>>> GetMyNotificationsAsync(
            [FromQuery] UserNotificationType? type = null,
            [FromQuery] bool unreadOnly = false,
            [FromQuery] int? pageIndex = 0,
            [FromQuery] int? pageSize = 20)
        {
            var result = await _notificationService.GetMyNotificationsAsync(new NotificationsSearchRequest
            {
                Type = type,
                UnreadOnly = unreadOnly,
                PageIndex = pageIndex,
                PageSize = pageSize
            });

            return result;
        }

        /// <summary>
        /// Счётчик уведомлений текущего пользователя
        /// </summary>
        [HttpGet("my/count")]
        public async Task<CommandResult<int>> CountMyNotificationsAsync(
            [FromQuery] UserNotificationType? type = null,
            [FromQuery] bool unreadOnly = true)
        {
            return await _notificationService.CountMyNotificationsAsync(type, unreadOnly);
        }

        /// <summary>
        /// Отметить оповещение как прочитанное
        /// </summary>
        [HttpGet("read/{notificationId}")]
        public async Task<CommandResult> ReadNotificationAsync(Guid notificationId)
        {
            try
            {
                await _connectionProvider.StartNewTransactionAsync();

                var result = await _notificationService.ReadNotificationAsync(notificationId);
                if (!result.Success)
                    await _connectionProvider.RollbackTransactionAsync();
                else
                    await _connectionProvider.CommitTransactionAsync();

                return result;
            }
            catch
            {
                await _connectionProvider.RollbackTransactionAsync();
                throw;
            }
        }

        /// <summary>
        /// Отметить все оповещения пользователя как прочитанные
        /// </summary>
        [HttpGet("read/all")]
        public async Task<CommandResult> ReadAllUserNotificationsAsync()
        {
            try
            {
                await _connectionProvider.StartNewTransactionAsync();

                var result = await _notificationService.ReadAllUserNotificationsAsync();
                if (!result.Success)
                    await _connectionProvider.RollbackTransactionAsync();
                else
                    await _connectionProvider.CommitTransactionAsync();

                return result;
            }
            catch
            {
                await _connectionProvider.RollbackTransactionAsync();
                throw;
            }
        }

        ///// <summary>
        ///// Отметить все оповещения пользователя как прочитанные
        ///// </summary>
        //[HttpGet("read/all")]
        //public async Task<CommandResult<PagedList<Notification>>> ReadAllUserNotificationsAsync()
        //{
        //        //        //        //
        //    var result = await _notificationService.ReadAllUserNotificationsAsync();

        //        //    return result;
        //}
    }
}
