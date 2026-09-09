using System.Diagnostics;
using AutoMapper;
using EList.Common.CorrelationId;
using EList.Common.Logger;
using EList.Common.Models;
using EList.Common.Support;
using EList.Models.Enums;
using EList.Models.Events;
using EList.Models.Invitations;
using EList.Models.Orders;
using EList.Repositories.Interfaces;
using EList.Services.Impl.Payments;
using EList.Services.Interfaces;
using NLog;

namespace EList.Services.Impl
{
    public class OrdersService : IOrdersService
    {
        #region logger
        private static readonly ILogger log = LogManager.GetCurrentClassLogger();
        private static readonly ILoggerWrapper logger = new NLogLoggerWrapper(log);
        private const string LOGGER_NAME = "EList.Services.Impl.OrdersService.";
        #endregion

        private readonly ICorrelationIdProvider _correlationIdProvider;
        private readonly IAccountDataHolder _accountDataHolder;
        private readonly IOrdersRepository _ordersRepository;
        private readonly IEventsRepository _eventsRepository;
        private readonly IEventOrganizatorsRepository _eventOrganizatorsRepository;
        private readonly IOrganizationsRepository _organizationsRepository;
        private readonly IParticipationsRepository _participationsRepository;
        private readonly IInvitationsRepository _invitationsRepository;
        private readonly IParticipantsBWListRepository _participantsBWListRepository;
        private readonly IModerationPenaltiesService _moderationPenaltiesService;
        private readonly INotificationsService _notificationsService;
        private readonly IPaymentProvider _paymentProvider;
        private readonly IAccountsRepository _accountsRepository;
        private readonly IMapper _mapper;

        public OrdersService(
            ICorrelationIdProvider correlationIdProvider,
            IAccountDataHolder accountDataHolder,
            IOrdersRepository ordersRepository,
            IEventsRepository eventsRepository,
            IEventOrganizatorsRepository eventOrganizatorsRepository,
            IOrganizationsRepository organizationsRepository,
            IParticipationsRepository participationsRepository,
            IInvitationsRepository invitationsRepository,
            IParticipantsBWListRepository participantsBWListRepository,
            IModerationPenaltiesService moderationPenaltiesService,
            INotificationsService notificationsService,
            IPaymentProvider paymentProvider,
            IAccountsRepository accountsRepository,
            IMapper mapper)
        {
            _correlationIdProvider = correlationIdProvider ?? throw new ArgumentNullException(nameof(correlationIdProvider));
            _accountDataHolder = accountDataHolder ?? throw new ArgumentNullException(nameof(accountDataHolder));
            _ordersRepository = ordersRepository ?? throw new ArgumentNullException(nameof(ordersRepository));
            _eventsRepository = eventsRepository ?? throw new ArgumentNullException(nameof(eventsRepository));
            _eventOrganizatorsRepository = eventOrganizatorsRepository ?? throw new ArgumentNullException(nameof(eventOrganizatorsRepository));
            _organizationsRepository = organizationsRepository ?? throw new ArgumentNullException(nameof(organizationsRepository));
            _participationsRepository = participationsRepository ?? throw new ArgumentNullException(nameof(participationsRepository));
            _invitationsRepository = invitationsRepository ?? throw new ArgumentNullException(nameof(invitationsRepository));
            _participantsBWListRepository = participantsBWListRepository ?? throw new ArgumentNullException(nameof(participantsBWListRepository));
            _moderationPenaltiesService = moderationPenaltiesService ?? throw new ArgumentNullException(nameof(moderationPenaltiesService));
            _notificationsService = notificationsService ?? throw new ArgumentNullException(nameof(notificationsService));
            _paymentProvider = paymentProvider ?? throw new ArgumentNullException(nameof(paymentProvider));
            _accountsRepository = accountsRepository ?? throw new ArgumentNullException(nameof(accountsRepository));
            _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
        }

        public async Task<CommandResult<CreateOrderResponse>> CreateOrderAsync(CreateOrderRequest request)
        {
            var correlationId = _correlationIdProvider.Get();
            var methodName = $"{LOGGER_NAME}{nameof(CreateOrderAsync)}";
            var execTime = Stopwatch.StartNew();
            logger.Debug(correlationId, null, methodName, "Method started", null);

            if (_accountDataHolder.AccountId == null)
                return CommandResult<CreateOrderResponse>.Fail(ErrorCode.UserMustBeAuthorized, "Пользователь не авторизован");

            if (request == null || request.EventId == Guid.Empty)
                return CommandResult<CreateOrderResponse>.Fail(ErrorCode.InvalidValue, "Не указано мероприятие");

            var quantity = request.Quantity <= 0 ? 1 : request.Quantity;
            if (quantity > 20)
                return CommandResult<CreateOrderResponse>.Fail(ErrorCode.InvalidValue, "Максимум 20 билетов в одном заказе");

            if (!PaymentSettings.IsTicketSalesGloballyEnabled())
            {
                return CommandResult<CreateOrderResponse>.Fail(ErrorCode.InvalidValue,
                    "Продажа билетов временно отключена");
            }

            if (!string.IsNullOrWhiteSpace(request.IdempotencyKey))
            {
                var existing = await _ordersRepository.GetOrderByIdempotencyKeyAsync(request.IdempotencyKey.Trim());
                if (existing != null)
                {
                    var full = await _ordersRepository.GetOrderFullAsync(existing.Id) ?? existing;
                    logger.Debug(correlationId, null, methodName, "Method finished (idempotent)", null, execTime.Elapsed);
                    return new CommandResult<CreateOrderResponse>(ToCreateResponse(full, null, null, full.Status == OrderStatus.Paid));
                }
            }

            var eventItem = await _eventsRepository.GetEventAsync(request.EventId);
            if (eventItem == null)
                return CommandResult<CreateOrderResponse>.Fail(ErrorCode.EventNotFound, $"Событие с id='{request.EventId}' не найдено");

            if (eventItem.Active == false)
                return CommandResult<CreateOrderResponse>.Fail(ErrorCode.EventCancelled, "Мероприятие было отменено");

            if (eventItem.Parameters == null || !eventItem.Parameters.TicketsEnabled)
            {
                return CommandResult<CreateOrderResponse>.Fail(ErrorCode.InvalidValue,
                    "Для этого мероприятия продажа билетов не включена");
            }

            var accessError = await AssertCanBuyTicketAsync(eventItem);
            if (accessError != null)
                return CommandResult<CreateOrderResponse>.Fail(accessError.ErrorCode, accessError.Message);

            var buyerId = _accountDataHolder.AccountId.Value;
            if (await _participationsRepository.IsUserParticipatedAsync(buyerId, eventItem.Id))
            {
                return CommandResult<CreateOrderResponse>.Fail(ErrorCode.InvalidValue,
                    "Вы уже участвуете в этом мероприятии");
            }

            if (eventItem.Parameters.MaxPersonsCount > 0)
            {
                var participantsCount = await _participationsRepository.GetParticipantsCountAsync(eventItem.Id);
                if (participantsCount + quantity > eventItem.Parameters.MaxPersonsCount)
                {
                    return CommandResult<CreateOrderResponse>.Fail(ErrorCode.EventIsFull,
                        "Недостаточно мест для указанного количества билетов");
                }
            }

            var sellerOrgId = await ResolveSellerOrganizationIdAsync(eventItem.Id);
            if (sellerOrgId == null)
            {
                return CommandResult<CreateOrderResponse>.Fail(ErrorCode.OrganizationNotVerified,
                    "Нет организации-продавца с правом продажи билетов");
            }

            var settings = PaymentSettings.Load();
            var unitPrice = ToMoney(eventItem.Parameters.Cost);
            var amountTotal = unitPrice * quantity;
            var amountCommission = Math.Round(
                amountTotal * settings.CommissionPercent / 100m,
                2,
                MidpointRounding.AwayFromZero);
            if (amountCommission > amountTotal)
                amountCommission = amountTotal;
            var amountSeller = amountTotal - amountCommission;

            var order = new Order
            {
                EventId = eventItem.Id,
                BuyerAccountId = buyerId,
                SellerOrganizationId = sellerOrgId.Value,
                Quantity = quantity,
                AmountTotal = amountTotal,
                AmountSeller = amountSeller,
                AmountCommission = amountCommission,
                Currency = settings.Currency,
                Status = OrderStatus.Pending,
                IdempotencyKey = string.IsNullOrWhiteSpace(request.IdempotencyKey)
                    ? null
                    : request.IdempotencyKey.Trim(),
                CreateDate = DateTimeOffset.UtcNow
            };

            order.Id = await _ordersRepository.CreateOrderAsync(order);

            if (amountTotal == 0)
            {
                await FulfillPaidOrderAsync(order.Id, buyerId, eventItem.Id, quantity);
                var paid = await _ordersRepository.GetOrderFullAsync(order.Id) ?? order;
                logger.Debug(correlationId, null, methodName, "Method finished (free)", null, execTime.Elapsed);
                return new CommandResult<CreateOrderResponse>(ToCreateResponse(paid, null, null, paidImmediately: true));
            }

            var payment = await _paymentProvider.CreatePaymentAsync(new PaymentCreationRequest
            {
                OrderId = order.Id,
                Amount = amountTotal,
                Currency = settings.Currency,
                Description = $"Билеты: {eventItem.Name} × {quantity}",
                ReturnUrl = settings.ReturnUrl,
                IdempotencyKey = order.IdempotencyKey,
                BuyerAccountId = buyerId,
                EventId = eventItem.Id
            });

            await _ordersRepository.SetProviderPaymentAsync(
                order.Id,
                _paymentProvider.Kind,
                payment.ProviderPaymentId);

            var pending = await _ordersRepository.GetOrderFullAsync(order.Id) ?? order;
            logger.Debug(correlationId, null, methodName, "Method finished", null, execTime.Elapsed);
            return new CommandResult<CreateOrderResponse>(ToCreateResponse(
                pending,
                payment.ConfirmationUrl,
                payment.ProviderPaymentId,
                paidImmediately: false));
        }

        public async Task<CommandResult<OrderResponse>> CompletePaymentAsync(CompletePaymentRequest request)
        {
            var correlationId = _correlationIdProvider.Get();
            var methodName = $"{LOGGER_NAME}{nameof(CompletePaymentAsync)}";
            var execTime = Stopwatch.StartNew();
            logger.Debug(correlationId, null, methodName, "Method started", null);

            if (_accountDataHolder.AccountId == null)
                return CommandResult<OrderResponse>.Fail(ErrorCode.UserMustBeAuthorized, "Пользователь не авторизован");

            if (request == null || (request.OrderId == null && string.IsNullOrWhiteSpace(request.ProviderPaymentId)))
            {
                return CommandResult<OrderResponse>.Fail(ErrorCode.InvalidValue,
                    "Укажите orderId или providerPaymentId");
            }

            Order? order = null;
            if (request.OrderId != null)
                order = await _ordersRepository.GetOrderAsync(request.OrderId.Value);

            if (order == null && !string.IsNullOrWhiteSpace(request.ProviderPaymentId))
            {
                order = await _ordersRepository.GetOrderByProviderPaymentAsync(
                    _paymentProvider.Kind,
                    request.ProviderPaymentId.Trim());
            }

            if (order == null)
                return CommandResult<OrderResponse>.Fail(ErrorCode.InvalidValue, "Заказ не найден");

            if (order.BuyerAccountId != _accountDataHolder.AccountId.Value)
                return CommandResult<OrderResponse>.Fail(ErrorCode.AccessError, "Нет доступа к заказу");

            if (order.Status == OrderStatus.Paid)
            {
                var already = await _ordersRepository.GetOrderFullAsync(order.Id) ?? order;
                logger.Debug(correlationId, null, methodName, "Method finished (already paid)", null, execTime.Elapsed);
                return new CommandResult<OrderResponse>(_mapper.Map<OrderResponse>(already));
            }

            if (order.Status != OrderStatus.Pending && order.Status != OrderStatus.Authorized)
            {
                return CommandResult<OrderResponse>.Fail(ErrorCode.InvalidValue,
                    $"Заказ в статусе {order.Status} нельзя оплатить");
            }

            var providerPaymentId = !string.IsNullOrWhiteSpace(request.ProviderPaymentId)
                ? request.ProviderPaymentId.Trim()
                : order.ProviderPaymentId;

            if (string.IsNullOrWhiteSpace(providerPaymentId))
            {
                return CommandResult<OrderResponse>.Fail(ErrorCode.InvalidValue,
                    "У заказа нет идентификатора платежа");
            }

            if (!_paymentProvider.SupportsManualComplete)
            {
                return CommandResult<OrderResponse>.Fail(ErrorCode.InvalidValue,
                    "Ручное подтверждение недоступно для текущего платёжного провайдера; дождитесь webhook");
            }

            await _paymentProvider.CompleteManuallyAsync(providerPaymentId);

            var payload = YooKassaWebhookPayloadFactory.BuildPaymentSucceeded(
                providerPaymentId,
                order.Id,
                order.AmountTotal,
                order.Currency);

            var webhookResult = await ProcessYooKassaWebhookAsync(payload);
            if (!webhookResult.Success)
                return CommandResult<OrderResponse>.Fail(webhookResult.ErrorCode, webhookResult.Message);

            var paid = await _ordersRepository.GetOrderFullAsync(order.Id) ?? order;
            logger.Debug(correlationId, null, methodName, "Method finished", null, execTime.Elapsed);
            return new CommandResult<OrderResponse>(_mapper.Map<OrderResponse>(paid));
        }

        public async Task<CommandResult> ProcessYooKassaWebhookAsync(string rawPayload)
        {
            var correlationId = _correlationIdProvider.Get();
            var methodName = $"{LOGGER_NAME}{nameof(ProcessYooKassaWebhookAsync)}";
            var execTime = Stopwatch.StartNew();
            logger.Debug(correlationId, null, methodName, "Method started", null);

            if (string.IsNullOrWhiteSpace(rawPayload))
                return CommandResult.Fail(ErrorCode.IsNullOrEmpty, "Пустое тело webhook");

            YooKassaWebhookNotification? notification;
            try
            {
                notification = Newtonsoft.Json.JsonConvert.DeserializeObject<YooKassaWebhookNotification>(rawPayload);
            }
            catch (Exception ex)
            {
                logger.Debug(correlationId, null, methodName, $"Invalid JSON: {ex.Message}", null);
                return CommandResult.Fail(ErrorCode.FormatError, "Некорректный JSON webhook ЮKassa");
            }

            if (notification?.Object == null || string.IsNullOrWhiteSpace(notification.Object.Id))
                return CommandResult.Fail(ErrorCode.InvalidValue, "В webhook нет object.id платежа");

            var eventName = string.IsNullOrWhiteSpace(notification.Event)
                ? "payment.unknown"
                : notification.Event.Trim();
            var providerPaymentId = notification.Object.Id.Trim();
            var providerEventId = $"{eventName}:{providerPaymentId}";

            var existingWebhook = await _ordersRepository.GetWebhookEventAsync(
                PaymentProvider.Yookassa, providerEventId);
            if (existingWebhook?.ProcessedAt != null)
            {
                logger.Debug(correlationId, null, methodName, "Method finished (idempotent)", null, execTime.Elapsed);
                return CommandResult.OK;
            }

            Guid webhookId;
            if (existingWebhook != null)
            {
                webhookId = existingWebhook.Id;
            }
            else
            {
                webhookId = await _ordersRepository.CreateWebhookEventAsync(new PaymentWebhookEvent
                {
                    Provider = PaymentProvider.Yookassa,
                    ProviderEventId = providerEventId,
                    Payload = rawPayload,
                    ReceivedAt = DateTimeOffset.UtcNow
                });
            }

            Order? order = await _ordersRepository.GetOrderByProviderPaymentAsync(
                PaymentProvider.Yookassa, providerPaymentId);

            if (order == null
                && notification.Object.Metadata != null
                && notification.Object.Metadata.TryGetValue("orderId", out var orderIdRaw)
                && Guid.TryParse(orderIdRaw, out var orderIdFromMeta))
            {
                order = await _ordersRepository.GetOrderAsync(orderIdFromMeta);
            }

            if (order == null)
            {
                await _ordersRepository.MarkWebhookProcessedAsync(webhookId, null);
                logger.Debug(correlationId, null, methodName,
                    $"Webhook stored but order not found for payment '{providerPaymentId}'", null, execTime.Elapsed);
                // OK: событие зафиксировано, ретраи ЮKassa не нужны.
                return CommandResult.OK;
            }

            if (string.Equals(eventName, "payment.succeeded", StringComparison.OrdinalIgnoreCase)
                || string.Equals(notification.Object.Status, "succeeded", StringComparison.OrdinalIgnoreCase))
            {
                if (_paymentProvider.SupportsManualComplete)
                    await _paymentProvider.CompleteManuallyAsync(providerPaymentId);

                if (string.IsNullOrWhiteSpace(order.ProviderPaymentId))
                {
                    await _ordersRepository.SetProviderPaymentAsync(
                        order.Id, PaymentProvider.Yookassa, providerPaymentId);
                }

                await FulfillPaidOrderAsync(order.Id, order.BuyerAccountId, order.EventId, order.Quantity);
            }
            else if (string.Equals(eventName, "payment.canceled", StringComparison.OrdinalIgnoreCase)
                     || string.Equals(notification.Object.Status, "canceled", StringComparison.OrdinalIgnoreCase))
            {
                if (order.Status == OrderStatus.Pending || order.Status == OrderStatus.Authorized)
                    await _ordersRepository.UpdateOrderStatusAsync(order.Id, OrderStatus.Canceled);
            }
            else if (string.Equals(eventName, "payment.waiting_for_capture", StringComparison.OrdinalIgnoreCase))
            {
                if (order.Status == OrderStatus.Pending)
                    await _ordersRepository.UpdateOrderStatusAsync(order.Id, OrderStatus.Authorized);
            }

            await _ordersRepository.MarkWebhookProcessedAsync(webhookId, order.Id);
            logger.Debug(correlationId, null, methodName, "Method finished", null, execTime.Elapsed);
            return CommandResult.OK;
        }

        public async Task<CommandResult<OrderResponse>> GetOrderAsync(Guid orderId)
        {
            var correlationId = _correlationIdProvider.Get();
            var methodName = $"{LOGGER_NAME}{nameof(GetOrderAsync)}";
            var execTime = Stopwatch.StartNew();
            logger.Debug(correlationId, null, methodName, "Method started", null);

            if (_accountDataHolder.AccountId == null)
                return CommandResult<OrderResponse>.Fail(ErrorCode.UserMustBeAuthorized, "Пользователь не авторизован");

            var order = await _ordersRepository.GetOrderFullAsync(orderId);
            if (order == null)
                return CommandResult<OrderResponse>.Fail(ErrorCode.InvalidValue, "Заказ не найден");

            if (order.BuyerAccountId != _accountDataHolder.AccountId.Value)
            {
                var isOrg = await _eventOrganizatorsRepository.IsAccountEventOrganizatorAsync(
                    order.EventId, _accountDataHolder.AccountId.Value);
                if (!isOrg)
                    return CommandResult<OrderResponse>.Fail(ErrorCode.AccessError, "Нет доступа к заказу");
            }

            logger.Debug(correlationId, null, methodName, "Method finished", null, execTime.Elapsed);
            return new CommandResult<OrderResponse>(_mapper.Map<OrderResponse>(order));
        }

        public async Task<CommandResult<List<OrderResponse>>> GetMyOrdersAsync()
        {
            var correlationId = _correlationIdProvider.Get();
            var methodName = $"{LOGGER_NAME}{nameof(GetMyOrdersAsync)}";
            var execTime = Stopwatch.StartNew();
            logger.Debug(correlationId, null, methodName, "Method started", null);

            if (_accountDataHolder.AccountId == null)
                return CommandResult<List<OrderResponse>>.Fail(ErrorCode.UserMustBeAuthorized, "Пользователь не авторизован");

            var orders = await _ordersRepository.GetOrdersByBuyerAsync(_accountDataHolder.AccountId.Value) ?? new List<Order>();
            var response = orders.Select(o => _mapper.Map<OrderResponse>(o)).ToList();

            logger.Debug(correlationId, null, methodName, "Method finished", null, execTime.Elapsed);
            return new CommandResult<List<OrderResponse>>(response);
        }

        public async Task<CommandResult<List<TicketResponse>>> GetMyTicketsAsync(Guid? eventId = null)
        {
            var correlationId = _correlationIdProvider.Get();
            var methodName = $"{LOGGER_NAME}{nameof(GetMyTicketsAsync)}";
            var execTime = Stopwatch.StartNew();
            logger.Debug(correlationId, null, methodName, "Method started", null);

            if (_accountDataHolder.AccountId == null)
                return CommandResult<List<TicketResponse>>.Fail(ErrorCode.UserMustBeAuthorized, "Пользователь не авторизован");

            var tickets = await _ordersRepository.GetTicketsByHolderAsync(_accountDataHolder.AccountId.Value)
                ?? new List<Ticket>();

            if (eventId != null)
                tickets = tickets.Where(t => t.EventId == eventId.Value).ToList();

            var response = tickets.Select(t => _mapper.Map<TicketResponse>(t)).ToList();
            logger.Debug(correlationId, null, methodName, "Method finished", null, execTime.Elapsed);
            return new CommandResult<List<TicketResponse>>(response);
        }

        public async Task<CommandResult<TicketResponse>> GetTicketByCodeAsync(string code)
        {
            var correlationId = _correlationIdProvider.Get();
            var methodName = $"{LOGGER_NAME}{nameof(GetTicketByCodeAsync)}";
            var execTime = Stopwatch.StartNew();
            logger.Debug(correlationId, null, methodName, "Method started", null);

            if (_accountDataHolder.AccountId == null)
                return CommandResult<TicketResponse>.Fail(ErrorCode.UserMustBeAuthorized, "Пользователь не авторизован");

            if (string.IsNullOrWhiteSpace(code))
                return CommandResult<TicketResponse>.Fail(ErrorCode.InvalidValue, "Не указан код билета");

            var ticket = await _ordersRepository.GetTicketByCodeAsync(code.Trim());
            if (ticket == null)
                return CommandResult<TicketResponse>.Fail(ErrorCode.InvalidValue, "Билет не найден");

            var isHolder = ticket.HolderAccountId == _accountDataHolder.AccountId.Value;
            var isOrg = await _eventOrganizatorsRepository.IsAccountEventOrganizatorAsync(
                ticket.EventId, _accountDataHolder.AccountId.Value);
            if (!isHolder && !isOrg)
                return CommandResult<TicketResponse>.Fail(ErrorCode.AccessError, "Нет доступа к билету");

            logger.Debug(correlationId, null, methodName, "Method finished", null, execTime.Elapsed);
            return new CommandResult<TicketResponse>(_mapper.Map<TicketResponse>(ticket));
        }

        public async Task<CommandResult<TicketResponse>> ValidateTicketForEventAsync(TicketCheckInRequest request)
        {
            var correlationId = _correlationIdProvider.Get();
            var methodName = $"{LOGGER_NAME}{nameof(ValidateTicketForEventAsync)}";
            var execTime = Stopwatch.StartNew();
            logger.Debug(correlationId, null, methodName, "Method started", null);

            var access = await AssertOrganizerCanManageTicketsAsync(request);
            if (!access.Success)
                return CommandResult<TicketResponse>.Fail(access.ErrorCode, access.Message);

            var ticketResult = await LoadTicketForEventCheckInAsync(request);
            if (!ticketResult.Success)
                return ticketResult;

            logger.Debug(correlationId, null, methodName, "Method finished", null, execTime.Elapsed);
            return ticketResult;
        }

        public async Task<CommandResult<TicketResponse>> CheckInTicketAsync(TicketCheckInRequest request)
        {
            var correlationId = _correlationIdProvider.Get();
            var methodName = $"{LOGGER_NAME}{nameof(CheckInTicketAsync)}";
            var execTime = Stopwatch.StartNew();
            logger.Debug(correlationId, null, methodName, "Method started", null);

            var access = await AssertOrganizerCanManageTicketsAsync(request);
            if (!access.Success)
                return CommandResult<TicketResponse>.Fail(access.ErrorCode, access.Message);

            var ticketResult = await LoadTicketForEventCheckInAsync(request);
            if (!ticketResult.Success)
                return ticketResult;

            var ticket = await _ordersRepository.GetTicketByCodeAsync(request.Code.Trim());
            if (ticket == null)
                return CommandResult<TicketResponse>.Fail(ErrorCode.InvalidValue, "Билет не найден");

            if (ticket.Status == TicketStatus.Used)
            {
                var when = ticket.CheckedInAt?.ToString("u") ?? "ранее";
                return CommandResult<TicketResponse>.Fail(ErrorCode.InvalidValue,
                    $"Билет уже отмечен как использованный ({when})");
            }

            if (ticket.Status != TicketStatus.Issued)
            {
                return CommandResult<TicketResponse>.Fail(ErrorCode.InvalidValue,
                    $"Билет в статусе {ticket.Status} нельзя отметить на входе");
            }

            var checkedInAt = DateTimeOffset.UtcNow;
            await _ordersRepository.CheckInTicketAsync(
                ticket.Id,
                _accountDataHolder.AccountId!.Value,
                checkedInAt);

            var updated = await _ordersRepository.GetTicketByCodeAsync(ticket.Code) ?? ticket;
            logger.Debug(correlationId, null, methodName, "Method finished", null, execTime.Elapsed);
            return new CommandResult<TicketResponse>(_mapper.Map<TicketResponse>(updated));
        }

        public async Task<CommandResult<TicketResponse>> TransferTicketAsync(TransferTicketRequest request)
        {
            var correlationId = _correlationIdProvider.Get();
            var methodName = $"{LOGGER_NAME}{nameof(TransferTicketAsync)}";
            var execTime = Stopwatch.StartNew();
            logger.Debug(correlationId, null, methodName, "Method started", null);

            if (_accountDataHolder.AccountId == null)
                return CommandResult<TicketResponse>.Fail(ErrorCode.UserMustBeAuthorized, "Пользователь не авторизован");

            if (request == null || request.NewHolderAccountId == Guid.Empty)
            {
                return CommandResult<TicketResponse>.Fail(ErrorCode.InvalidValue,
                    "Укажите нового владельца билета");
            }

            if (request.TicketId == null && string.IsNullOrWhiteSpace(request.Code))
            {
                return CommandResult<TicketResponse>.Fail(ErrorCode.InvalidValue,
                    "Укажите ticketId или code");
            }

            Ticket? ticket = null;
            if (request.TicketId != null)
                ticket = await _ordersRepository.GetTicketAsync(request.TicketId.Value);
            if (ticket == null && !string.IsNullOrWhiteSpace(request.Code))
                ticket = await _ordersRepository.GetTicketByCodeAsync(request.Code.Trim());

            if (ticket == null)
                return CommandResult<TicketResponse>.Fail(ErrorCode.InvalidValue, "Билет не найден");

            var actorId = _accountDataHolder.AccountId.Value;
            if (ticket.HolderAccountId != actorId)
            {
                return CommandResult<TicketResponse>.Fail(ErrorCode.AccessError,
                    "Передать можно только свой билет");
            }

            if (ticket.Status != TicketStatus.Issued)
            {
                return CommandResult<TicketResponse>.Fail(ErrorCode.InvalidValue,
                    $"Билет в статусе {ticket.Status} нельзя передать");
            }

            if (request.NewHolderAccountId == ticket.HolderAccountId)
            {
                return CommandResult<TicketResponse>.Fail(ErrorCode.InvalidValue,
                    "Новый владелец совпадает с текущим");
            }

            var newHolder = await _accountsRepository.GetAccountAsync(request.NewHolderAccountId);
            if (newHolder == null || !newHolder.Active)
            {
                return CommandResult<TicketResponse>.Fail(ErrorCode.AccountNotFound,
                    "Аккаунт нового владельца не найден или неактивен");
            }

            var eventItem = await _eventsRepository.GetEventAsync(ticket.EventId);
            if (eventItem == null)
                return CommandResult<TicketResponse>.Fail(ErrorCode.EventNotFound, "Мероприятие не найдено");

            if (eventItem.Active == false)
                return CommandResult<TicketResponse>.Fail(ErrorCode.EventCancelled, "Мероприятие было отменено");

            var recipientAccess = await AssertAccountCanHoldTicketAsync(eventItem, request.NewHolderAccountId);
            if (recipientAccess != null)
                return CommandResult<TicketResponse>.Fail(recipientAccess.ErrorCode, recipientAccess.Message);

            var previousHolderId = ticket.HolderAccountId;
            await _ordersRepository.ReassignTicketHolderAsync(ticket.Id, request.NewHolderAccountId);

            // Участие: отдельные сущности. Перенос «идёт на событие» вместе с билетом.
            var previousStillHasTicket = await HolderHasActiveTicketForEventAsync(
                previousHolderId, ticket.EventId, excludeTicketId: ticket.Id);
            if (!previousStillHasTicket
                && await _participationsRepository.IsUserParticipatedAsync(previousHolderId, ticket.EventId))
            {
                await _participationsRepository.LeaveEventAsync(previousHolderId, ticket.EventId);
            }

            if (!await _participationsRepository.IsUserParticipatedAsync(request.NewHolderAccountId, ticket.EventId))
            {
                if (eventItem.Parameters?.MaxPersonsCount > 0)
                {
                    var participantsCount = await _participationsRepository.GetParticipantsCountAsync(ticket.EventId);
                    if (participantsCount >= eventItem.Parameters.MaxPersonsCount)
                    {
                        await _ordersRepository.ReassignTicketHolderAsync(ticket.Id, previousHolderId);
                        if (!previousStillHasTicket)
                            await _participationsRepository.ParticipateAsync(previousHolderId, ticket.EventId);

                        return CommandResult<TicketResponse>.Fail(ErrorCode.EventIsFull,
                            "Недостаточно мест, чтобы передать билет новому участнику");
                    }
                }

                await _participationsRepository.ParticipateAsync(request.NewHolderAccountId, ticket.EventId);
                await _notificationsService.NotifyParticipatedAsync(ticket.EventId, request.NewHolderAccountId);
            }

            var updated = await _ordersRepository.GetTicketAsync(ticket.Id) ?? ticket;
            updated.HolderAccountId = request.NewHolderAccountId;
            logger.Debug(correlationId, null, methodName, "Method finished", null, execTime.Elapsed);
            return new CommandResult<TicketResponse>(_mapper.Map<TicketResponse>(updated));
        }

        private async Task<bool> HolderHasActiveTicketForEventAsync(
            Guid holderAccountId,
            Guid eventId,
            Guid? excludeTicketId)
        {
            var tickets = await _ordersRepository.GetTicketsByHolderAsync(holderAccountId) ?? new List<Ticket>();
            return tickets.Any(t =>
                t.EventId == eventId
                && (excludeTicketId == null || t.Id != excludeTicketId.Value)
                && (t.Status == TicketStatus.Issued || t.Status == TicketStatus.Used));
        }

        private async Task<CommandResult?> AssertAccountCanHoldTicketAsync(Event eventItem, Guid accountId)
        {
            var participateBan = await _moderationPenaltiesService.AssertNotRestrictedAsync(
                accountId, ModerationPenaltyType.BanEventParticipate);
            if (!participateBan.Success)
                return participateBan;

            var eventBan = await _moderationPenaltiesService.AssertNotRestrictedAsync(
                accountId, ModerationPenaltyType.BanFromEvent, eventItem.Id);
            if (!eventBan.Success)
                return eventBan;

            if (eventItem.Parameters?.Private ?? false)
            {
                var whiteListCount = await _participantsBWListRepository.WhiteListPersonsCountAsync(eventItem.Id);
                if (whiteListCount == 0)
                {
                    var isUserInvited = await _invitationsRepository.IsUserInvitatedAsync(accountId, eventItem.Id);
                    if (!isUserInvited)
                    {
                        return CommandResult.Fail(ErrorCode.AccessError,
                            "Получатель не может принять билет на закрытое мероприятие без приглашения");
                    }
                }
                else if (!await _participantsBWListRepository.IsUserInWhiteListAsync(eventItem.Id, accountId))
                {
                    return CommandResult.Fail(ErrorCode.AccessError,
                        "Получатель не в белом списке закрытого мероприятия");
                }
            }
            else if (await _participantsBWListRepository.IsUserInBlackListAsync(eventItem.Id, accountId))
            {
                return CommandResult.Fail(ErrorCode.AccessError,
                    "Получатель в чёрном списке мероприятия");
            }

            return null;
        }

        private async Task<CommandResult> AssertOrganizerCanManageTicketsAsync(TicketCheckInRequest? request)
        {
            if (_accountDataHolder.AccountId == null)
                return CommandResult.Fail(ErrorCode.UserMustBeAuthorized, "Пользователь не авторизован");

            if (request == null || request.EventId == Guid.Empty)
                return CommandResult.Fail(ErrorCode.InvalidValue, "Не указано мероприятие");

            if (string.IsNullOrWhiteSpace(request.Code))
                return CommandResult.Fail(ErrorCode.InvalidValue, "Не указан код билета");

            var eventItem = await _eventsRepository.GetEventAsync(request.EventId);
            if (eventItem == null)
                return CommandResult.Fail(ErrorCode.EventNotFound, $"Событие с id='{request.EventId}' не найдено");

            var isOrg = await _eventOrganizatorsRepository.IsAccountEventOrganizatorAsync(
                request.EventId, _accountDataHolder.AccountId.Value);
            if (!isOrg && !_accountDataHolder.IsPlatformModeratorOrAbove)
            {
                return CommandResult.Fail(ErrorCode.AccessError,
                    "Отмечать билеты могут только организаторы мероприятия");
            }

            return CommandResult.OK;
        }

        private async Task<CommandResult<TicketResponse>> LoadTicketForEventCheckInAsync(TicketCheckInRequest request)
        {
            var ticket = await _ordersRepository.GetTicketByCodeAsync(request.Code.Trim());
            if (ticket == null)
                return CommandResult<TicketResponse>.Fail(ErrorCode.InvalidValue, "Билет не найден");

            if (ticket.EventId != request.EventId)
            {
                return CommandResult<TicketResponse>.Fail(ErrorCode.InvalidValue,
                    "Билет относится к другому мероприятию");
            }

            return new CommandResult<TicketResponse>(_mapper.Map<TicketResponse>(ticket));
        }

        private async Task FulfillPaidOrderAsync(Guid orderId, Guid buyerAccountId, Guid eventId, int quantity)
        {
            var existing = await _ordersRepository.GetOrderAsync(orderId);
            if (existing == null)
                return;

            if (existing.Status != OrderStatus.Paid)
                await _ordersRepository.SetOrderPaidAsync(orderId, DateTimeOffset.UtcNow);

            var tickets = await _ordersRepository.GetTicketsByOrderAsync(orderId);
            if (tickets == null || tickets.Count == 0)
            {
                var toCreate = new List<Ticket>();
                for (var i = 0; i < quantity; i++)
                {
                    toCreate.Add(new Ticket
                    {
                        Id = Guid.NewGuid(),
                        OrderId = orderId,
                        EventId = eventId,
                        HolderAccountId = buyerAccountId,
                        Status = TicketStatus.Issued,
                        Code = GenerateTicketCode(),
                        IssuedAt = DateTimeOffset.UtcNow
                    });
                }

                await _ordersRepository.CreateTicketsAsync(toCreate);
            }

            if (!await _participationsRepository.IsUserParticipatedAsync(buyerAccountId, eventId))
            {
                await _participationsRepository.ParticipateAsync(buyerAccountId, eventId);

                var invitations = await _invitationsRepository.SearchInvitationsAsync(new InvitationsSearchRequest
                {
                    InvitedAccountIds = new List<Guid> { buyerAccountId },
                    EventIds = new List<Guid> { eventId }
                });
                if (invitations.Result?.Any() == true)
                {
                    foreach (var invitation in invitations.Result)
                        await _invitationsRepository.DeleteInvitationAsync(eventId, buyerAccountId);
                }

                await _notificationsService.NotifyParticipatedAsync(eventId, buyerAccountId);
            }
        }

        private async Task<CommandResult?> AssertCanBuyTicketAsync(Event eventItem)
        {
            var accountId = _accountDataHolder.AccountId!.Value;

            var participateBan = await _moderationPenaltiesService.AssertNotRestrictedAsync(
                accountId, ModerationPenaltyType.BanEventParticipate);
            if (!participateBan.Success)
                return participateBan;

            var eventBan = await _moderationPenaltiesService.AssertNotRestrictedAsync(
                accountId, ModerationPenaltyType.BanFromEvent, eventItem.Id);
            if (!eventBan.Success)
                return eventBan;

            if (eventItem.Parameters?.Private ?? false)
            {
                var whiteListCount = await _participantsBWListRepository.WhiteListPersonsCountAsync(eventItem.Id);
                if (whiteListCount == 0)
                {
                    var isUserInvited = await _invitationsRepository.IsUserInvitatedAsync(accountId, eventItem.Id);
                    if (!isUserInvited)
                    {
                        return CommandResult.Fail(ErrorCode.AccessError,
                            "Купить билет на закрытое мероприятие можно только по приглашению");
                    }
                }
                else if (!await _participantsBWListRepository.IsUserInWhiteListAsync(eventItem.Id, accountId))
                {
                    return CommandResult.Fail(ErrorCode.AccessError,
                        "Купить билет на закрытое мероприятие могут только пользователи из белого списка");
                }
            }
            else if (await _participantsBWListRepository.IsUserInBlackListAsync(eventItem.Id, accountId))
            {
                return CommandResult.Fail(ErrorCode.AccessError,
                    "Организатор добавил вас в чёрный список мероприятия");
            }

            return null;
        }

        private async Task<Guid?> ResolveSellerOrganizationIdAsync(Guid eventId)
        {
            var organizators = await _eventOrganizatorsRepository.GetByEventIdAsync(eventId);
            var organizationIds = organizators?
                .Where(i => i.OrganizationId != null)
                .Select(i => i.OrganizationId!.Value)
                .Distinct()
                .ToList() ?? new List<Guid>();

            foreach (var organizationId in organizationIds)
            {
                var organization = await _organizationsRepository.GetOrganizationAsync(organizationId);
                if (organization?.CanSellTickets == true)
                    return organizationId;
            }

            return null;
        }

        private CreateOrderResponse ToCreateResponse(
            Order order,
            string? confirmationUrl,
            string? providerPaymentId,
            bool paidImmediately)
        {
            return new CreateOrderResponse
            {
                Order = _mapper.Map<OrderResponse>(order),
                ConfirmationUrl = confirmationUrl,
                ProviderPaymentId = providerPaymentId ?? order.ProviderPaymentId,
                PaidImmediately = paidImmediately
            };
        }

        private static decimal ToMoney(double? cost)
        {
            if (cost == null || cost <= 0)
                return 0m;
            return Math.Round(Convert.ToDecimal(cost.Value), 2, MidpointRounding.AwayFromZero);
        }

        private static string GenerateTicketCode()
            => $"EL{Guid.NewGuid():N}".ToUpperInvariant();
    }
}
