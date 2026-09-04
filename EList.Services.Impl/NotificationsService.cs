using System.Diagnostics;
using System.Net.WebSockets;
using System.Text;
using EList.Common.CorrelationId;
using EList.Common.Logger;
using EList.Common.Models;
using EList.Common.Support;
using EList.Models.Accounts;
using EList.Models.ContentReports;
using EList.Models.Enums;
using EList.Models.Events;
using EList.Models.Notifications;
using EList.Models.Organizations;
using EList.Models.Subscriptions;
using EList.Repositories.Interfaces;
using EList.Services.Interfaces;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NLog;

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
        private readonly IEventsRepository _eventsRepository;
        private readonly IInvitationsRepository _invitationsRepository;
        private readonly IParticipationsRepository _participationsRepository;
        private readonly IAccountsRepository _accountsRepository;
        private readonly IPersonsRepository _personsRepository;
        private readonly ISubscriptionsRepository _subscriptionsRepository;
        private readonly IEventOrganizatorsRepository _eventOrganizatorsRepository;
        private readonly IEventsRatingRepository _eventsRatingRepository;
        private readonly IConversationRepository _conversationRepository;
        private readonly IOrganizationsRepository _organizationsRepository;
        private readonly IAccountPlatformRolesRepository _accountPlatformRolesRepository;
        public NotificationsService(
            WebSocketConnectionManager connectionManager,
            ICorrelationIdProvider correlationIdProvider,
            IAccountDataHolder accountDataHolder,
            INotificationsRepository notificationsRepository,
            IEventsRepository eventsRepository,
            IInvitationsRepository invitationsRepository,
            IParticipationsRepository participationsRepository,
            IAccountsRepository accountsRepository,
            IPersonsRepository personsRepository,
            ISubscriptionsRepository subscriptionsRepository,
            IEventOrganizatorsRepository eventOrganizatorsRepository,
            IEventsRatingRepository eventsRatingRepository,
            IConversationRepository conversationRepository,
            IOrganizationsRepository organizationsRepository,
            IAccountPlatformRolesRepository accountPlatformRolesRepository)
        {
            _correlationIdProvider = correlationIdProvider ?? throw new ArgumentNullException(nameof(correlationIdProvider));
            _notificationsRepository = notificationsRepository ?? throw new ArgumentNullException(nameof(notificationsRepository));
            _eventsRepository = eventsRepository ?? throw new ArgumentNullException(nameof(eventsRepository));
            _invitationsRepository = invitationsRepository ?? throw new ArgumentNullException(nameof(invitationsRepository));
            _participationsRepository = participationsRepository ?? throw new ArgumentNullException(nameof(participationsRepository));
            _accountsRepository = accountsRepository ?? throw new ArgumentNullException(nameof(accountsRepository));
            _personsRepository = personsRepository ?? throw new ArgumentException(nameof(personsRepository));
            _subscriptionsRepository = subscriptionsRepository ?? throw new ArgumentNullException(nameof(subscriptionsRepository));
            _eventOrganizatorsRepository = eventOrganizatorsRepository ?? throw new ArgumentNullException(nameof(eventOrganizatorsRepository));
            _eventsRatingRepository = eventsRatingRepository ?? throw new ArgumentNullException(nameof(eventsRatingRepository));
            _conversationRepository = conversationRepository ?? throw new ArgumentNullException(nameof(conversationRepository));
            _organizationsRepository = organizationsRepository ?? throw new ArgumentNullException(nameof(organizationsRepository));
            _accountPlatformRolesRepository = accountPlatformRolesRepository ?? throw new ArgumentNullException(nameof(accountPlatformRolesRepository));
            _connectionManager = connectionManager;
            _accountDataHolder = accountDataHolder;
        }


        #region main logic
        public async Task<CommandResult> AddConnectionAsync(Guid accountId, WebSocket socket)
        {
            var correlationId = _correlationIdProvider.Get();
            var execTime = Stopwatch.StartNew();
            var methodName = $"{LOGGER_NAME}{nameof(AddConnectionAsync)}";

            logger.Debug(correlationId, null, methodName, $"Method started", null);

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
            var accountId = _accountDataHolder.AccountId.Value;
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


        /// <summary>
        /// Обработчик уведомления (сохранение в базу и отправка пользователю)
        /// </summary>
        /// <param name="notification"></param>
        /// <returns></returns>
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

        /// <summary>
        /// Отправка уведомления конкретному пользователю
        /// </summary>
        /// <param name="accountId"></param>
        /// <param name="notification"></param>
        /// <returns></returns>
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

        /// <summary>
        /// Пометить уведомление как прочитанное
        /// </summary>
        /// <param name="notificationId"></param>
        /// <returns></returns>
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

        /// <summary>
        /// Пометить все уведомления как прочитанные
        /// </summary>
        /// <returns></returns>
        public async Task<CommandResult> ReadAllUserNotificationsAsync()
        {
            var correlationId = _correlationIdProvider.Get();
            var methodName = $"{LOGGER_NAME}{nameof(ReadAllUserNotificationsAsync)}";
            var execTime = Stopwatch.StartNew();
            logger.Debug(correlationId, null, methodName, $"Method started", null);

            var accountId = _accountDataHolder.AccountId.Value;

            await _notificationsRepository.ReadAllUserNotificationsAsync(accountId);

            logger.Debug(correlationId, null, methodName, $"Method finished", null, execTime.Elapsed);
            return CommandResult.OK;
        }

        public async Task<CommandResult<PagedList<Notification>>> GetMyNotificationsAsync(NotificationsSearchRequest? request = null)
        {
            var correlationId = _correlationIdProvider.Get();
            var methodName = $"{LOGGER_NAME}{nameof(GetMyNotificationsAsync)}";
            var execTime = Stopwatch.StartNew();
            logger.Debug(correlationId, null, methodName, "Method started", null);

            if (_accountDataHolder.AccountId == null)
                return CommandResult<PagedList<Notification>>.Fail(ErrorCode.AccessError, "Необходимо авторизоваться");

            request ??= new NotificationsSearchRequest();
            var pageIndex = request.PageIndex ?? 0;
            var pageSize = request.PageSize ?? 20;
            var unreadOnly = request.UnreadOnly == true;

            var result = await _notificationsRepository.SearchUserNotificationsAsync(
                _accountDataHolder.AccountId.Value,
                request.Type,
                unreadOnly,
                pageIndex,
                pageSize);

            logger.Debug(correlationId, null, methodName, "Method finished", null, execTime.Elapsed);
            return new CommandResult<PagedList<Notification>>(result);
        }

        public async Task<CommandResult<int>> CountMyNotificationsAsync(UserNotificationType? type = null, bool unreadOnly = true)
        {
            if (_accountDataHolder.AccountId == null)
                return CommandResult<int>.Fail(ErrorCode.AccessError, "Необходимо авторизоваться");

            var count = await _notificationsRepository.CountUserNotificationsAsync(
                _accountDataHolder.AccountId.Value,
                type,
                unreadOnly);
            return new CommandResult<int>(count);
        }

        #endregion main logic




        #region structured notifications

        #region event
        public async Task<CommandResult> NotifyEventCreatedAsync(Guid eventId, List<Guid> subscribers = null)
        {
            var correlationId = _correlationIdProvider.Get();
            var methodName = $"{LOGGER_NAME}{nameof(NotifyEventCreatedAsync)}";
            var execTime = Stopwatch.StartNew();
            logger.Debug(correlationId, null, methodName, $"Method started", null);

            subscribers ??= await _notificationsRepository.SearchSubscribersEventCreatedAsync(_accountDataHolder.AccountId.Value);
            var eventData = await _eventsRepository.GetEventAsync(eventId);

            if (subscribers?.Any() ?? false)
            {
                var notifications = subscribers.Select(subscriberId => new Notification
                {
                    Id = Guid.NewGuid(),
                    AccountId = subscriberId,
                    EventId = eventId,
                    CreatedAt = DateTime.UtcNow,
                    Message = $"{_accountDataHolder.AccountNameFullString} создал новое мероприятие {eventData.Name}",
                    Title = "Новое событие",
                    RelatedAccountId = _accountDataHolder.AccountId,
                    Type = UserNotificationType.EventCreated,
                    Data = new EventShort(eventData)
                }).ToList();

                await _notificationsRepository.CreateNotificationsAsync(notifications);

                var wsTasks = notifications.Select(n => SendToUserAsync(n.AccountId, n));
                await Task.WhenAll(wsTasks);
            }

            logger.Debug(correlationId, null, methodName, $"Method finished", null, execTime.Elapsed);
            return CommandResult.OK;
        }

        public async Task<CommandResult> NotifyEventUpdatedAsync(Guid eventId)
        {
            var correlationId = _correlationIdProvider.Get();
            var methodName = $"{LOGGER_NAME}{nameof(NotifyEventUpdatedAsync)}";
            var execTime = Stopwatch.StartNew();
            logger.Debug(correlationId, null, methodName, $"Method started", null);

            var eventData = await _eventsRepository.GetEventAsync(eventId);
            var audience = await GetEventLifecycleAudienceAsync(eventId);

            var notifications = audience.Select(accountId => BuildNotification(
                accountId,
                eventId,
                _accountDataHolder.AccountId,
                UserNotificationType.EventUpdated,
                "Событие было обновлено",
                $"В событии \"{eventData.Name}\" обновление",
                new EventShort(eventData))).ToList();

            await PersistAndSendAsync(notifications);

            logger.Debug(correlationId, null, methodName, $"Method finished", null, execTime.Elapsed);
            return CommandResult.OK;
        }

        public async Task<CommandResult> NotifyEventCancelledAsync(Guid eventId)
        {
            var correlationId = _correlationIdProvider.Get();
            var methodName = $"{LOGGER_NAME}{nameof(NotifyEventCancelledAsync)}";
            var execTime = Stopwatch.StartNew();
            logger.Debug(correlationId, null, methodName, $"Method started", null);

            var eventData = await _eventsRepository.GetEventAsync(eventId);
            var audience = await GetEventLifecycleAudienceAsync(eventId);

            var notifications = audience.Select(accountId => BuildNotification(
                accountId,
                eventId,
                _accountDataHolder.AccountId,
                UserNotificationType.EventCancelled,
                "Отмена события",
                $"Событие \"{eventData.Name}\" было отменено",
                new EventShort(eventData))).ToList();

            await PersistAndSendAsync(notifications);

            logger.Debug(correlationId, null, methodName, $"Method finished", null, execTime.Elapsed);
            return CommandResult.OK;
        }
        #endregion event


        #region invitation
        public async Task<CommandResult> NotifyUsersInvitedAsync(Guid eventId, List<Guid> subscribers)
        {
            var correlationId = _correlationIdProvider.Get();
            var methodName = $"{LOGGER_NAME}{nameof(NotifyUsersInvitedAsync)}";
            var execTime = Stopwatch.StartNew();
            logger.Debug(correlationId, null, methodName, $"Method started", null);

            var eventData = await _eventsRepository.GetEventAsync(eventId);

            if (subscribers?.Any() ?? false)
            {
                var notifications = subscribers.Select(subscriberId => new Notification
                {
                    Id = Guid.NewGuid(),
                    AccountId = subscriberId,
                    EventId = eventId,
                    CreatedAt = DateTime.UtcNow,
                    Message = $"{_accountDataHolder.AccountNameFullString} приглашает вас на \"{eventData.Name}\"",
                    Title = null,
                    RelatedAccountId = _accountDataHolder.AccountId,
                    Type = UserNotificationType.NewInvitation,
                    Data = new EventShort(eventData)
                }).ToList();

                await _notificationsRepository.CreateNotificationsAsync(notifications);

                var wsTasks = notifications.Select(n => SendToUserAsync(n.AccountId, n));
                await Task.WhenAll(wsTasks);
            }

            logger.Debug(correlationId, null, methodName, $"Method finished", null, execTime.Elapsed);
            return CommandResult.OK;
        }

        public async Task<CommandResult> NotifyInvitationAcceptedAsync(Guid eventId, Guid invitedAccountId, Guid? inviterAccountId)
        {
            var correlationId = _correlationIdProvider.Get();
            var methodName = $"{LOGGER_NAME}{nameof(NotifyInvitationAcceptedAsync)}";
            var execTime = Stopwatch.StartNew();
            logger.Debug(correlationId, null, methodName, "Method started", null);

            var eventData = await _eventsRepository.GetEventAsync(eventId);
            var recipients = await GetInvitationStakeholdersAsync(eventId, inviterAccountId, invitedAccountId);
            var actorName = _accountDataHolder.AccountNameFullString ?? "Пользователь";

            var notifications = recipients.Select(accountId => BuildNotification(
                accountId,
                eventId,
                invitedAccountId,
                UserNotificationType.InvitationAccepted,
                "Приглашение принято",
                $"{actorName} принял(а) приглашение на \"{eventData.Name}\"",
                new EventShort(eventData))).ToList();

            await PersistAndSendAsync(notifications);

            logger.Debug(correlationId, null, methodName, "Method finished", null, execTime.Elapsed);
            return CommandResult.OK;
        }

        public async Task<CommandResult> NotifyInvitationDeclinedAsync(Guid eventId, Guid invitedAccountId, Guid? inviterAccountId)
        {
            var correlationId = _correlationIdProvider.Get();
            var methodName = $"{LOGGER_NAME}{nameof(NotifyInvitationDeclinedAsync)}";
            var execTime = Stopwatch.StartNew();
            logger.Debug(correlationId, null, methodName, "Method started", null);

            var eventData = await _eventsRepository.GetEventAsync(eventId);
            var recipients = await GetInvitationStakeholdersAsync(eventId, inviterAccountId, invitedAccountId);
            var actorName = _accountDataHolder.AccountNameFullString ?? "Пользователь";

            var notifications = recipients.Select(accountId => BuildNotification(
                accountId,
                eventId,
                invitedAccountId,
                UserNotificationType.InvitationDeclined,
                "Приглашение отклонено",
                $"{actorName} отклонил(а) приглашение на \"{eventData.Name}\"",
                new EventShort(eventData))).ToList();

            await PersistAndSendAsync(notifications);

            logger.Debug(correlationId, null, methodName, "Method finished", null, execTime.Elapsed);
            return CommandResult.OK;
        }

        public async Task<CommandResult> NotifyInvitationCancelledAsync(Guid eventId, Guid invitedAccountId)
        {
            var correlationId = _correlationIdProvider.Get();
            var methodName = $"{LOGGER_NAME}{nameof(NotifyInvitationCancelledAsync)}";
            var execTime = Stopwatch.StartNew();
            logger.Debug(correlationId, null, methodName, "Method started", null);

            var eventData = await _eventsRepository.GetEventAsync(eventId);
            var notification = BuildNotification(
                invitedAccountId,
                eventId,
                _accountDataHolder.AccountId,
                UserNotificationType.InvitationCancelled,
                "Приглашение отменено",
                $"Ваше приглашение на \"{eventData.Name}\" было отменено",
                new EventShort(eventData));

            await PersistAndSendAsync(new List<Notification> { notification });

            logger.Debug(correlationId, null, methodName, "Method finished", null, execTime.Elapsed);
            return CommandResult.OK;
        }
        #endregion invitation


        #region participation
        public async Task<CommandResult> NotifyParticipatedAsync(Guid eventId)
        {
            var correlationId = _correlationIdProvider.Get();
            var methodName = $"{LOGGER_NAME}{nameof(NotifyParticipatedAsync)}";
            var execTime = Stopwatch.StartNew();
            logger.Debug(correlationId, null, methodName, $"Method started", null);

            var eventData = await _eventsRepository.GetEventAsync(eventId);
            var recipients = await GetParticipationActivityRecipientsAsync(eventId);
            var actorName = _accountDataHolder.AccountNameFullString ?? "Пользователь";

            var notifications = recipients.Select(accountId => BuildNotification(
                accountId,
                eventId,
                _accountDataHolder.AccountId,
                UserNotificationType.Participated,
                null,
                $"{actorName} принял участие в \"{eventData.Name}\"",
                new EventShort(eventData))).ToList();

            await PersistAndSendAsync(notifications);

            logger.Debug(correlationId, null, methodName, $"Method finished", null, execTime.Elapsed);
            return CommandResult.OK;
        }

        public async Task<CommandResult> NotifyEventLeftAsync(Guid eventId)
        {
            var correlationId = _correlationIdProvider.Get();
            var methodName = $"{LOGGER_NAME}{nameof(NotifyEventLeftAsync)}";
            var execTime = Stopwatch.StartNew();
            logger.Debug(correlationId, null, methodName, $"Method started", null);

            var eventData = await _eventsRepository.GetEventAsync(eventId);
            var recipients = await GetParticipationActivityRecipientsAsync(eventId);
            var actorName = _accountDataHolder.AccountNameFullString ?? "Пользователь";

            var notifications = recipients.Select(accountId => BuildNotification(
                accountId,
                eventId,
                _accountDataHolder.AccountId,
                UserNotificationType.EventLeft,
                null,
                $"{actorName} покинул событие \"{eventData.Name}\"",
                new EventShort(eventData))).ToList();

            await PersistAndSendAsync(notifications);

            logger.Debug(correlationId, null, methodName, $"Method finished", null, execTime.Elapsed);
            return CommandResult.OK;
        }

        public async Task<CommandResult> NotifyRemovedFromEventAsync(Guid eventId, List<Guid> accountIds)
        {
            var correlationId = _correlationIdProvider.Get();
            var methodName = $"{LOGGER_NAME}{nameof(NotifyRemovedFromEventAsync)}";
            var execTime = Stopwatch.StartNew();
            logger.Debug(correlationId, null, methodName, "Method started", null);

            if (accountIds == null || !accountIds.Any())
                return CommandResult.OK;

            var eventData = await _eventsRepository.GetEventAsync(eventId);
            var notifications = accountIds.Distinct().Select(accountId => BuildNotification(
                accountId,
                eventId,
                _accountDataHolder.AccountId,
                UserNotificationType.RemovedFromEvent,
                "Исключение из мероприятия",
                $"Вас исключили из участников мероприятия \"{eventData.Name}\"",
                new EventShort(eventData))).ToList();

            await PersistAndSendAsync(notifications);

            logger.Debug(correlationId, null, methodName, "Method finished", null, execTime.Elapsed);
            return CommandResult.OK;
        }
        #endregion participation


        #region subscription
        public async Task<CommandResult> NotifySubscribedAsync(Guid subscribedToId)
        {
            var correlationId = _correlationIdProvider.Get();
            var methodName = $"{LOGGER_NAME}{nameof(NotifySubscribedAsync)}";
            var execTime = Stopwatch.StartNew();
            logger.Debug(correlationId, null, methodName, $"Method started", null);

            var subscribedTo = await _accountsRepository.GetAccountAsync(subscribedToId);
            var subscribedToPerson = await _personsRepository.GetPersonInfoAsync(subscribedToId);
            var subscribedToAccountFullString = !string.IsNullOrWhiteSpace(subscribedToPerson?.FIO)
                ? $"{subscribedToPerson.FIO} ({subscribedTo?.Login})"
                : $"{subscribedTo?.Login}";

            #region уведомляем всех, кроме того, на кого подписались
            var subscribers = await _subscriptionsRepository.GetSubscribersIdsAsync(new SubscriptionsSearchRequest
            {
                AccountId = _accountDataHolder.AccountId.Value,
                NotifySubscribed = true
            });
            subscribers = subscribers?.Where(i => i != subscribedToId)?.ToList();

            if (subscribers?.Any() ?? false)
            {
                var notifications = subscribers.Select(subscriberId => new Notification
                {
                    Id = Guid.NewGuid(),
                    AccountId = subscriberId,
                    EventId = null,
                    CreatedAt = DateTime.UtcNow,
                    Message = $"{_accountDataHolder.AccountNameFullString} подписался на {subscribedToAccountFullString}",
                    Title = null,
                    RelatedAccountId = subscribedToId,
                    Type = UserNotificationType.RelatedPersonSubscribed,
                    Data = subscribedTo
                }).ToList();

                await _notificationsRepository.CreateNotificationsAsync(notifications);

                var wsTasks = notifications.Select(n => SendToUserAsync(n.AccountId, n));
                await Task.WhenAll(wsTasks);
            }
            #endregion

            #region уведомляем того, на кого подписались
            var notification = new Notification
            {
                Id = Guid.NewGuid(),
                AccountId = subscribedToId,
                EventId = null,
                CreatedAt = DateTime.UtcNow,
                Message = $"На вас подписался {_accountDataHolder.AccountNameFullString}",
                Title = $"У вас новый подписчик",
                RelatedAccountId = _accountDataHolder.AccountId,
                Type = UserNotificationType.NewSubscription,
                Data = new AccountPublicData(_accountDataHolder.Account),
            };

            await _notificationsRepository.CreateNotificationAsync(notification);
            await SendToUserAsync(subscribedToId, notification);
            #endregion

            logger.Debug(correlationId, null, methodName, $"Method finished", null, execTime.Elapsed);
            return CommandResult.OK;
        }

        public async Task<CommandResult> NotifyUnsubscribedAsync(Guid unsubscribedFromId)
        {
            var correlationId = _correlationIdProvider.Get();
            var methodName = $"{LOGGER_NAME}{nameof(NotifyUnsubscribedAsync)}";
            var execTime = Stopwatch.StartNew();
            logger.Debug(correlationId, null, methodName, $"Method started", null);

            var unsubscribedFrom = await _accountsRepository.GetAccountAsync(unsubscribedFromId);
            var personUnsubscribedFrom = await _personsRepository.GetPersonInfoAsync(unsubscribedFromId);
            var unsubscribedFromAccountFullString = !string.IsNullOrWhiteSpace(personUnsubscribedFrom?.FIO)
                ? $"{personUnsubscribedFrom.FIO} ({unsubscribedFrom?.Login})"
                : $"{unsubscribedFrom?.Login}";

            #region уведомляем всех, кроме того, от кого отписались
            var subscribers = await _subscriptionsRepository.GetSubscribersIdsAsync(new SubscriptionsSearchRequest
            {
                AccountId = _accountDataHolder.AccountId.Value,
                NotifySubscribed = true
            });
            subscribers = subscribers?.Where(i => i != unsubscribedFromId)?.ToList();

            if (subscribers?.Any() ?? false)
            {
                var notifications = subscribers.Select(subscriberId => new Notification
                {
                    Id = Guid.NewGuid(),
                    AccountId = subscriberId,
                    EventId = null,
                    CreatedAt = DateTime.UtcNow,
                    Message = $"{_accountDataHolder.AccountNameFullString} отписался от {unsubscribedFromAccountFullString}",
                    Title = null,
                    RelatedAccountId = unsubscribedFromId,
                    Type = UserNotificationType.RelatedPersonUnsubscribed,
                    Data = unsubscribedFrom
                }).ToList();

                await _notificationsRepository.CreateNotificationsAsync(notifications);

                var wsTasks = notifications.Select(n => SendToUserAsync(n.AccountId, n));
                await Task.WhenAll(wsTasks);
            }
            #endregion

            #region уведомляем того, от кого отписались
            var notification = new Notification
            {
                Id = Guid.NewGuid(),
                AccountId = unsubscribedFromId,
                EventId = null,
                CreatedAt = DateTime.UtcNow,
                Message = $"{_accountDataHolder.AccountNameFullString} отписался от вас",
                Title = $"От вас отписались",
                RelatedAccountId = _accountDataHolder.AccountId,
                Type = UserNotificationType.Unsubscribed,
                Data = new AccountPublicData(_accountDataHolder.Account),
            };

            await _notificationsRepository.CreateNotificationAsync(notification);
            await SendToUserAsync(unsubscribedFromId, notification);
            #endregion

            logger.Debug(correlationId, null, methodName, $"Method finished", null, execTime.Elapsed);
            return CommandResult.OK;
        }
        #endregion subscription

        #region BWlist
        public async Task<CommandResult> NotifyAddedToBlackListAsync(Guid eventId, List<Guid> blackList)
        {
            var correlationId = _correlationIdProvider.Get();
            var methodName = $"{LOGGER_NAME}{nameof(NotifyAddedToBlackListAsync)}";
            var execTime = Stopwatch.StartNew();
            logger.Debug(correlationId, null, methodName, $"Method started", null);

            var eventData = await _eventsRepository.GetEventAsync(eventId);

            if (blackList?.Any() ?? false)
            {
                var notifications = blackList.Distinct().Select(subscriberId => BuildNotification(
                    subscriberId,
                    eventId,
                    _accountDataHolder.AccountId,
                    UserNotificationType.AddedToBlackList,
                    null,
                    $"Вас добавили в чёрный список мероприятия \"{eventData.Name}\"",
                    new EventShort(eventData))).ToList();

                await PersistAndSendAsync(notifications);
            }

            logger.Debug(correlationId, null, methodName, $"Method finished", null, execTime.Elapsed);
            return CommandResult.OK;
        }

        public async Task<CommandResult> NotifyAddedToWhiteListAsync(Guid eventId, List<Guid> whiteList)
        {
            var correlationId = _correlationIdProvider.Get();
            var methodName = $"{LOGGER_NAME}{nameof(NotifyAddedToWhiteListAsync)}";
            var execTime = Stopwatch.StartNew();
            logger.Debug(correlationId, null, methodName, "Method started", null);

            if (whiteList == null || !whiteList.Any())
                return CommandResult.OK;

            var eventData = await _eventsRepository.GetEventAsync(eventId);
            var notifications = whiteList.Distinct().Select(accountId => BuildNotification(
                accountId,
                eventId,
                _accountDataHolder.AccountId,
                UserNotificationType.AddedToWhiteList,
                null,
                $"Вас добавили в белый список мероприятия \"{eventData.Name}\"",
                new EventShort(eventData))).ToList();

            await PersistAndSendAsync(notifications);

            logger.Debug(correlationId, null, methodName, "Method finished", null, execTime.Elapsed);
            return CommandResult.OK;
        }

        public async Task<CommandResult> NotifyRemovedFromBlackListAsync(Guid eventId, List<Guid> accountIds)
        {
            var correlationId = _correlationIdProvider.Get();
            var methodName = $"{LOGGER_NAME}{nameof(NotifyRemovedFromBlackListAsync)}";
            var execTime = Stopwatch.StartNew();
            logger.Debug(correlationId, null, methodName, "Method started", null);

            if (accountIds == null || !accountIds.Any())
                return CommandResult.OK;

            var eventData = await _eventsRepository.GetEventAsync(eventId);
            var notifications = accountIds.Distinct().Select(accountId => BuildNotification(
                accountId,
                eventId,
                _accountDataHolder.AccountId,
                UserNotificationType.RemovedFromBlackList,
                null,
                $"Вас удалили из чёрного списка мероприятия \"{eventData.Name}\"",
                new EventShort(eventData))).ToList();

            await PersistAndSendAsync(notifications);

            logger.Debug(correlationId, null, methodName, "Method finished", null, execTime.Elapsed);
            return CommandResult.OK;
        }

        public async Task<CommandResult> NotifyRemovedFromWhiteListAsync(Guid eventId, List<Guid> accountIds)
        {
            var correlationId = _correlationIdProvider.Get();
            var methodName = $"{LOGGER_NAME}{nameof(NotifyRemovedFromWhiteListAsync)}";
            var execTime = Stopwatch.StartNew();
            logger.Debug(correlationId, null, methodName, "Method started", null);

            if (accountIds == null || !accountIds.Any())
                return CommandResult.OK;

            var eventData = await _eventsRepository.GetEventAsync(eventId);
            var notifications = accountIds.Distinct().Select(accountId => BuildNotification(
                accountId,
                eventId,
                _accountDataHolder.AccountId,
                UserNotificationType.RemovedFromWhiteList,
                null,
                $"Вас удалили из белого списка мероприятия \"{eventData.Name}\"",
                new EventShort(eventData))).ToList();

            await PersistAndSendAsync(notifications);

            logger.Debug(correlationId, null, methodName, "Method finished", null, execTime.Elapsed);
            return CommandResult.OK;
        }

        public async Task<CommandResult> NotifyNotInWhiteListAsync(Guid eventId, List<Guid> notInWhiteList)
        {
            var correlationId = _correlationIdProvider.Get();
            var methodName = $"{LOGGER_NAME}{nameof(NotifyNotInWhiteListAsync)}";
            var execTime = Stopwatch.StartNew();
            logger.Debug(correlationId, null, methodName, $"Method started", null);

            var eventData = await _eventsRepository.GetEventAsync(eventId);

            if (notInWhiteList?.Any() ?? false)
            {
                var notifications = notInWhiteList.Distinct().Select(subscriberId => BuildNotification(
                    subscriberId,
                    eventId,
                    _accountDataHolder.AccountId,
                    UserNotificationType.NotInWhiteList,
                    null,
                    $"Вы не попали в белый список закрытого мероприятия \"{eventData.Name}\"",
                    new EventShort(eventData))).ToList();

                await PersistAndSendAsync(notifications);
            }

            logger.Debug(correlationId, null, methodName, $"Method finished", null, execTime.Elapsed);
            return CommandResult.OK;
        }
        #endregion BWlist

        #region event rating
        public async Task<CommandResult> NotifyNewEventRatingAsync(Guid eventId, Guid ratingItem, List<Guid> organizators = null)
        {
            var correlationId = _correlationIdProvider.Get();
            var methodName = $"{LOGGER_NAME}{nameof(NotifyNewEventRatingAsync)}";
            var execTime = Stopwatch.StartNew();
            logger.Debug(correlationId, null, methodName, $"Method started", null);

            var eventData = await _eventsRepository.GetEventAsync(eventId);
            organizators ??= (await _eventOrganizatorsRepository.GetOrganizatorIdsByEventIdAsync(eventId))?.ToList();
            var rating = await _eventsRatingRepository.GetRatingItemAsync(ratingItem);

            if (organizators?.Any() ?? false)
            {
                var notifications = organizators.Where(i => i != _accountDataHolder.AccountId).Select(organizatorId => new Notification
                {
                    Id = Guid.NewGuid(),
                    AccountId = organizatorId,
                    EventId = eventId,
                    CreatedAt = DateTime.UtcNow,
                    Message = $"{_accountDataHolder.AccountNameFullString} оценил мероприятие \"{eventData.Name}\"",
                    Title = "Новая оценка у мероприятия",
                    RelatedAccountId = _accountDataHolder.AccountId,
                    Type = UserNotificationType.NewEventRating,
                    Data = rating
                }).ToList();

                await _notificationsRepository.CreateNotificationsAsync(notifications);

                var wsTasks = notifications.Select(n => SendToUserAsync(n.AccountId, n));
                await Task.WhenAll(wsTasks);
            }

            logger.Debug(correlationId, null, methodName, $"Method finished", null, execTime.Elapsed);
            return CommandResult.OK;
        }

        public async Task<CommandResult> NotifyEventRatingChangedAsync(Guid eventId, Guid ratingItem, List<Guid> organizators = null)
        {
            var correlationId = _correlationIdProvider.Get();
            var methodName = $"{LOGGER_NAME}{nameof(NotifyEventRatingChangedAsync)}";
            var execTime = Stopwatch.StartNew();
            logger.Debug(correlationId, null, methodName, $"Method started", null);

            var eventData = await _eventsRepository.GetEventAsync(eventId);
            organizators ??= (await _eventOrganizatorsRepository.GetOrganizatorIdsByEventIdAsync(eventId))?.ToList();
            var rating = await _eventsRatingRepository.GetRatingItemAsync(ratingItem);

            if (organizators?.Any() ?? false)
            {
                var notifications = organizators.Where(i => i != _accountDataHolder.AccountId).Select(organizatorId => new Notification
                {
                    Id = Guid.NewGuid(),
                    AccountId = organizatorId,
                    EventId = eventId,
                    CreatedAt = DateTime.UtcNow,
                    Message = $"{_accountDataHolder.AccountNameFullString} изменил свою оценку мероприятия \"{eventData.Name}\"",
                    Title = "Оценка мероприятия изменилась",
                    RelatedAccountId = _accountDataHolder.AccountId,
                    Type = UserNotificationType.EventRatingChanged,
                    Data = rating
                }).ToList();

                await _notificationsRepository.CreateNotificationsAsync(notifications);

                var wsTasks = notifications.Select(n => SendToUserAsync(n.AccountId, n));
                await Task.WhenAll(wsTasks);
            }

            logger.Debug(correlationId, null, methodName, $"Method finished", null, execTime.Elapsed);
            return CommandResult.OK;
        }

        public async Task<CommandResult> NotifyEventRatingDeletedAsync(Guid eventId, List<Guid> organizators = null)
        {
            var correlationId = _correlationIdProvider.Get();
            var methodName = $"{LOGGER_NAME}{nameof(NotifyEventRatingDeletedAsync)}";
            var execTime = Stopwatch.StartNew();
            logger.Debug(correlationId, null, methodName, $"Method started", null);

            var eventData = await _eventsRepository.GetEventAsync(eventId);
            organizators ??= (await _eventOrganizatorsRepository.GetOrganizatorIdsByEventIdAsync(eventId))?.ToList();

            if (organizators?.Any() ?? false)
            {
                var notifications = organizators.Where(i => i != _accountDataHolder.AccountId).Select(organizatorId => new Notification
                {
                    Id = Guid.NewGuid(),
                    AccountId = organizatorId,
                    EventId = eventId,
                    CreatedAt = DateTime.UtcNow,
                    Message = $"{_accountDataHolder.AccountNameFullString} удалил свою оценку мероприятия \"{eventData.Name}\"",
                    Title = "Оценка мероприятия изменилась",
                    RelatedAccountId = _accountDataHolder.AccountId,
                    Type = UserNotificationType.EventRatingDeleted
                }).ToList();

                await _notificationsRepository.CreateNotificationsAsync(notifications);

                var wsTasks = notifications.Select(n => SendToUserAsync(n.AccountId, n));
                await Task.WhenAll(wsTasks);
            }

            logger.Debug(correlationId, null, methodName, $"Method finished", null, execTime.Elapsed);
            return CommandResult.OK;
        }
        #endregion

        #region message
        public async Task<CommandResult> NotifyCommentRepliedAsync(Guid? eventId, Guid messageId, Guid replyId)
        {
            var correlationId = _correlationIdProvider.Get();
            var methodName = $"{LOGGER_NAME}{nameof(NotifyCommentRepliedAsync)}";
            var execTime = Stopwatch.StartNew();
            logger.Debug(correlationId, null, methodName, $"Method started", null);

            var message = await _conversationRepository.GetMessageAsync(messageId);
            if (message?.AccountId == null || message.AccountId == _accountDataHolder.AccountId)
            {
                logger.Debug(correlationId, null, methodName, $"Method finished", null, execTime.Elapsed);
                return CommandResult.OK;
            }

            var reply = await _conversationRepository.GetMessageAsync(replyId);
            var preview = BuildMessagePreview(reply?.MessageText);

            var notification = BuildNotification(
                message.AccountId.Value,
                eventId,
                _accountDataHolder.AccountId,
                UserNotificationType.MessageReplied,
                $"{_accountDataHolder.AccountNameFullString} ответил на ваше сообщение",
                preview,
                reply);

            await PersistAndSendAsync(new List<Notification> { notification });

            logger.Debug(correlationId, null, methodName, $"Method finished", null, execTime.Elapsed);
            return CommandResult.OK;
        }

        /// <summary>Обратная совместимость со старым именем метода.</summary>
        public Task<CommandResult> NotifyCommentRepliedsync(Guid? eventId, Guid messageId, Guid replyId)
            => NotifyCommentRepliedAsync(eventId, messageId, replyId);

        public async Task<CommandResult> NotifyNewMessageAsync(Guid conversationId, Guid messageId, Guid? eventId = null)
        {
            var correlationId = _correlationIdProvider.Get();
            var methodName = $"{LOGGER_NAME}{nameof(NotifyNewMessageAsync)}";
            var execTime = Stopwatch.StartNew();
            logger.Debug(correlationId, null, methodName, "Method started", null);

            var message = await _conversationRepository.GetMessageAsync(messageId);
            if (message == null)
                return CommandResult.OK;

            var recipients = await GetNewMessageRecipientsAsync(conversationId, eventId, excludeAccountId: _accountDataHolder.AccountId);
            if (message.ReplyTo != null)
            {
                var parent = await _conversationRepository.GetMessageAsync(message.ReplyTo.Value);
                if (parent?.AccountId != null)
                    recipients.Remove(parent.AccountId.Value);
            }

            if (recipients.Count == 0)
            {
                logger.Debug(correlationId, null, methodName, "Method finished", null, execTime.Elapsed);
                return CommandResult.OK;
            }

            var preview = BuildMessagePreview(message.MessageText);
            var actorName = _accountDataHolder.AccountNameFullString ?? "Пользователь";
            var notifications = recipients.Select(accountId => BuildNotification(
                accountId,
                eventId,
                _accountDataHolder.AccountId,
                UserNotificationType.NewMessage,
                $"{actorName} написал(а) сообщение",
                preview,
                message)).ToList();

            await PersistAndSendAsync(notifications);

            logger.Debug(correlationId, null, methodName, "Method finished", null, execTime.Elapsed);
            return CommandResult.OK;
        }
        #endregion

        #region content reports
        public async Task<CommandResult> NotifyContentReportCreatedAsync(ContentReport report)
        {
            var correlationId = _correlationIdProvider.Get();
            var methodName = $"{LOGGER_NAME}{nameof(NotifyContentReportCreatedAsync)}";
            var execTime = Stopwatch.StartNew();
            logger.Debug(correlationId, null, methodName, "Method started", null);

            var actorId = _accountDataHolder.AccountId;
            var exclude = new HashSet<Guid>();
            if (actorId != null)
                exclude.Add(actorId.Value);

            var data = BuildContentReportData(report, queue: null);
            var notifications = new List<Notification>();

            var subjectIds = await GetReportSubjectAccountIdsAsync(report, includeEventOrganizers: false);
            foreach (var accountId in subjectIds.Where(id => !exclude.Contains(id)))
            {
                notifications.Add(BuildNotification(
                    accountId,
                    report.EventId,
                    relatedAccountId: null,
                    UserNotificationType.ContentReportFiledAgainstYou,
                    "Жалоба на ваш контент",
                    BuildFiledAgainstYouMessage(report),
                    data));
                exclude.Add(accountId);
            }

            if (report.OrganizerStatus != null && report.EventId != null)
            {
                var organizers = await _eventOrganizatorsRepository.GetAllOrganizerAccountIdsAsync(report.EventId.Value);
                var organizerData = BuildContentReportData(report, queue: "organizers");
                foreach (var accountId in organizers.Where(id => !exclude.Contains(id)))
                {
                    notifications.Add(BuildNotification(
                        accountId,
                        report.EventId,
                        actorId,
                        UserNotificationType.ContentReportNewInOrganizerQueue,
                        "Новая жалоба по мероприятию",
                        "Поступила жалоба на контент вашего мероприятия. Её нужно рассмотреть.",
                        organizerData));
                }
            }

            if (report.PlatformStatus != null)
            {
                var staff = await GetPlatformStaffAccountIdsAsync();
                var staffData = BuildContentReportData(report, queue: "platform");
                foreach (var accountId in staff.Where(id => !exclude.Contains(id)))
                {
                    notifications.Add(BuildNotification(
                        accountId,
                        report.EventId,
                        actorId,
                        UserNotificationType.ContentReportNewInPlatformQueue,
                        "Новая жалоба на площадке",
                        BuildPlatformQueueMessage(report),
                        staffData));
                }
            }

            await PersistAndSendAsync(notifications);

            logger.Debug(correlationId, null, methodName, "Method finished", null, execTime.Elapsed);
            return CommandResult.OK;
        }

        public async Task<CommandResult> NotifyContentReportResolvedAsync(
            ContentReport report,
            ReportResolutionAction action,
            string? comment)
        {
            var correlationId = _correlationIdProvider.Get();
            var methodName = $"{LOGGER_NAME}{nameof(NotifyContentReportResolvedAsync)}";
            var execTime = Stopwatch.StartNew();
            logger.Debug(correlationId, null, methodName, "Method started", null);

            var actorId = _accountDataHolder.AccountId;
            var exclude = new HashSet<Guid>();
            if (actorId != null)
                exclude.Add(actorId.Value);

            var data = BuildContentReportData(report, queue: null, action);
            var notifications = new List<Notification>();

            if (report.ReporterAccountId != default && !exclude.Contains(report.ReporterAccountId))
            {
                var dismissed = action == ReportResolutionAction.Dismiss;
                notifications.Add(BuildNotification(
                    report.ReporterAccountId,
                    report.EventId,
                    actorId,
                    UserNotificationType.ContentReportReviewed,
                    dismissed ? "Жалоба отклонена" : "Жалоба рассмотрена",
                    dismissed
                        ? "Ваша жалоба рассмотрена: нарушений не найдено."
                        : "Ваша жалоба рассмотрена, по ней приняты меры.",
                    data));
                exclude.Add(report.ReporterAccountId);
            }

            var includeEventOrganizers = action == ReportResolutionAction.Warn
                && report.TargetType == ReportTargetType.Event;
            var subjectIds = (await GetReportSubjectAccountIdsAsync(report, includeEventOrganizers))
                .Where(id => !exclude.Contains(id))
                .Distinct()
                .ToList();

            switch (action)
            {
                case ReportResolutionAction.Warn:
                    foreach (var accountId in subjectIds)
                    {
                        notifications.Add(BuildNotification(
                            accountId,
                            report.EventId,
                            actorId,
                            UserNotificationType.ContentReportWarningIssued,
                            "Предупреждение модерации",
                            string.IsNullOrWhiteSpace(comment)
                                ? "Модератор вынес предупреждение по вашей публикации или профилю."
                                : comment.Trim(),
                            data));
                    }
                    break;

                case ReportResolutionAction.HideContent:
                case ReportResolutionAction.DeleteContent:
                    foreach (var accountId in subjectIds)
                    {
                        notifications.Add(BuildNotification(
                            accountId,
                            report.EventId,
                            actorId,
                            UserNotificationType.ContentReportContentModerated,
                            action == ReportResolutionAction.DeleteContent
                                ? "Контент удалён"
                                : "Контент скрыт",
                            action == ReportResolutionAction.DeleteContent
                                ? "Ваш контент удалён по итогам рассмотрения жалобы."
                                : "Ваш контент скрыт по итогам рассмотрения жалобы.",
                            data));
                    }
                    break;

                case ReportResolutionAction.SuspendAccount:
                    foreach (var accountId in subjectIds)
                    {
                        notifications.Add(BuildNotification(
                            accountId,
                            report.EventId,
                            actorId,
                            UserNotificationType.ContentReportAccountSuspended,
                            "Аккаунт приостановлен",
                            "Ваш аккаунт приостановлен модерацией площадки.",
                            data));
                    }
                    break;

                case ReportResolutionAction.SuspendOrganization:
                    foreach (var accountId in subjectIds)
                    {
                        notifications.Add(BuildNotification(
                            accountId,
                            report.EventId,
                            actorId,
                            UserNotificationType.ContentReportOrganizationSuspended,
                            "Организация приостановлена",
                            "Организация приостановлена модерацией площадки.",
                            data));
                    }
                    break;

                case ReportResolutionAction.RemoveOrganizator:
                    foreach (var accountId in subjectIds)
                    {
                        notifications.Add(BuildNotification(
                            accountId,
                            report.EventId,
                            actorId,
                            UserNotificationType.ContentReportOrganizatorRemoved,
                            "Сняты с организаторов",
                            "Вы сняты с организаторов мероприятия по итогам модерации.",
                            data));
                    }
                    break;

                case ReportResolutionAction.ResetAvatar:
                    foreach (var accountId in subjectIds)
                    {
                        notifications.Add(BuildNotification(
                            accountId,
                            report.EventId,
                            actorId,
                            UserNotificationType.ContentReportAvatarReset,
                            "Аватарка сброшена",
                            "Аватарка или обложка сброшена модерацией.",
                            data));
                    }
                    break;
            }

            await PersistAndSendAsync(notifications);

            logger.Debug(correlationId, null, methodName, "Method finished", null, execTime.Elapsed);
            return CommandResult.OK;
        }

        public async Task<CommandResult> NotifyContentReportEscalatedAsync(ContentReport report)
        {
            var correlationId = _correlationIdProvider.Get();
            var methodName = $"{LOGGER_NAME}{nameof(NotifyContentReportEscalatedAsync)}";
            var execTime = Stopwatch.StartNew();
            logger.Debug(correlationId, null, methodName, "Method started", null);

            var actorId = _accountDataHolder.AccountId;
            var exclude = new HashSet<Guid>();
            if (actorId != null)
                exclude.Add(actorId.Value);

            var staff = await GetPlatformStaffAccountIdsAsync();
            var data = BuildContentReportData(report, queue: "platform");
            var notifications = staff
                .Where(id => !exclude.Contains(id))
                .Select(accountId => BuildNotification(
                    accountId,
                    report.EventId,
                    actorId,
                    UserNotificationType.ContentReportNewInPlatformQueue,
                    "Жалоба эскалирована на площадку",
                    "Организатор передал жалобу на рассмотрение площадке.",
                    data))
                .ToList();

            await PersistAndSendAsync(notifications);

            logger.Debug(correlationId, null, methodName, "Method finished", null, execTime.Elapsed);
            return CommandResult.OK;
        }

        public async Task<CommandResult> NotifyContentReportPenaltyIssuedAsync(ModerationPenalty penalty)
        {
            var correlationId = _correlationIdProvider.Get();
            var methodName = $"{LOGGER_NAME}{nameof(NotifyContentReportPenaltyIssuedAsync)}";
            var execTime = Stopwatch.StartNew();
            logger.Debug(correlationId, null, methodName, "Method started", null);

            var accountIds = new List<Guid>();
            if (penalty.AccountId != null)
                accountIds.Add(penalty.AccountId.Value);

            if (penalty.OrganizationId != null)
            {
                var members = await _organizationsRepository.GetMembersByOrganizationIdAsync(penalty.OrganizationId.Value);
                accountIds.AddRange(members.Where(m => m.AccountId != Guid.Empty).Select(m => m.AccountId));
            }

            var actorId = _accountDataHolder.AccountId;
            var message = ModerationPenaltiesService.FormatRestrictionMessage(penalty);
            var data = new ContentReportNotificationData
            {
                ReportId = penalty.ReportId ?? Guid.Empty,
                EventId = penalty.EventId,
                OrganizationId = penalty.OrganizationId,
                PenaltyType = penalty.PenaltyType,
                PenaltyEndsAt = penalty.EndsAt,
                ResolutionAction = ReportResolutionAction.ApplyPenalty
            };

            var notifications = accountIds
                .Where(id => id != actorId)
                .Distinct()
                .Select(accountId => BuildNotification(
                    accountId,
                    penalty.EventId,
                    actorId,
                    UserNotificationType.ContentReportPenaltyIssued,
                    "Ограничение от модерации",
                    message,
                    data))
                .ToList();

            await PersistAndSendAsync(notifications);

            logger.Debug(correlationId, null, methodName, "Method finished", null, execTime.Elapsed);
            return CommandResult.OK;
        }

        public async Task<CommandResult> NotifyEventRestoredAsync(Guid eventId)
        {
            var correlationId = _correlationIdProvider.Get();
            var methodName = $"{LOGGER_NAME}{nameof(NotifyEventRestoredAsync)}";
            var execTime = Stopwatch.StartNew();
            logger.Debug(correlationId, null, methodName, "Method started", null);

            var eventData = await _eventsRepository.GetEventAsync(eventId);
            var audience = await GetEventLifecycleAudienceAsync(eventId);

            var notifications = audience.Select(accountId => BuildNotification(
                accountId,
                eventId,
                _accountDataHolder.AccountId,
                UserNotificationType.EventRestored,
                "Мероприятие восстановлено",
                $"Мероприятие \"{eventData.Name}\" снова активно",
                new EventShort(eventData))).ToList();

            await PersistAndSendAsync(notifications);

            logger.Debug(correlationId, null, methodName, "Method finished", null, execTime.Elapsed);
            return CommandResult.OK;
        }

        public async Task<CommandResult> NotifyOrganizationMemberAddedAsync(Guid organizationId, Guid accountId)
        {
            return await NotifyOrganizationMemberChangeAsync(
                organizationId,
                accountId,
                UserNotificationType.OrganizationMemberAdded,
                "Добавление в организацию",
                orgName => $"Вас добавили в организацию \"{orgName}\"");
        }

        public async Task<CommandResult> NotifyOrganizationMemberRemovedAsync(Guid organizationId, Guid accountId)
        {
            return await NotifyOrganizationMemberChangeAsync(
                organizationId,
                accountId,
                UserNotificationType.OrganizationMemberRemoved,
                "Удаление из организации",
                orgName => $"Вас удалили из организации \"{orgName}\"");
        }

        public async Task<CommandResult> NotifyOrganizationMemberDeactivatedAsync(Guid organizationId, Guid accountId)
        {
            return await NotifyOrganizationMemberChangeAsync(
                organizationId,
                accountId,
                UserNotificationType.OrganizationMemberDeactivated,
                "Деактивация в организации",
                orgName => $"Ваше участие в организации \"{orgName}\" деактивировано");
        }

        public async Task<CommandResult> NotifyOrganizationOwnershipTransferredAsync(
            Guid organizationId,
            Guid newOwnerAccountId,
            Guid previousOwnerAccountId)
        {
            var correlationId = _correlationIdProvider.Get();
            var methodName = $"{LOGGER_NAME}{nameof(NotifyOrganizationOwnershipTransferredAsync)}";
            var execTime = Stopwatch.StartNew();
            logger.Debug(correlationId, null, methodName, "Method started", null);

            var organization = await _organizationsRepository.GetOrganizationAsync(organizationId);
            var notifications = new List<Notification>
            {
                BuildNotification(
                    newOwnerAccountId,
                    null,
                    previousOwnerAccountId,
                    UserNotificationType.OrganizationOwnershipTransferred,
                    "Владение организацией",
                    $"Вам передано владение организацией \"{organization?.Name}\"",
                    organization),
                BuildNotification(
                    previousOwnerAccountId,
                    null,
                    newOwnerAccountId,
                    UserNotificationType.OrganizationOwnershipTransferred,
                    "Владение организацией",
                    $"Вы передали владение организацией \"{organization?.Name}\"",
                    organization)
            };

            await PersistAndSendAsync(notifications);

            logger.Debug(correlationId, null, methodName, "Method finished", null, execTime.Elapsed);
            return CommandResult.OK;
        }

        public async Task<CommandResult> NotifyOrganizationVerificationApprovedAsync(Guid organizationId)
        {
            return await NotifyOrganizationVerificationAsync(
                organizationId,
                approved: true,
                reason: null);
        }

        public async Task<CommandResult> NotifyOrganizationVerificationRejectedAsync(Guid organizationId, string? reason)
        {
            return await NotifyOrganizationVerificationAsync(
                organizationId,
                approved: false,
                reason);
        }

        public async Task<CommandResult> NotifyEventOrganizatorAssignedAsync(Guid eventId, List<Guid> accountIds)
        {
            var correlationId = _correlationIdProvider.Get();
            var methodName = $"{LOGGER_NAME}{nameof(NotifyEventOrganizatorAssignedAsync)}";
            var execTime = Stopwatch.StartNew();
            logger.Debug(correlationId, null, methodName, "Method started", null);

            if (accountIds == null || !accountIds.Any())
                return CommandResult.OK;

            var eventData = await _eventsRepository.GetEventAsync(eventId);
            var notifications = accountIds.Distinct()
                .Where(id => id != _accountDataHolder.AccountId)
                .Select(accountId => BuildNotification(
                    accountId,
                    eventId,
                    _accountDataHolder.AccountId,
                    UserNotificationType.EventOrganizatorAssigned,
                    "Назначение организатором",
                    $"Вас назначили организатором мероприятия \"{eventData.Name}\"",
                    new EventShort(eventData)))
                .ToList();

            await PersistAndSendAsync(notifications);

            logger.Debug(correlationId, null, methodName, "Method finished", null, execTime.Elapsed);
            return CommandResult.OK;
        }

        public async Task<CommandResult> NotifyEventOrganizatorRemovedAsync(Guid eventId, Guid accountId)
        {
            var correlationId = _correlationIdProvider.Get();
            var methodName = $"{LOGGER_NAME}{nameof(NotifyEventOrganizatorRemovedAsync)}";
            var execTime = Stopwatch.StartNew();
            logger.Debug(correlationId, null, methodName, "Method started", null);

            if (accountId == _accountDataHolder.AccountId)
                return CommandResult.OK;

            var eventData = await _eventsRepository.GetEventAsync(eventId);
            var notification = BuildNotification(
                accountId,
                eventId,
                _accountDataHolder.AccountId,
                UserNotificationType.EventOrganizatorRemoved,
                "Снятие с организаторов",
                $"Вас сняли с организаторов мероприятия \"{eventData.Name}\"",
                new EventShort(eventData));

            await PersistAndSendAsync(new List<Notification> { notification });

            logger.Debug(correlationId, null, methodName, "Method finished", null, execTime.Elapsed);
            return CommandResult.OK;
        }

        public async Task<CommandResult> NotifyAgreementUpdateRequiredAsync(
            DocumentType documentType,
            string version,
            List<Guid> accountIds)
        {
            var correlationId = _correlationIdProvider.Get();
            var methodName = $"{LOGGER_NAME}{nameof(NotifyAgreementUpdateRequiredAsync)}";
            var execTime = Stopwatch.StartNew();
            logger.Debug(correlationId, null, methodName, "Method started", null);

            if (accountIds == null || !accountIds.Any())
                return CommandResult.OK;

            var documentLabel = documentType switch
            {
                DocumentType.Policy => "Политика обработки ПДн",
                DocumentType.Consent => "Согласие на обработку ПДн",
                DocumentType.Agreement => "Пользовательское соглашение",
                DocumentType.OrganizationAgreement => "Соглашение организации",
                DocumentType.TicketingAgreement => "Соглашение о билетах",
                _ => documentType.ToString()
            };

            var notifications = accountIds.Distinct().Select(accountId => BuildNotification(
                accountId,
                null,
                _accountDataHolder.AccountId,
                UserNotificationType.AgreementUpdateRequired,
                "Обновлены юридические документы",
                $"Появилась новая версия документа «{documentLabel}» (v{version}). Необходимо принять её, чтобы продолжить работу.",
                new { DocumentType = documentType, Version = version })).ToList();

            await PersistAndSendAsync(notifications);

            logger.Debug(correlationId, null, methodName, "Method finished", null, execTime.Elapsed);
            return CommandResult.OK;
        }

        private async Task<CommandResult> NotifyOrganizationMemberChangeAsync(
            Guid organizationId,
            Guid accountId,
            UserNotificationType type,
            string title,
            Func<string, string> messageFactory)
        {
            var correlationId = _correlationIdProvider.Get();
            var methodName = $"{LOGGER_NAME}NotifyOrganizationMemberChangeAsync";
            var execTime = Stopwatch.StartNew();
            logger.Debug(correlationId, null, methodName, "Method started", null);

            var organization = await _organizationsRepository.GetOrganizationAsync(organizationId);
            var notification = BuildNotification(
                accountId,
                null,
                _accountDataHolder.AccountId,
                type,
                title,
                messageFactory(organization?.Name ?? "организации"),
                organization);

            await PersistAndSendAsync(new List<Notification> { notification });

            logger.Debug(correlationId, null, methodName, "Method finished", null, execTime.Elapsed);
            return CommandResult.OK;
        }

        private async Task<CommandResult> NotifyOrganizationVerificationAsync(
            Guid organizationId,
            bool approved,
            string? reason)
        {
            var correlationId = _correlationIdProvider.Get();
            var methodName = $"{LOGGER_NAME}{nameof(NotifyOrganizationVerificationAsync)}";
            var execTime = Stopwatch.StartNew();
            logger.Debug(correlationId, null, methodName, "Method started", null);

            var organization = await _organizationsRepository.GetOrganizationAsync(organizationId);
            var members = await GetOrganizationMemberIdsAsync(organizationId);
            if (members.Count == 0 && organization?.CreatedByAccountId != null)
                members.Add(organization.CreatedByAccountId.Value);

            var type = approved
                ? UserNotificationType.OrganizationVerificationApproved
                : UserNotificationType.OrganizationVerificationRejected;
            var title = approved ? "Организация верифицирована" : "Верификация отклонена";
            var message = approved
                ? $"Организация \"{organization?.Name}\" успешно прошла верификацию"
                : $"Верификация организации \"{organization?.Name}\" отклонена"
                  + (string.IsNullOrWhiteSpace(reason) ? string.Empty : $": {reason}");

            var notifications = members.Distinct().Select(accountId => BuildNotification(
                accountId,
                null,
                null,
                type,
                title,
                message,
                organization)).ToList();

            await PersistAndSendAsync(notifications);

            logger.Debug(correlationId, null, methodName, "Method finished", null, execTime.Elapsed);
            return CommandResult.OK;
        }

        private async Task<List<Guid>> GetEventLifecycleAudienceAsync(Guid eventId)
        {
            var ids = new HashSet<Guid>();

            var participants = await _participationsRepository.GetEventParticipantIdsAsync(eventId);
            if (participants != null)
            {
                foreach (var id in participants)
                    ids.Add(id);
            }

            var invitees = await _invitationsRepository.GetInvitedUsersAsync(eventId);
            if (invitees != null)
            {
                foreach (var id in invitees)
                    ids.Add(id);
            }

            var organizators = await _eventOrganizatorsRepository.GetAllOrganizerAccountIdsAsync(eventId);
            if (organizators != null)
            {
                foreach (var id in organizators)
                    ids.Add(id);
            }

            if (_accountDataHolder.AccountId != null)
                ids.Remove(_accountDataHolder.AccountId.Value);

            return ids.ToList();
        }

        private async Task<List<Guid>> GetParticipationActivityRecipientsAsync(Guid eventId)
        {
            var ids = new HashSet<Guid>();

            if (_accountDataHolder.AccountId != null)
            {
                var subscribers = await _subscriptionsRepository.GetSubscribersIdsAsync(new SubscriptionsSearchRequest
                {
                    AccountId = _accountDataHolder.AccountId.Value,
                    NotifyParticipated = true
                });
                if (subscribers != null)
                {
                    foreach (var id in subscribers)
                        ids.Add(id);
                }
            }

            var organizators = await _eventOrganizatorsRepository.GetAllOrganizerAccountIdsAsync(eventId);
            if (organizators != null)
            {
                foreach (var id in organizators)
                    ids.Add(id);
            }

            if (_accountDataHolder.AccountId != null)
                ids.Remove(_accountDataHolder.AccountId.Value);

            return ids.ToList();
        }

        private async Task<HashSet<Guid>> GetNewMessageRecipientsAsync(
            Guid conversationId,
            Guid? eventId,
            Guid? excludeAccountId)
        {
            var ids = new HashSet<Guid>();

            if (eventId != null)
            {
                var participants = await _participationsRepository.GetEventParticipantIdsAsync(eventId.Value);
                if (participants != null)
                {
                    foreach (var id in participants)
                        ids.Add(id);
                }

                var organizators = await _eventOrganizatorsRepository.GetAllOrganizerAccountIdsAsync(eventId.Value);
                if (organizators != null)
                {
                    foreach (var id in organizators)
                        ids.Add(id);
                }
            }

            var authors = await _conversationRepository.GetConversationAuthorAccountIdsAsync(conversationId);
            if (authors != null)
            {
                foreach (var id in authors)
                    ids.Add(id);
            }

            if (excludeAccountId != null)
                ids.Remove(excludeAccountId.Value);

            return ids;
        }

        private static string BuildMessagePreview(string? text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return string.Empty;

            return text.Length > 100
                ? text.Substring(0, 100) + "..."
                : text;
        }

        private async Task<List<Guid>> GetInvitationStakeholdersAsync(
            Guid eventId,
            Guid? inviterAccountId,
            Guid invitedAccountId)
        {
            var ids = new HashSet<Guid>();
            if (inviterAccountId != null)
                ids.Add(inviterAccountId.Value);

            var organizators = await _eventOrganizatorsRepository.GetAllOrganizerAccountIdsAsync(eventId);
            if (organizators != null)
            {
                foreach (var id in organizators)
                    ids.Add(id);
            }

            ids.Remove(invitedAccountId);
            if (_accountDataHolder.AccountId != null)
                ids.Remove(_accountDataHolder.AccountId.Value);

            return ids.ToList();
        }

        private async Task<List<Guid>> GetReportSubjectAccountIdsAsync(ContentReport report, bool includeEventOrganizers)
        {
            var ids = new List<Guid>();

            switch (report.TargetType)
            {
                case ReportTargetType.Account:
                    ids.Add(report.ReportedAccountId ?? report.TargetId);
                    break;

                case ReportTargetType.Message:
                case ReportTargetType.Photo:
                    if (report.ReportedAccountId != null)
                        ids.Add(report.ReportedAccountId.Value);
                    if (report.OrganizationId != null)
                        ids.AddRange(await GetOrganizationMemberIdsAsync(report.OrganizationId.Value));
                    break;

                case ReportTargetType.Organization:
                    if (report.OrganizationId != null)
                        ids.AddRange(await GetOrganizationMemberIdsAsync(report.OrganizationId.Value));
                    break;

                case ReportTargetType.EventOrganizator:
                    if (report.ReportedAccountId != null)
                        ids.Add(report.ReportedAccountId.Value);
                    if (report.OrganizationId != null)
                        ids.AddRange(await GetOrganizationMemberIdsAsync(report.OrganizationId.Value));
                    break;

                case ReportTargetType.Event:
                    if (includeEventOrganizers && report.EventId != null)
                        ids.AddRange(await _eventOrganizatorsRepository.GetAllOrganizerAccountIdsAsync(report.EventId.Value));
                    break;
            }

            return ids.Distinct().ToList();
        }

        private async Task<List<Guid>> GetOrganizationMemberIdsAsync(Guid organizationId)
        {
            var members = await _organizationsRepository.GetMembersByOrganizationIdAsync(organizationId, onlyActive: true);
            return members?.Select(m => m.AccountId).Distinct().ToList() ?? new List<Guid>();
        }

        private async Task<List<Guid>> GetPlatformStaffAccountIdsAsync()
        {
            var roles = await _accountPlatformRolesRepository.GetAllAsync(role: null, onlyActive: true);
            return roles?.Select(r => r.AccountId).Distinct().ToList() ?? new List<Guid>();
        }

        private static ContentReportNotificationData BuildContentReportData(
            ContentReport report,
            string? queue,
            ReportResolutionAction? action = null)
        {
            return new ContentReportNotificationData
            {
                ReportId = report.Id,
                TargetType = report.TargetType,
                TargetId = report.TargetId,
                EventId = report.EventId,
                OrganizationId = report.OrganizationId,
                ReasonCode = report.Reason?.Code,
                ReasonName = report.Reason?.Name,
                ResolutionAction = action ?? report.ResolutionAction,
                Queue = queue
            };
        }

        private static string BuildFiledAgainstYouMessage(ContentReport report)
        {
            return report.TargetType switch
            {
                ReportTargetType.Account => "На ваш профиль поступила жалоба. Её рассмотрит модерация площадки.",
                ReportTargetType.Organization => "На организацию поступила жалоба. Её рассмотрит модерация площадки.",
                ReportTargetType.EventOrganizator => "На вас как на организатора поступила жалоба. Её рассмотрит модерация площадки.",
                ReportTargetType.Message => "На ваше сообщение поступила жалоба.",
                ReportTargetType.Photo => "На ваше фото поступила жалоба.",
                _ => "На ваш контент поступила жалоба."
            };
        }

        private static string BuildPlatformQueueMessage(ContentReport report)
        {
            return report.TargetType switch
            {
                ReportTargetType.Event => "Поступила жалоба на мероприятие.",
                ReportTargetType.Account => "Поступила жалоба на профиль пользователя.",
                ReportTargetType.Organization => "Поступила жалоба на организацию.",
                ReportTargetType.EventOrganizator => "Поступила жалоба на организатора.",
                _ => "Поступила жалоба, требующая рассмотрения площадкой."
            };
        }

        private static Notification BuildNotification(
            Guid accountId,
            Guid? eventId,
            Guid? relatedAccountId,
            UserNotificationType type,
            string? title,
            string message,
            object data)
        {
            return new Notification
            {
                Id = Guid.NewGuid(),
                AccountId = accountId,
                EventId = eventId,
                CreatedAt = DateTime.UtcNow,
                Title = title,
                Message = message,
                RelatedAccountId = relatedAccountId,
                Type = type,
                Data = data
            };
        }

        private async Task PersistAndSendAsync(List<Notification> notifications)
        {
            if (notifications == null || notifications.Count == 0)
                return;

            await _notificationsRepository.CreateNotificationsAsync(notifications);
            var wsTasks = notifications.Select(n => SendToUserAsync(n.AccountId, n));
            await Task.WhenAll(wsTasks);
        }
        #endregion

        #endregion structured notifications




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

                        if (socket.State == WebSocketState.CloseReceived)
                        {
                            await socket.CloseOutputAsync(
                                WebSocketCloseStatus.NormalClosure,
                                "Закрытие по запросу клиента",
                                CancellationToken.None);
                        }
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
                logger.Warn(correlationId, null, methodName,
                    $"WebSocket connection lost: accountId={accountId}, reason={ex.Message}", null);
            }
            finally
            {
                _connectionManager.RemoveConnection(accountId, connectionId);

                if (socket.State != WebSocketState.Closed && socket.State != WebSocketState.Aborted)
                {
                    try { socket.Abort(); } catch { }
                }

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
                var json = new JObject(rawMessage);
                //using var doc = JsonDocument.Parse(rawMessage);
                //var type = doc.RootElement.TryGetProperty("type", out var typeProp)
                //    ? typeProp.GetString()
                //    : null;

                //switch (type)
                //{
                //    case "ping":
                //        await SendNotificationAsync(socket, new { type = "pong", timestamp = DateTimeOffset.UtcNow });
                //        break;

                //    default:
                //        logger.Debug(correlationId, null, methodName,
                //            $"Unknown message type '{type}' from accountId={accountId}", null);
                //        await SendNotificationAsync(socket, new
                //        {
                //            type = "error",
                //            message = $"Неизвестный тип сообщения: '{type}'"
                //        });
                //        break;
                //}
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

            var json = JsonConvert.SerializeObject(data);
            //var json = JsonSerializer.Serialize(data, new JsonSerializerOptions
            //{
            //    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            //    WriteIndented = false
            //});

            var bytes = Encoding.UTF8.GetBytes(json);
            await socket.SendAsync(
                new ArraySegment<byte>(bytes),
                WebSocketMessageType.Text,
                endOfMessage: true,
                CancellationToken.None);
        }
        #endregion private
    }
}
