using EList.Common.CorrelationId;
using EList.Common.Extensions;
using EList.Common.Logger;
using EList.Common.Models;
using EList.Common.Support;
using EList.Common.TemplateParser;
using EList.Models.Accounts;
using EList.Models.Enums;
using EList.Repositories.Interfaces;
using EList.Services.Interfaces;
using EList.Sms;
using EList.Smtp;
using NLog;
using System.Diagnostics;
using System.Net.Mail;
using System.Net.Sockets;
using System.Net.WebSockets;
using System.Text.Json;
using System.Text;
using EList.Models.Notifications;
using Microsoft.AspNetCore.Mvc;
using NLog.Web.LayoutRenderers;
using EList.Common.Threading;
using System.Collections.Concurrent;

namespace EList.Services.Impl
{
    public class NotificationsService : INotificationsService
    {
        #region logger
        private static readonly ILogger log = LogManager.GetCurrentClassLogger();
        private static readonly ILoggerWrapper logger = new NLogLoggerWrapper(log);
        private const string LOGGER_NAME = "EList.Services.Impl.NotificationsService.";
        #endregion

        private readonly WebSocketConnectionManager _connectionManager;
        private readonly ICorrelationIdProvider _correlationIdProvider;
        private readonly IAccountDataHolder _accountDataHolder;
        private readonly INotificationsRepository _notificationsRepository;
        
        public NotificationsService(
            WebSocketConnectionManager connectionManager,
            ICorrelationIdProvider correlationIdProvider,
            IAccountDataHolder accountDataHolder,
            INotificationsRepository notificationsRepository)
        {
            _correlationIdProvider = correlationIdProvider ?? throw new ArgumentNullException(nameof(correlationIdProvider));
            _notificationsRepository = notificationsRepository ?? throw new ArgumentNullException(nameof(notificationsRepository));
            _connectionManager = connectionManager;
            _accountDataHolder = accountDataHolder;
        }



        public async Task<CommandResult> AddConnectionAsync(Guid accountId, WebSocket socket)
        {
            var correlationId = _correlationIdProvider.Get();
            var execTime = Stopwatch.StartNew();
            var methodName = $"{LOGGER_NAME}{nameof(AddConnectionAsync)}";

            logger.Debug(correlationId, null, methodName, $"Method started", null);

            _connectionManager.AddConnection(accountId, socket);

            var connectionId = _connectionManager.AddConnection(accountId, socket);

            logger.Debug(correlationId, null, methodName,
                $"WebSocket connected: accountId={accountId}, connectionId={connectionId}", null);

            var notifications = await _notificationsRepository.GetUnreadedUserNotificationsAsync(accountId);

            foreach (var notification in notifications)
            {
                await SendNotificationAsync(socket, notification);
            }

            // Цикл чтения сообщений от клиента (удерживает соединение открытым)
            await ReceiveLoopAsync(socket, accountId, connectionId, correlationId);

            logger.Debug(correlationId, null, methodName, $"Method finished", null, execTime.Elapsed);
            return CommandResult.OK;
        }

        public async Task<CommandResult> AddConnectionAsync(WebSocket socket)
        {
            var accountId = _accountDataHolder.AccountId;
            return await AddConnectionAsync(accountId, socket);
        }

        public CommandResult<ConnectionStats> GetConnectionStats()
        {
            var correlationId = _correlationIdProvider.Get();
            var execTime = Stopwatch.StartNew();
            var methodName = $"{LOGGER_NAME}{nameof(GetConnectionStats)}";

            logger.Debug(correlationId, null, methodName, $"Method started", null);

            var result = new CommandResult<ConnectionStats>(new ConnectionStats
            {
                ConnectedAccountCounts = _connectionManager.ConnectedAccountsCount,
                TotalConnectionsCount = _connectionManager.TotalConnectionsCount
            });

            logger.Debug(correlationId, null, methodName, $"Method finished", null, execTime.Elapsed);
            return result;
        }

        public async Task<CommandResult> HandleNewNotificationAsync(Notification notification)
        {
            var correlationId = _correlationIdProvider.Get();
            var methodName = $"{LOGGER_NAME}{nameof(HandleNewNotificationAsync)}";
            var execTime = Stopwatch.StartNew();
            logger.Debug(correlationId, null, methodName, $"Method started", null);

            notification.Id = await _notificationsRepository.CreateNotificationAsync(notification);

            var sendToUserResult = await SendToUserAsync(notification.AccountId, notification);

            logger.Debug(correlationId, null, methodName, $"Method finished", null, execTime.Elapsed);
            return sendToUserResult;
        }

        public async Task<CommandResult> ReadNotificationAsync(Guid notificationId)
        {
            var correlationId = _correlationIdProvider.Get();
            var methodName = $"{LOGGER_NAME}{nameof(ReadNotificationAsync)}";
            var execTime = Stopwatch.StartNew();
            logger.Debug(correlationId, null, methodName, $"Method started", null);

            await _notificationsRepository.ReadNotificationAsync(notificationId);

            logger.Debug(correlationId, null, methodName, $"Method finished", null, execTime.Elapsed);
            return CommandResult.OK;
        }

        public async Task<CommandResult> ReadAllUserNotificationsAsync()
        {
            var correlationId = _correlationIdProvider.Get();
            var methodName = $"{LOGGER_NAME}{nameof(ReadAllUserNotificationsAsync)}";
            var execTime = Stopwatch.StartNew();
            logger.Debug(correlationId, null, methodName, $"Method started", null);

            var accountId = _accountDataHolder.AccountId;

            await _notificationsRepository.ReadAllUserNotificationsAsync(accountId);

            logger.Debug(correlationId, null, methodName, $"Method finished", null, execTime.Elapsed);
            return CommandResult.OK;
        }

        public async Task<CommandResult> SendToUserAsync(Guid accountId, Notification notification)
        {
            var correlationId = _correlationIdProvider.Get();
            var methodName = $"{LOGGER_NAME}{nameof(SendToUserAsync)}";
            var execTime = Stopwatch.StartNew();
            logger.Debug(correlationId, null, methodName, $"Method started", null);

            var sockets = _connectionManager.GetConnections(accountId).ToList();

            if (!sockets.Any())
            {
                logger.Debug(correlationId, null, methodName,
                    $"No active connections for accountId={accountId}", null);
                return CommandResult.Fail(ErrorCode.NoActiveSocketConnections, "Нет активных соединений для данного аккаунта");
            }

            var sent = 0;
            foreach (var socket in sockets)
            {
                try
                {
                    await SendNotificationAsync(socket, notification);
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

            return CommandResult.OK;
            //return Ok(new { success = true, connectionsDelivered = sent, connectionsTotal = sockets.Count });
        }

        /// <summary>
        /// Отправить уведомление всем подключённым WebSocket-клиентам (broadcast).
        /// </summary>
        /// <param name="request">Тело уведомления</param>
        public async Task<CommandResult> BroadcastAsync(Notification request)
        {
            var correlationId = _correlationIdProvider.Get();
            var methodName = $"{LOGGER_NAME}{nameof(BroadcastAsync)}";
            var execTime = Stopwatch.StartNew();
            logger.Debug(correlationId, null, methodName, $"Method started", null);

            var allSockets = _connectionManager.GetAllConnections().ToList();

            if (!allSockets.Any())
                return CommandResult.Fail(ErrorCode.NoActiveSocketConnections, "Нет подключённых клиентов");

            var sent = 0;
            foreach (var socket in allSockets)
            {
                try
                {
                    await SendNotificationAsync(socket, request);
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

            return CommandResult.OK;
        }





        #region structured notifications

        public async Task<CommandResult> NotifyEventCreatedAsync(Guid creatorId, Guid eventId)
        {
            var correlationId = _correlationIdProvider.Get();
            var methodName = $"{LOGGER_NAME}{nameof(BroadcastAsync)}";
            var execTime = Stopwatch.StartNew();
            logger.Debug(correlationId, null, methodName, $"Method started", null);

            var subscribers = await _notificationsRepository.SearchSubscribersEventCreatedAsync(creatorId);

            if (subscribers?.Any() ?? false)
            {
                var newNotifications = new ConcurrentQueue<Notification>(subscribers.Select(i => new Notification
                {
                    AccountId = i,
                    EventId = eventId,
                    CreatedAt = DateTime.UtcNow,
                    Message = "",
                    Title = "Новое событие",
                    RelatedAccountId = creatorId,
                    Type = "event_created"
                }));

                var workerCount = Math.Min(10, newNotifications.Count);
                var tasks = Enumerable.Range(0, workerCount).Select(_ => Task.Run(async () =>
                {
                    while (newNotifications.TryDequeue(out var notification))
                    {
                        await HandleNewNotificationAsync(notification);
                    }
                }));

                await Task.WhenAll(tasks);
            }
            return CommandResult.OK;
        }


        #endregion




        #region private
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
                        await SendNotificationAsync(socket, new { type = "pong", timestamp = DateTimeOffset.UtcNow });
                        break;

                    default:
                        logger.Debug(correlationId, null, methodName,
                            $"Unknown message type '{type}' from accountId={accountId}", null);
                        await SendNotificationAsync(socket, new
                        {
                            type = "error",
                            message = $"Неизвестный тип сообщения: '{type}'"
                        });
                        break;
                }
            }
            catch (JsonException)
            {
                await SendNotificationAsync(socket, new
                {
                    type = "error",
                    message = "Некорректный JSON"
                });
            }
        }

        private static async Task SendNotificationAsync(WebSocket socket, object data)
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
        #endregion

        /*
        public async Task<CommandResult> NotifyUserByContactAsync(SystemNotificationType notificationType)
        {
            var correlationId = _correlationIdProvider.Get();
            var execTime = Stopwatch.StartNew();
            var methodName = $"{LOGGER_NAME}{nameof(NotifyUserByContactAsync)}";

            logger.Debug(correlationId, null, methodName, $"Method started", null);

            var contacts = await _contactsRepository.GetAccountContactsAsync(tokenData.AccountId);

            contacts = contacts?.Where(i => i.IsAuthorizationContact).ToList();

            if (!contacts.NullSafeAny())
                return CommandResult.Fail(ErrorCode.UserHasNoNecessaryContacts, "У пользователя отсутствует контакт для уведомления");

            var tokens = new Dictionary<string, string>
            {
                { "#ACTIVATION_CODE#", tokenData.ActivationKey}
            };

            var contact = contacts.FirstOrDefault();

            var notification = await _notificationsRepository.GetNotificationByTypeAsync(notificationType);

            var isEmail = MailAddress.TryCreate(contact.Value, out var eMail);
            if (isEmail)
            {
                var messageBody = _templateParser.Parse(notification.Message, tokens);
                await _smtpClient.SendMessageAsync(correlationId, new Smtp.Models.Message
                {
                    IsBodyHtml = true,
                    MessageBody = messageBody,
                    MessageSubject = "EList",
                    RecipientEmail = contact.Value
                });
                logger.Debug(correlationId, null, methodName, $"Method finished", null, execTime.Elapsed);
                return CommandResult.OK;
            }

            var isPhone = true; //Валидация на корректность введения телефона
            {
                var messageBody = _templateParser.Parse(notification.ShortMessage, tokens);
                await _smsClient.SendSmsAsync(contact.Value, messageBody);
            }

            return CommandResult.Fail(ErrorCode.UnableToNotifyUser, "Не удалось уведомить пользователя");
        }*/
    }
}
