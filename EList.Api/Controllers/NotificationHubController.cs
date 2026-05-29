using EList.Api.Infrastructure;
using EList.Common.CorrelationId;
using EList.Common.Logger;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NLog;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;

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

        private readonly WebSocketConnectionManager _connectionManager;
        private readonly ICorrelationIdProvider _correlationIdProvider;

        public NotificationHubController(
            WebSocketConnectionManager connectionManager,
            ICorrelationIdProvider correlationIdProvider)
        {
            _connectionManager = connectionManager;
            _correlationIdProvider = correlationIdProvider;
        }

        // ──────────────────────────────────────────────
        //  1. WebSocket-эндпоинт: подключение клиента
        // ──────────────────────────────────────────────

        /// <summary>
        /// Установить WebSocket-соединение для получения уведомлений.
        ///
        /// URL: ws(s)://host/eList/ws/notifications?accountId={guid}
        ///
        /// После подключения сервер будет отправлять JSON-сообщения вида:
        /// { "type": "notification", "payload": { ... } }
        ///
        /// Клиент может отправлять:
        /// { "type": "ping" }  — сервер ответит { "type": "pong" }
        /// </summary>
        [AllowAnonymous]
        [Route("/ws/notifications")]
        [ApiExplorerSettings(IgnoreApi = true)]
        public async Task ConnectWebSocket([FromQuery] Guid accountId)
        {
            var correlationId = _correlationIdProvider.Get();
            var methodName = $"{LOGGER_NAME}{nameof(ConnectWebSocket)}";

            if (!HttpContext.WebSockets.IsWebSocketRequest)
            {
                logger.Debug(correlationId, null, methodName, "Rejected non-WebSocket request", null);
                HttpContext.Response.StatusCode = StatusCodes.Status400BadRequest;
                await HttpContext.Response.WriteAsync("Ожидается WebSocket-соединение");
                return;
            }

            if (accountId == Guid.Empty)
            {
                HttpContext.Response.StatusCode = StatusCodes.Status400BadRequest;
                await HttpContext.Response.WriteAsync("Параметр accountId обязателен");
                return;
            }

            var socket = await HttpContext.WebSockets.AcceptWebSocketAsync();
            var connectionId = _connectionManager.AddConnection(accountId, socket);

            logger.Debug(correlationId, null, methodName,
                $"WebSocket connected: accountId={accountId}, connectionId={connectionId}", null);

            // Отправляем приветственное сообщение
            var welcome = new
            {
                type = "connected",
                connectionId,
                message = "Соединение установлено. Ожидайте уведомления."
            };
            await SendJsonAsync(socket, welcome);

            // Цикл чтения сообщений от клиента (удерживает соединение открытым)
            await ReceiveLoopAsync(socket, accountId, connectionId, correlationId);
        }

        /// <summary>
        /// Цикл чтения входящих сообщений от клиента.
        /// Поддерживает ping/pong и graceful-закрытие.
        /// </summary>
        private async Task ReceiveLoopAsync(WebSocket socket, Guid accountId, string connectionId, string correlationId)
        {
            var methodName = $"{LOGGER_NAME}{nameof(ReceiveLoopAsync)}";
            var buffer = new byte[4 * 1024];

            try
            {
                while (socket.State == WebSocketState.Open)
                {
                    var result = await socket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);

                    if (result.MessageType == WebSocketMessageType.Close)
                    {
                        logger.Debug(correlationId, null, methodName,
                            $"Client requested close: accountId={accountId}", null);

                        await socket.CloseAsync(
                            WebSocketCloseStatus.NormalClosure,
                            "Закрытие по запросу клиента",
                            CancellationToken.None);
                        break;
                    }

                    if (result.MessageType == WebSocketMessageType.Text)
                    {
                        var message = Encoding.UTF8.GetString(buffer, 0, result.Count);
                        await HandleClientMessageAsync(socket, message, accountId, correlationId);
                    }
                }
            }
            catch (WebSocketException ex)
            {
                logger.Error(correlationId, null, methodName,
                    $"WebSocket error: accountId={accountId}, error={ex.Message}", null, ex, null);
            }
            finally
            {
                _connectionManager.RemoveConnection(accountId, connectionId);
                logger.Debug(correlationId, null, methodName,
                    $"WebSocket disconnected: accountId={accountId}, connectionId={connectionId}", null);
            }
        }

        /// <summary>
        /// Обработка входящего сообщения от клиента.
        /// Сейчас поддерживает только ping → pong.
        /// Сюда можно добавлять свои типы сообщений.
        /// </summary>
        private async Task HandleClientMessageAsync(WebSocket socket, string rawMessage, Guid accountId, string correlationId)
        {
            var methodName = $"{LOGGER_NAME}{nameof(HandleClientMessageAsync)}";

            try
            {
                using var doc = JsonDocument.Parse(rawMessage);
                var type = doc.RootElement.TryGetProperty("type", out var typeProp)
                    ? typeProp.GetString()
                    : null;

                switch (type)
                {
                    case "ping":
                        await SendJsonAsync(socket, new { type = "pong", timestamp = DateTimeOffset.UtcNow });
                        break;

                    default:
                        logger.Debug(correlationId, null, methodName,
                            $"Unknown message type '{type}' from accountId={accountId}", null);
                        await SendJsonAsync(socket, new
                        {
                            type = "error",
                            message = $"Неизвестный тип сообщения: '{type}'"
                        });
                        break;
                }
            }
            catch (JsonException)
            {
                await SendJsonAsync(socket, new
                {
                    type = "error",
                    message = "Некорректный JSON"
                });
            }
        }

        // ──────────────────────────────────────────────
        //  2. REST-эндпоинт: отправка уведомления конкретному пользователю
        // ──────────────────────────────────────────────

        /// <summary>
        /// Отправить уведомление конкретному пользователю по accountId.
        /// Сообщение будет доставлено во все активные WebSocket-соединения этого аккаунта.
        /// </summary>
        /// <param name="accountId">Идентификатор аккаунта-получателя</param>
        /// <param name="request">Тело уведомления</param>
        [AllowAnonymous]
        [HttpPost("send/{accountId}")]
        public async Task<IActionResult> SendToUserAsync(Guid accountId, [FromBody] NotificationRequest request)
        {
            var correlationId = _correlationIdProvider.Get();
            var methodName = $"{LOGGER_NAME}{nameof(SendToUserAsync)}";

            var sockets = _connectionManager.GetConnections(accountId).ToList();

            if (!sockets.Any())
            {
                logger.Debug(correlationId, null, methodName,
                    $"No active connections for accountId={accountId}", null);
                return Ok(new { success = false, message = "Нет активных соединений для данного аккаунта" });
            }

            var payload = new
            {
                type = "notification",
                payload = new
                {
                    title = request.Title,
                    body = request.Body,
                    data = request.Data,
                    timestamp = DateTimeOffset.UtcNow
                }
            };

            var sent = 0;
            foreach (var socket in sockets)
            {
                try
                {
                    await SendJsonAsync(socket, payload);
                    sent++;
                }
                catch (WebSocketException ex)
                {
                    logger.Error(correlationId, null, methodName,
                        $"Failed to send to one connection: {ex.Message}", null, ex, null);
                }
            }

            logger.Debug(correlationId, null, methodName,
                $"Notification sent to accountId={accountId}, delivered to {sent}/{sockets.Count} connections", null);

            return Ok(new { success = true, connectionsDelivered = sent, connectionsTotal = sockets.Count });
        }

        // ──────────────────────────────────────────────
        //  3. REST-эндпоинт: рассылка уведомления всем подключённым
        // ──────────────────────────────────────────────

        /// <summary>
        /// Отправить уведомление всем подключённым WebSocket-клиентам (broadcast).
        /// </summary>
        /// <param name="request">Тело уведомления</param>
        [AllowAnonymous]
        [HttpPost("broadcast")]
        public async Task<IActionResult> BroadcastAsync([FromBody] NotificationRequest request)
        {
            var correlationId = _correlationIdProvider.Get();
            var methodName = $"{LOGGER_NAME}{nameof(BroadcastAsync)}";

            var allSockets = _connectionManager.GetAllConnections().ToList();

            if (!allSockets.Any())
                return Ok(new { success = false, message = "Нет подключённых клиентов" });

            var payload = new
            {
                type = "broadcast",
                payload = new
                {
                    title = request.Title,
                    body = request.Body,
                    data = request.Data,
                    timestamp = DateTimeOffset.UtcNow
                }
            };

            var sent = 0;
            foreach (var socket in allSockets)
            {
                try
                {
                    await SendJsonAsync(socket, payload);
                    sent++;
                }
                catch (WebSocketException ex)
                {
                    logger.Error(correlationId, null, methodName,
                        $"Failed to send broadcast to one connection: {ex.Message}", null, ex, null);
                }
            }

            logger.Debug(correlationId, null, methodName,
                $"Broadcast sent to {sent}/{allSockets.Count} connections", null);

            return Ok(new { success = true, connectionsDelivered = sent, connectionsTotal = allSockets.Count });
        }

        // ──────────────────────────────────────────────
        //  4. REST-эндпоинт: статистика подключений
        // ──────────────────────────────────────────────

        /// <summary>
        /// Получить статистику по активным WebSocket-соединениям
        /// </summary>
        [AllowAnonymous]
        [HttpGet("connections/stats")]
        public IActionResult GetConnectionStats()
        {
            return Ok(new
            {
                connectedAccounts = _connectionManager.ConnectedAccountsCount,
                totalConnections = _connectionManager.TotalConnectionsCount
            });
        }

        // ──────────────────────────────────────────────
        //  Вспомогательные методы
        // ──────────────────────────────────────────────

        private static async Task SendJsonAsync(WebSocket socket, object data)
        {
            if (socket.State != WebSocketState.Open)
                return;

            var json = JsonSerializer.Serialize(data, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                WriteIndented = false
            });

            var bytes = Encoding.UTF8.GetBytes(json);
            await socket.SendAsync(
                new ArraySegment<byte>(bytes),
                WebSocketMessageType.Text,
                endOfMessage: true,
                CancellationToken.None);
        }
    }

    // ──────────────────────────────────────────────
    //  Модель запроса
    // ──────────────────────────────────────────────

    /// <summary>
    /// Модель запроса на отправку уведомления
    /// </summary>
    public class NotificationRequest
    {
        /// <summary>
        /// Заголовок уведомления
        /// </summary>
        public string? Title { get; set; }

        /// <summary>
        /// Текст уведомления
        /// </summary>
        public string? Body { get; set; }

        /// <summary>
        /// Произвольные данные (JSON-объект), передаваемые вместе с уведомлением
        /// </summary>
        public object? Data { get; set; }
    }
}
