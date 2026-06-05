using System.Diagnostics;
using EList.Common.CorrelationId;
using EList.Common.Logger;
using EList.Common.Models;
using EList.DbDataProvider.Interfaces;
using EList.Models.Accounts;
using EList.Models.Notifications;
using EList.Services.Impl;
using EList.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NLog;

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
    public class NotificationHubController : ControllerBase
    {
        #region logger
        private static readonly NLog.ILogger log = LogManager.GetCurrentClassLogger();
        private static readonly ILoggerWrapper logger = new NLogLoggerWrapper(log);
        private const string LOGGER_NAME = "EList.Api.Controllers.NotificationHubController.";
        #endregion

        private readonly INotificationsService _notificationService;
        private readonly ICorrelationIdProvider _correlationIdProvider;
        private readonly IAccountDataHolder _accountDataHolder;
        private readonly IDataConnectionProvider _connectionProvider;

        public NotificationHubController(
            INotificationsService notificationService,
            ICorrelationIdProvider correlationIdProvider,
            IAccountDataHolder accountDataHolder,
            INotificationsService notificationsService,
            IDataConnectionProvider connectionProvider)
        {
            _notificationService = notificationService;
            _correlationIdProvider = correlationIdProvider;
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
            var correlationId = _correlationIdProvider.Get();
            var methodName = $"{LOGGER_NAME}{nameof(ConnectWebSocket)}";
            var execTime = Stopwatch.StartNew();

            logger.Debug(correlationId, null, methodName, $"Method started", null);

            if (!HttpContext.WebSockets.IsWebSocketRequest)
            {
                logger.Debug(correlationId, null, methodName, "Rejected non-WebSocket request", null);
                HttpContext.Response.StatusCode = StatusCodes.Status400BadRequest;
                await HttpContext.Response.WriteAsync("Ожидается WebSocket-соединение");
                return;
            }

            var socket = await HttpContext.WebSockets.AcceptWebSocketAsync();
            await _notificationService.AddConnectionAsync(socket);

            logger.Debug(correlationId, null, methodName, $"Method finished", null, execTime.Elapsed);
        }

        /// <summary>
        /// Отправить уведомление конкретному пользователю по accountId.
        /// Сообщение будет доставлено во все активные WebSocket-соединения этого аккаунта.
        /// </summary>
        /// <param name="accountId">Идентификатор аккаунта-получателя</param>
        [AllowAnonymous]
        [HttpPost("send/{accountId}")]
        public async Task<CommandResult> SendToUserAsync(Guid accountId, Notification request)
        {
            var correlationId = _correlationIdProvider.Get();
            var methodName = $"{LOGGER_NAME}{nameof(SendToUserAsync)}";
            var execTime = Stopwatch.StartNew();
            logger.Debug(correlationId, null, methodName, $"Method started", null);

            var result = await _notificationService.SendToUserAsync(accountId, request);

            logger.Debug(correlationId, null, methodName, $"Method finished", null, execTime.Elapsed);

            return result;
        }

        
        /// <summary>
        /// Отправить уведомление всем подключённым WebSocket-клиентам (broadcast).
        /// </summary>
        /// <param name="request">Тело уведомления</param>
        [HttpPost("broadcast")]
        public async Task<CommandResult> BroadcastAsync(Notification request)
        {
            var correlationId = _correlationIdProvider.Get();
            var methodName = $"{LOGGER_NAME}{nameof(BroadcastAsync)}";
            var execTime = Stopwatch.StartNew();
            logger.Debug(correlationId, null, methodName, $"Method started", null);
            
            var result = await _notificationService.BroadcastAsync(request);

            logger.Debug(correlationId, null, methodName, $"Method finished", null, execTime.Elapsed);

            return result;
        }

        /// <summary>
        /// Получить статистику по активным WebSocket-соединениям
        /// </summary>
        [HttpGet("connections/stats")]
        public CommandResult<ConnectionStats> GetConnectionStats()
        {
            var correlationId = _correlationIdProvider.Get();
            var methodName = $"{LOGGER_NAME}{nameof(GetConnectionStats)}";
            var execTime = Stopwatch.StartNew();
            logger.Debug(correlationId, null, methodName, $"Method started", null);

            var result = _notificationService.GetConnectionStats();

            logger.Debug(correlationId, null, methodName, $"Method finished", null, execTime.Elapsed);
            return result;
        }


        /// <summary>
        /// Отметить оповещение как прочитанное
        /// </summary>
        [HttpGet("read/{notificationId}")]
        public async Task<CommandResult> ReadNotificationAsync(Guid notificationId)
        {
            var correlationId = _correlationIdProvider.Get();
            var methodName = $"{LOGGER_NAME}{nameof(ReadNotificationAsync)}";
            var execTime = Stopwatch.StartNew();
            try
            {
                logger.Debug(correlationId, null, methodName, $"Method started", null);
                await _connectionProvider.StartNewTransactionAsync();

                var result = await _notificationService.ReadNotificationAsync(notificationId);
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
        /// Отметить все оповещения пользователя как прочитанные
        /// </summary>
        [HttpGet("read/all")]
        public async Task<CommandResult> ReadAllUserNotificationsAsync()
        {
            var correlationId = _correlationIdProvider.Get();
            var methodName = $"{LOGGER_NAME}{nameof(ReadAllUserNotificationsAsync)}";
            var execTime = Stopwatch.StartNew();
            try
            {
                logger.Debug(correlationId, null, methodName, $"Method started", null);
                await _connectionProvider.StartNewTransactionAsync();

                var result = await _notificationService.ReadAllUserNotificationsAsync();
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


        ///// <summary>
        ///// Отметить все оповещения пользователя как прочитанные
        ///// </summary>
        //[HttpGet("read/all")]
        //public async Task<CommandResult<PagedList<Notification>>> ReadAllUserNotificationsAsync()
        //{
        //    var correlationId = _correlationIdProvider.Get();
        //    var methodName = $"{LOGGER_NAME}{nameof(ReadAllUserNotificationsAsync)}";
        //    var execTime = Stopwatch.StartNew();
        //    logger.Debug(correlationId, null, methodName, $"Method started", null);

        //    var result = await _notificationService.ReadAllUserNotificationsAsync();

        //    logger.Debug(correlationId, null, methodName, $"Method finished", null, execTime.Elapsed);
        //    return result;
        //}
    }
}
