using System.Diagnostics;
using AutoMapper;
using EList.Common.Configuration;
using EList.Common.CorrelationId;
using EList.Common.Logger;
using EList.Common.Models;
using EList.Common.Support;
using EList.Localization;
using EList.Models.Enums;
using EList.Models.EventOrganizators;
using EList.Models.Events;
using EList.Models.Events.EventMetadata;
using EList.Models.Participation;
using EList.Repositories.Interfaces;
using EList.Services.Interfaces;
using NLog;
using Org.BouncyCastle.Ocsp;

namespace EList.Services.Impl
{
    public class EventsService : IEventsService
    {
        #region logger
        private static readonly ILogger log = LogManager.GetCurrentClassLogger();
        private static readonly ILoggerWrapper logger = new NLogLoggerWrapper(log);
        private const string LOGGER_NAME = "EList.Services.Impl.EventsService.";
        #endregion

        private readonly IEventsMetadataRepository _eventsMetadataRepository;
        private readonly IEventsRepository _eventsRepository;
        private readonly ICorrelationIdProvider _correlationIdProvider;
        private readonly IEventOrganizatorsRepository _eventOrganizatorsRepository;
        private readonly IAuthorizationRepository _authorizationRepository;
        private readonly IMapper _mapper;
        private readonly IAccountDataHolder _accountDataHolder;
        private readonly IInvitationsRepository _invitationsRepository;
        private readonly IParticipationsRepository _participationsRepository;
        private readonly IWalletsRepository _walletsRepository;
        private readonly IParticipantsBWListRepository _participantsBWListRepository;
        private readonly INotificationsService _notificationsService;
        private readonly ISubscriptionsRepository _subscriptionsRepository;
        private readonly IOrganizationsRepository _organizationsRepository;
        private readonly IModerationPenaltiesService _moderationPenaltiesService;

        public EventsService(ICorrelationIdProvider correlationIdProvider,
            IEventsMetadataRepository eventsMetadataRepository,
            IEventsRepository eventsRepository,
            IEventOrganizatorsRepository eventOrganizatorsRepository,
            IAuthorizationRepository authorizationRepository,
            IMapper mapper,
            IInvitationsRepository invitationsRepository,
            IParticipationsRepository participationsRepository,
            IWalletsRepository walletsRepository,
            IAccountDataHolder accountDataHolder,
            IParticipantsBWListRepository participantsBWListRepository,
            INotificationsService notificationsService,
            ISubscriptionsRepository subscriptionsRepository,
            IOrganizationsRepository organizationsRepository,
            IModerationPenaltiesService moderationPenaltiesService)
        {
            _correlationIdProvider = correlationIdProvider ?? throw new ArgumentNullException(nameof(correlationIdProvider));
            _eventsMetadataRepository = eventsMetadataRepository ?? throw new ArgumentNullException(nameof(eventsMetadataRepository));
            _eventsRepository = eventsRepository ?? throw new ArgumentNullException(nameof(eventsRepository));
            _eventOrganizatorsRepository = eventOrganizatorsRepository ?? throw new ArgumentNullException(nameof(eventOrganizatorsRepository));
            _authorizationRepository = authorizationRepository ?? throw new Exception(nameof(authorizationRepository));
            _invitationsRepository = invitationsRepository ?? throw new Exception(nameof(invitationsRepository));
            _subscriptionsRepository = subscriptionsRepository ?? throw new Exception(nameof(subscriptionsRepository));
            _participationsRepository = participationsRepository ?? throw new Exception(nameof(participationsRepository));
            _participantsBWListRepository = participantsBWListRepository ?? throw new Exception(nameof(participantsBWListRepository));
            _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
            _walletsRepository = walletsRepository ?? throw new Exception(nameof(walletsRepository));
            _notificationsService = notificationsService ?? throw new Exception(nameof(notificationsService));
            _organizationsRepository = organizationsRepository ?? throw new ArgumentNullException(nameof(organizationsRepository));
            _moderationPenaltiesService = moderationPenaltiesService ?? throw new ArgumentNullException(nameof(moderationPenaltiesService));
            _accountDataHolder = accountDataHolder;
        }



        #region eventTypes
        public async Task<CommandResult<Guid?>> CreateEventTypeAsync(EventTypeRequest request)
        {
            var correlationId = _correlationIdProvider.Get();
            var execTime = Stopwatch.StartNew();
            var methodName = $"{LOGGER_NAME}{nameof(CreateEventTypeAsync)}";
            logger.Debug(correlationId, null, methodName, $"Method started", null);

            var eventCategory = await _eventsMetadataRepository.GetEventCategoryAsync(request.EventCategoryId);

            if (eventCategory == null)
                return CommandResult<Guid?>.Fail(ErrorCode.EventCategoryNotFound, "Категория события не найдена");

            var result = await _eventsMetadataRepository.CreateEventTypeAsync(request);

            logger.Debug(correlationId, null, methodName, $"Method finished", null, execTime.Elapsed);
            return new CommandResult<Guid?>(result);
        }

        public async Task<CommandResult> DeleteEventTypeAsync(Guid id)
        {
            var correlationId = _correlationIdProvider.Get();
            var execTime = Stopwatch.StartNew();
            var methodName = $"{LOGGER_NAME}{nameof(DeleteEventTypeAsync)}";
            logger.Debug(correlationId, null, methodName, $"Method started", null);

            var eventType = await _eventsMetadataRepository.GetEventTypeAsync(id);

            if (eventType == null)
                return CommandResult<Guid>.Fail(ErrorCode.EventTypeNotFound, "Тип события не найден");

            await _eventsMetadataRepository.DeleteEventTypeAsync(id);

            logger.Debug(correlationId, null, methodName, $"Method finished", null, execTime.Elapsed);
            return CommandResult.OK;
        }

        public async Task<CommandResult<List<EventType>>> GetAllEventTypesAsync()
        {
            var correlationId = _correlationIdProvider.Get();
            var execTime = Stopwatch.StartNew();
            var methodName = $"{LOGGER_NAME}{nameof(CreateEventTypeAsync)}";
            logger.Debug(correlationId, null, methodName, $"Method started", null);

            var result = await _eventsMetadataRepository.GetAllEventTypesAsync();

            result.ForEach(i => i.Name = Localizator.GetProperty(i.LocalizationPath, i.Name));

            logger.Debug(correlationId, null, methodName, $"Method finished", null, execTime.Elapsed);
            return new CommandResult<List<EventType>>(result);
        }

        public async Task<CommandResult<EventType?>> GetEventTypeAsync(Guid id)
        {
            var correlationId = _correlationIdProvider.Get();
            var execTime = Stopwatch.StartNew();
            var methodName = $"{LOGGER_NAME}{nameof(GetEventTypeAsync)}";
            logger.Debug(correlationId, null, methodName, $"Method started", null);

            var result = await _eventsMetadataRepository.GetEventTypeAsync(id);

            if (result == null)
                return CommandResult<EventType?>.Fail(ErrorCode.EventTypeNotFound, "Тип события не найден");

            result.Name = Localizator.GetProperty(result.LocalizationPath, result.Name);

            logger.Debug(correlationId, null, methodName, $"Method finished", null, execTime.Elapsed);
            return new CommandResult<EventType?>(result);
        }

        public async Task<CommandResult<List<EventType>?>> GetEventTypesByEventIdAsync(Guid eventId)
        {
            var correlationId = _correlationIdProvider.Get();
            var execTime = Stopwatch.StartNew();
            var methodName = $"{LOGGER_NAME}{nameof(GetEventTypesByEventIdAsync)}";
            logger.Debug(correlationId, null, methodName, $"Method started", null);

            var curEvent = await _eventsRepository.GetEventAsync(eventId);
            if (curEvent == null)
                return CommandResult<List<EventType>?>.Fail(ErrorCode.EventNotFound, "Событие не найдено");

            var result = await _eventsMetadataRepository.GetEventTypesByEventIdAsync(eventId);
            result?.ForEach(i => i.Name = Localizator.GetProperty(i.LocalizationPath, i.Name));

            logger.Debug(correlationId, null, methodName, $"Method finished", null, execTime.Elapsed);
            return new CommandResult<List<EventType>?>(result);
        }

        public async Task<CommandResult<List<EventType>?>> GetEventTypesByCategoryIdAsync(Guid categoryId)
        {
            var correlationId = _correlationIdProvider.Get();
            var execTime = Stopwatch.StartNew();
            var methodName = $"{LOGGER_NAME}{nameof(GetEventTypesByCategoryIdAsync)}";
            logger.Debug(correlationId, null, methodName, $"Method started", null);

            var category = await _eventsMetadataRepository.GetEventCategoryAsync(categoryId);
            if (category == null)
                return CommandResult<List<EventType>?>.Fail(ErrorCode.EventNotFound, "Событие не найдено");
            var result = await _eventsMetadataRepository.GetEventTypesByCategoryIdAsync(categoryId);
            result?.ForEach(i => i.Name = Localizator.GetProperty(i.LocalizationPath, i.Name));

            logger.Debug(correlationId, null, methodName, $"Method finished", null, execTime.Elapsed);
            return new CommandResult<List<EventType>?>(result);
        }

        public async Task<CommandResult> UpdateEventTypeAsync(Guid id, EventTypeRequest request)
        {
            var correlationId = _correlationIdProvider.Get();
            var execTime = Stopwatch.StartNew();
            var methodName = $"{LOGGER_NAME}{nameof(UpdateEventTypeAsync)}";
            logger.Debug(correlationId, null, methodName, $"Method started", null);

            await _eventsMetadataRepository.UpdateEventTypeAsync(id, request);

            logger.Debug(correlationId, null, methodName, $"Method finished", null, execTime.Elapsed);
            return CommandResult.OK;
        }

        public async Task<CommandResult> SetEventTypesAsync(Guid eventId, List<Guid> typeIds)
        {
            var correlationId = _correlationIdProvider.Get();
            var execTime = Stopwatch.StartNew();
            var methodName = $"{LOGGER_NAME}{nameof(SetEventTypesAsync)}";
            logger.Debug(correlationId, null, methodName, $"Method started", null);

            var eventItem = await _eventsRepository.GetEventAsync(eventId);

            if (eventItem == null)
                return CommandResult.Fail(ErrorCode.EventNotFound, $"Событие с id='{eventId}' не найдено");

            await _eventsMetadataRepository.BindEventTypesAsync(eventId, typeIds);

            logger.Debug(correlationId, null, methodName, $"Method finished", null, execTime.Elapsed);
            return new CommandResult<Guid?>(eventId);
        }
        #endregion

        #region eventCategories
        public async Task<CommandResult<Guid?>> CreateEventCategoryAsync(EventCategoryRequest request)
        {
            var correlationId = _correlationIdProvider.Get();
            var execTime = Stopwatch.StartNew();
            var methodName = $"{LOGGER_NAME}{nameof(CreateEventCategoryAsync)}";
            logger.Debug(correlationId, null, methodName, $"Method started", null);

            var result = await _eventsMetadataRepository.CreateEventCategoryAsync(request);

            logger.Debug(correlationId, null, methodName, $"Method finished", null, execTime.Elapsed);
            return new CommandResult<Guid?>(result);
        }

        public async Task<CommandResult> DeleteEventCategoryAsync(Guid id)
        {
            var correlationId = _correlationIdProvider.Get();
            var execTime = Stopwatch.StartNew();
            var methodName = $"{LOGGER_NAME}{nameof(DeleteEventCategoryAsync)}";
            logger.Debug(correlationId, null, methodName, $"Method started", null);

            var eventCategory = await _eventsMetadataRepository.GetEventCategoryAsync(id);

            if (eventCategory == null)
                return CommandResult<Guid>.Fail(ErrorCode.EventCategoryNotFound, "Категория события не найдена");

            await _eventsMetadataRepository.DeleteEventCategoryAsync(id);

            logger.Debug(correlationId, null, methodName, $"Method finished", null, execTime.Elapsed);
            return CommandResult.OK;
        }

        public async Task<CommandResult<List<EventCategory>>> GetAllEventCategoriesAsync()
        {
            var correlationId = _correlationIdProvider.Get();
            var execTime = Stopwatch.StartNew();
            var methodName = $"{LOGGER_NAME}{nameof(CreateEventTypeAsync)}";
            logger.Debug(correlationId, null, methodName, $"Method started", null);

            var result = await _eventsMetadataRepository.GetAllEventCategoriesAsync();

            result.ForEach(i => i.Name = Localizator.GetProperty(i.LocalizationPath, i.Name));

            logger.Debug(correlationId, null, methodName, $"Method finished", null, execTime.Elapsed);
            return new CommandResult<List<EventCategory>>(result);
        }

        public async Task<CommandResult<EventCategory?>> GetEventCategoryAsync(Guid id)
        {
            var correlationId = _correlationIdProvider.Get();
            var execTime = Stopwatch.StartNew();
            var methodName = $"{LOGGER_NAME}{nameof(GetEventCategoryAsync)}";
            logger.Debug(correlationId, null, methodName, $"Method started", null);

            var result = await _eventsMetadataRepository.GetEventCategoryAsync(id);

            if (result == null)
                return CommandResult<EventCategory?>.Fail(ErrorCode.EventCategoryNotFound, "Категория события не найдена");

            result.Name = Localizator.GetProperty(result.LocalizationPath, result.Name);

            logger.Debug(correlationId, null, methodName, $"Method finished", null, execTime.Elapsed);
            return new CommandResult<EventCategory?>(result);
        }

        public async Task<CommandResult> UpdateEventCategoryAsync(Guid id, EventCategoryRequest request)
        {
            var correlationId = _correlationIdProvider.Get();
            var execTime = Stopwatch.StartNew();
            var methodName = $"{LOGGER_NAME}{nameof(UpdateEventCategoryAsync)}";
            logger.Debug(correlationId, null, methodName, $"Method started", null);

            await _eventsMetadataRepository.UpdateEventCategoryAsync(id, request);

            logger.Debug(correlationId, null, methodName, $"Method finished", null, execTime.Elapsed);
            return CommandResult.OK;
        }
        #endregion

        #region eventParameters
        //public async Task<CommandResult<Guid?>> CreateEventParametersAsync(EventParametersRequest request)
        //{
        //    var correlationId = _correlationIdProvider.Get();
        //    var execTime = Stopwatch.StartNew();
        //    var methodName = $"{LOGGER_NAME}{nameof(CreateEventParametersAsync)}";
        //    logger.Debug(correlationId, null, methodName, $"Method started", null);

        //    var result = await _eventsMetadataRepository.CreateEventParametersAsync(request);

        //    logger.Debug(correlationId, null, methodName, $"Method finished", null, execTime.Elapsed);
        //    return new CommandResult<Guid?>(result);
        //}

        //public async Task<CommandResult> DeleteEventParametersAsync(Guid id)
        //{
        //    var correlationId = _correlationIdProvider.Get();
        //    var execTime = Stopwatch.StartNew();
        //    var methodName = $"{LOGGER_NAME}{nameof(DeleteEventParametersAsync)}";
        //    logger.Debug(correlationId, null, methodName, $"Method started", null);

        //    await _eventsMetadataRepository.DeleteEventParametersAsync(id);

        //    logger.Debug(correlationId, null, methodName, $"Method finished", null, execTime.Elapsed);
        //    return CommandResult.OK;
        //}

        //public async Task<CommandResult> UpdateEventParametersAsync(Guid id, EventParametersRequest request)
        //{
        //    var correlationId = _correlationIdProvider.Get();
        //    var execTime = Stopwatch.StartNew();
        //    var methodName = $"{LOGGER_NAME}{nameof(UpdateEventParametersAsync)}";
        //    logger.Debug(correlationId, null, methodName, $"Method started", null);

        //    await _eventsMetadataRepository.UpdateEventParametersAsync(id, request);

        //    logger.Debug(correlationId, null, methodName, $"Method finished", null, execTime.Elapsed);
        //    return CommandResult.OK;
        //}

        //public async Task<CommandResult<EventParameters?>> GetEventParametersAsync(Guid id)
        //{
        //    var correlationId = _correlationIdProvider.Get();
        //    var execTime = Stopwatch.StartNew();
        //    var methodName = $"{LOGGER_NAME}{nameof(GetEventParametersAsync)}";
        //    logger.Debug(correlationId, null, methodName, $"Method started", null);

        //    var result = await _eventsMetadataRepository.GetEventParametersAsync(id);

        //    logger.Debug(correlationId, null, methodName, $"Method finished", null, execTime.Elapsed);
        //    return new CommandResult<EventParameters?>(result);
        //}

        public async Task<CommandResult<EventParameters?>> GetEventParametersByEventIdAsync(Guid eventId)
        {
            var correlationId = _correlationIdProvider.Get();
            var execTime = Stopwatch.StartNew();
            var methodName = $"{LOGGER_NAME}{nameof(GetEventParametersByEventIdAsync)}";
            logger.Debug(correlationId, null, methodName, $"Method started", null);

            var eventItem = await _eventsRepository.GetEventAsync(eventId);
            if (eventItem == null)
                return CommandResult<EventParameters?>.Fail(ErrorCode.EventParametersNotFound, $"Событие с id='{eventId}' не найдено");

            var result = await _eventsMetadataRepository.GetEventParametersByEventIdAsync(eventId);
            if (result == null)
                return CommandResult<EventParameters?>.Fail(ErrorCode.EventParametersNotFound, $"Параметры для события id='{eventId}' не найдены");

            logger.Debug(correlationId, null, methodName, $"Method finished", null, execTime.Elapsed);
            return new CommandResult<EventParameters?>(result);
        }

        public async Task<CommandResult> SetEventParametersAsync(Guid eventId, EventParametersRequest parameters)
        {
            var correlationId = _correlationIdProvider.Get();
            var execTime = Stopwatch.StartNew();
            var methodName = $"{LOGGER_NAME}{nameof(SetEventParametersAsync)}";
            logger.Debug(correlationId, null, methodName, $"Method started", null);

            // TODO: Провести валидацию на доступность выставления параметров (для премиум аккаунтов)

            var curEvent = await _eventsRepository.GetEventAsync(eventId);
            if (curEvent == null)
                return CommandResult.Fail(ErrorCode.EventNotFound, $"Событие с id='{eventId}' не найдено");

            if (_accountDataHolder.AccountId == null
                || !await _eventOrganizatorsRepository.IsAccountEventOrganizatorAsync(eventId, _accountDataHolder.AccountId.Value))
                return CommandResult.Fail(ErrorCode.AccessError, $"Указанный пользователь не является организатором события с id='{eventId}' ");

            var ticketSalesError = await EnsureTicketSalesAllowedAsync(eventId, parameters.TicketsEnabled);
            if (ticketSalesError != null)
                return ticketSalesError;

            //if (!Enum.IsDefined(typeof(AgeRating), parameters.AgeLimit))
            //    return CommandResult.Fail(ErrorCode.InvalidAgeLimitValue, "Значение возрастного ограничения может принимать значения '0', '6', '12', '16' или '18'");

            if (curEvent.EventParametersId == null)
            {
                var parametersId = await _eventsMetadataRepository.CreateEventParametersAsync(parameters);
                await _eventsMetadataRepository.BindEventParametersAsync(curEvent.Id, parametersId);
            }
            else
            {
                await _eventsMetadataRepository.UpdateEventParametersAsync(curEvent.EventParametersId.Value, parameters);
            }

            logger.Debug(correlationId, null, methodName, $"Method finished", null, execTime.Elapsed);
            return CommandResult.OK;
        }
        #endregion

        private async Task<CommandResult?> EnsureTicketSalesAllowedAsync(Guid eventId, bool ticketsEnabled)
        {
            if (!ticketsEnabled)
                return null;

            var organizators = await _eventOrganizatorsRepository.GetByEventIdAsync(eventId);
            var organizationIds = organizators?
                .Where(i => i.OrganizationId != null)
                .Select(i => i.OrganizationId!.Value)
                .Distinct()
                .ToList() ?? new List<Guid>();

            if (organizationIds.Count == 0)
            {
                return CommandResult.Fail(ErrorCode.InvalidValue,
                    "Продажу билетов можно включить только для мероприятия с организацией-организатором");
            }

            foreach (var organizationId in organizationIds)
            {
                var organization = await _organizationsRepository.GetOrganizationAsync(organizationId);
                if (organization?.CanSellTickets == true)
                    return null;
            }

            return CommandResult.Fail(ErrorCode.OrganizationNotVerified,
                "Ни одна организация-организатор не имеет права продавать билеты");
        }

        #region events
        public async Task<CommandResult<Guid?>> CreateEventAsync(CreateEventRequest request)
        {
            var correlationId = _correlationIdProvider.Get();
            var execTime = Stopwatch.StartNew();
            var methodName = $"{LOGGER_NAME}{nameof(CreateEventAsync)}";
            logger.Debug(correlationId, null, methodName, $"Method started", null);

            #region validation
            if (request.EventParameters == null)
                request.EventParameters = new EventParametersRequest
                {
                    AgeLimit = 0,
                    Cost = 0,
                    Private = false,
                    AllowUsersToInvite = true
                };

            request.OrganizatorAccountIds ??= new List<Guid>();
            request.OrganizatorOrganizationIds ??= new List<Guid>();

            // Для мероприятия от организации берём тариф кошелька организации, иначе — личного аккаунта
            Models.Wallets.TariffValidator? tariffValidator;
            if (request.OrganizatorOrganizationIds.Count > 0)
            {
                // Основная организация — первая в списке (обычно передаётся одна)
                tariffValidator = await _walletsRepository.GetOrganizationTariffValidatorAsync(request.OrganizatorOrganizationIds[0]);
            }
            else
            {
                tariffValidator = await _walletsRepository.GetAccountTariffValidatorAsync(_accountDataHolder.AccountId.Value);
            }

            var ageThreshold = tariffValidator != null
                ? tariffValidator.AgeLimit : 0;

            if (ageThreshold != null)
                if (request.EventParameters.AgeLimit > ageThreshold)
                    return CommandResult<Guid?>.Fail(ErrorCode.InvalidAgeLimitValue, $"В рамках текущего тарифа доступно создание мероприятий только мероприятия c ограничением по возрассту до {ageThreshold} лет");

            if (request.EventParameters.AgeLimit >= 18 && !_accountDataHolder.AdultConfirmed)
                return CommandResult<Guid?>.Fail(ErrorCode.InvalidAgeLimitValue, $"Несовершеннолетние пользователи не могут создавать мероприятия 18+");

            if (request.EventParameters.Cost > 0 && !_accountDataHolder.AdultConfirmed)
                return CommandResult<Guid?>.Fail(ErrorCode.InvalidAgeLimitValue, $"Несовершеннолетние пользователи не могут создавать платные мероприятия");

            var createBan = await _moderationPenaltiesService.AssertNotRestrictedAsync(
                _accountDataHolder.AccountId.Value, ModerationPenaltyType.BanEventCreate);
            if (!createBan.Success)
                return CommandResult<Guid?>.Fail(createBan.ErrorCode, createBan.Message);

            var organizeBan = await _moderationPenaltiesService.AssertNotRestrictedAsync(
                _accountDataHolder.AccountId.Value, ModerationPenaltyType.BanOrganize);
            if (!organizeBan.Success)
                return CommandResult<Guid?>.Fail(organizeBan.ErrorCode, organizeBan.Message);
            #endregion

            var eventId = await _eventsRepository.CreateEventAsync(request.Event);
            
            #region Привязываем идентификаторы организаторов к событию
            // Личный аккаунт добавляем автоматически только если организация-организатор не указана
            if (request.OrganizatorOrganizationIds.Count == 0
                && !request.OrganizatorAccountIds.Contains(_accountDataHolder.AccountId.Value))
            {
                request.OrganizatorAccountIds.Add(_accountDataHolder.AccountId.Value);
            }

            foreach (var organizationId in request.OrganizatorOrganizationIds)
            {
                var isMember = await _organizationsRepository.IsActiveMemberAsync(organizationId, _accountDataHolder.AccountId.Value);
                if (!isMember)
                    return CommandResult<Guid?>.Fail(ErrorCode.AccessError,
                        $"Вы не являетесь участником организации '{organizationId}' и не можете указать её организатором");

                var organization = await _organizationsRepository.GetOrganizationAsync(organizationId);
                if (organization == null || !organization.Active)
                    return CommandResult<Guid?>.Fail(ErrorCode.AccessError,
                        "Организация приостановлена и не может создавать мероприятия");

                await _moderationPenaltiesService.LiftExpiredForOrganizationAsync(organizationId);
                var orgPenalty = await _moderationPenaltiesService.GetActiveForOrganizationAsync(organizationId);
                if (orgPenalty.Any(p => p.PenaltyType == ModerationPenaltyType.SuspendOrganization))
                    return CommandResult<Guid?>.Fail(ErrorCode.ModerationPenaltyActive,
                        "Организация приостановлена модерацией и не может создавать мероприятия");
            }

            foreach (var accountId in request.OrganizatorAccountIds)
            {
                var coOrganizerBan = await _moderationPenaltiesService.AssertNotRestrictedAsync(
                    accountId, ModerationPenaltyType.BanOrganize);
                if (!coOrganizerBan.Success)
                    return CommandResult<Guid?>.Fail(coOrganizerBan.ErrorCode, coOrganizerBan.Message);
            }

            foreach (var accountId in request.OrganizatorAccountIds)
            {
                await _eventOrganizatorsRepository.CreateAsync(new EventOrganizatorRequest
                {
                    AccountId = accountId,
                    EventId = eventId,
                });
            }

            foreach (var organizationId in request.OrganizatorOrganizationIds)
            {
                await _eventOrganizatorsRepository.CreateAsync(new EventOrganizatorRequest
                {
                    OrganizationId = organizationId,
                    EventId = eventId,
                });
            }
            #endregion

            var createEventParametersResult = await SetEventParametersAsync(eventId, request.EventParameters);

            if (!createEventParametersResult.Success)
                return CommandResult<Guid?>.Fail(createEventParametersResult.ErrorCode, createEventParametersResult.Message);

            await _eventsMetadataRepository.BindEventTypesAsync(eventId, request.EventTypes);

            #region Автоматическое заполнение белого списка для частного мероприятия
            var isPrivate = request.EventParameters?.Private == true;
            if (isPrivate && (request.WhiteList?.Any() ?? false))
            {
                await _participantsBWListRepository.AddToWhiteListAsync(new AddUsersToBWListRequest
                {
                    AccountIds = request.WhiteList,
                    EventId = eventId
                });
            }
            #endregion

            #region Автоматическое заполнение черного списка
            if (!isPrivate && (request.BlackList?.Any() ?? false))
            {
                await _participantsBWListRepository.AddToBlackListAsync(new AddUsersToBWListRequest
                {
                    AccountIds = request.BlackList,
                    EventId = eventId
                });
            }
            #endregion

            #region автоматическая рассылка приглашений
            var usersToInvite = new List<Guid>();
            var subscribersList = new List<Guid>();
            if (isPrivate)
            {
                if (request.InviteAllSubscribers)
                {
                    usersToInvite = request.WhiteList;
                }
                else
                {
                    usersToInvite = request.InviteUsers;

                    if (request.WhiteList?.Any() ?? false)
                        usersToInvite = usersToInvite?.Where(i => request.WhiteList?.Contains(i) ?? false)?.ToList();
                }
            }
            else
            {
                subscribersList = await _subscriptionsRepository.GetSubscribersIdsAsync(new Models.Subscriptions.SubscriptionsSearchRequest
                {
                    AccountId = _accountDataHolder.AccountId.Value,
                    NotifyEventCreated = true,
                });

                if (request.InviteAllSubscribers)
                    usersToInvite = subscribersList;
                else
                    usersToInvite = request.InviteUsers;

                usersToInvite = usersToInvite?.Where(i => !request.BlackList?.Contains(i) ?? true).ToList();
                subscribersList = subscribersList?.Where(i => !usersToInvite?.Contains(i) ?? true)?.ToList();
            }

            if (usersToInvite?.Any() ?? false)
            {
                await _invitationsRepository.CreateInvitationsAsync(new Models.Invitations.CreateInvitationsRequest
                {
                    AccountIds = usersToInvite,
                    EventId = eventId
                }, _accountDataHolder.AccountId.Value);
                await _notificationsService.NotifyUsersInvitedAsync(eventId, usersToInvite);
            }
            #endregion

            if (!isPrivate)
                await _notificationsService.NotifyEventCreatedAsync(eventId, subscribersList);

            logger.Debug(correlationId, null, methodName, $"Method finished", null, execTime.Elapsed);
            return new CommandResult<Guid?>(eventId);
        }

        public async Task<CommandResult> UpdateEventAsync(Guid eventId, EventRequest request)
        {
            var correlationId = _correlationIdProvider.Get();
            var execTime = Stopwatch.StartNew();
            var methodName = $"{LOGGER_NAME}{nameof(CreateEventAsync)}";
            logger.Debug(correlationId, null, methodName, $"Method started", null);

            var eventItem = await _eventsRepository.GetEventAsync(eventId);
            if (eventItem == null)
                return CommandResult.Fail(ErrorCode.EventNotFound, $"Событие с id='{eventId}' не найдено");

            if (_accountDataHolder.AccountId == null
                || !await _eventOrganizatorsRepository.IsAccountEventOrganizatorAsync(eventId, _accountDataHolder.AccountId.Value))
                return CommandResult.Fail(ErrorCode.AccessError, $"Указанный пользователь не является организатором события с id='{eventId}' ");

            await _eventsRepository.UpdateEventAsync(eventId, request);

            await _notificationsService.NotifyEventUpdatedAsync(eventId);

            logger.Debug(correlationId, null, methodName, $"Method finished", null, execTime.Elapsed);
            return new CommandResult<Guid?>(eventId);
        }

        public async Task<CommandResult> SetEventCoverImageAsync(Guid eventId, Guid? imageId)
        {
            var correlationId = _correlationIdProvider.Get();
            var execTime = Stopwatch.StartNew();
            var methodName = $"{LOGGER_NAME}{nameof(SetEventCoverImageAsync)}";
            logger.Debug(correlationId, null, methodName, $"Method started", null);

            var eventItem = await _eventsRepository.GetEventAsync(eventId);
            if (eventItem == null)
                return CommandResult.Fail(ErrorCode.EventNotFound, $"Событие с id='{eventId}' не найдено");

            //var accountInfo = await _authorizationRepository.GetAuthorizationDataAsync(token);

            if (_accountDataHolder.AccountId == null
                || !await _eventOrganizatorsRepository.IsAccountEventOrganizatorAsync(eventId, _accountDataHolder.AccountId.Value))
                return CommandResult.Fail(ErrorCode.AccessError, $"Указанный пользователь не является организатором события с id='{eventId}' ");

            await _eventsRepository.SetEventCoverImageAsync(eventId, imageId);

            logger.Debug(correlationId, null, methodName, $"Method finished", null, execTime.Elapsed);
            return new CommandResult<Guid?>(eventId);
        }

        public async Task<CommandResult<Event>> GetEventAsync(Guid eventId)
        {
            var correlationId = _correlationIdProvider.Get();
            var execTime = Stopwatch.StartNew();
            var methodName = $"{LOGGER_NAME}{nameof(CreateEventAsync)}";
            logger.Debug(correlationId, null, methodName, $"Method started", null);

            //TODO: Добавить проверку на то что пользователю вообще доступно это событие

            var eventItem = await _eventsRepository.GetEventAsync(eventId);

            if (eventItem == null)
                return CommandResult<Event>.Fail(ErrorCode.EventNotFound, $"Событие с id='{eventId}' не найдено");

            var isOrganizator = _accountDataHolder.AccountId != null
                && await _eventOrganizatorsRepository.IsAccountEventOrganizatorAsync(eventId, _accountDataHolder.AccountId.Value);

            if (!isOrganizator)
            {
                if (eventItem.Parameters?.Private == true)
                {
                    if (_accountDataHolder.AccountId == null)
                        return CommandResult<Event>.Fail(ErrorCode.EventAccessDenied, "Сначала Необходимо авторизоваться");

                    var isUserInWhiteList = await _participantsBWListRepository.IsUserInWhiteListAsync(eventId, _accountDataHolder.AccountId.Value);
                    if (!isUserInWhiteList)
                    {
                        var whiteListIsEmpty = await _participantsBWListRepository.IsWhiteListEmptyAsync(eventId);
                        if (whiteListIsEmpty)
                        {
                            var isUserParticipated = await _participationsRepository.IsUserParticipatedAsync(_accountDataHolder.AccountId.Value, eventId);
                            if (!isUserParticipated)
                            {
                                var invitation = await _invitationsRepository.GetInvitationAsync(_accountDataHolder.AccountId.Value, eventId);
                                if (invitation == null)
                                    return CommandResult<Event>.Fail(ErrorCode.EventAccessDenied, "Посещать закрытые мероприятия можно только приглашению");
                            }
                        }
                        else
                        {
                            return CommandResult<Event>.Fail(ErrorCode.EventAccessDenied, "Посещать закрытые мероприятия можно только приглашению");
                        }
                    }
                }
                else
                {
                    if (_accountDataHolder.AccountId != null)
                    {
                        var isUserInBlackList = await _participantsBWListRepository.IsUserInBlackListAsync(eventId, _accountDataHolder.AccountId.Value);
                        if (isUserInBlackList)
                            return CommandResult<Event>.Fail(ErrorCode.EventAccessDenied, "Организатор добавил вас в чёрный список мероприятия");
                    }
                }
            }

            #region age validation
            if (!isOrganizator)
            {
                if (eventItem.Parameters == null)
                    eventItem.Parameters = new EventParameters
                    {
                        AgeLimit = 0,
                    };
                else
                    eventItem.Parameters.AgeLimit = GetEventMinAllowedAge(eventItem.Parameters?.AgeLimit);

                if (eventItem.Parameters.AgeLimit >= 18 && !_accountDataHolder.AdultConfirmed)
                    return CommandResult<Event>.Fail(ErrorCode.EventAccessDenied, $"Просмотр мероприятий 18+ недоступен");
            }
            #endregion

            logger.Debug(correlationId, null, methodName, $"Method finished", null, execTime.Elapsed);
            return new CommandResult<Event>(eventItem);
        }

        private static int GetEventMinAllowedAge(int? value)
        {
            value = value ?? 0;
            var ageRatingValues = Enum.GetValues<AgeRating>().Cast<int>().ToList();
            var nextAvailableRatingValue = ageRatingValues.FirstOrDefault(x => x >= value, 18);
            return nextAvailableRatingValue;
        }

        //private static bool ValidateAgeAccessToEvent(int? eventAgeLimit, int userAge, bool strongValidation)
        //{
        //    if (userAge >= 18)
        //        return true;

        //    var minAllowedAge = GetEventMinAllowedAge(eventAgeLimit);

        //    if (strongValidation)
        //    {
        //        if (minAllowedAge >= userAge)
        //            return false;
        //        else
        //            return true;
        //    }   
        //    else
        //    {
        //        if (minAllowedAge == 18 && userAge < 18)
        //            return false;
        //        return true;
        //    }   
        //}

        public async Task<CommandResult<PagedList<Event>?>> SearchEventsAsync(EventsSearchRequest request)
        {
            var correlationId = _correlationIdProvider.Get();
            var execTime = Stopwatch.StartNew();
            var methodName = $"{LOGGER_NAME}{nameof(SearchEventsAsync)}";
            logger.Debug(correlationId, null, methodName, $"Method started", null);

            var searchResult = await _eventsRepository.SearchEventsAsync(request, _accountDataHolder.AccountId, _accountDataHolder.AdultConfirmed);

            logger.Debug(correlationId, null, methodName, $"Method finished", null, execTime.Elapsed);
            return new CommandResult<PagedList<Event>?>(searchResult);
        }

        public async Task<CommandResult<PagedList<EventShort>?>> SearchEventsShortAsync(EventsSearchRequest request)
        {
            var correlationId = _correlationIdProvider.Get();
            var execTime = Stopwatch.StartNew();
            var methodName = $"{LOGGER_NAME}{nameof(SearchEventsShortAsync)}";
            logger.Debug(correlationId, null, methodName, $"Method started", null);

            var searchResult = await _eventsRepository.SearchEventsShortAsync(request, _accountDataHolder.AccountId, _accountDataHolder.AdultConfirmed);

            logger.Debug(correlationId, null, methodName, $"Method finished", null, execTime.Elapsed);
            return new CommandResult<PagedList<EventShort>?>(searchResult);
        }

        public async Task<CommandResult> CancelEventAsync(Guid eventId)
        {
            var correlationId = _correlationIdProvider.Get();
            var execTime = Stopwatch.StartNew();
            var methodName = $"{LOGGER_NAME}{nameof(CancelEventAsync)}";
            logger.Debug(correlationId, null, methodName, $"Method started", null);

            var curEvent = await _eventsRepository.GetEventAsync(eventId);
            if (curEvent == null)
                return CommandResult.Fail(ErrorCode.EventNotFound, $"Мероприятие с id='{eventId} не найдено'");

            if (_accountDataHolder.AccountId == null
                || !await _eventOrganizatorsRepository.IsAccountEventOrganizatorAsync(eventId, _accountDataHolder.AccountId.Value))
                return CommandResult.Fail(ErrorCode.AccessError, $"У вас нет доступа к редактированию текущего мероприятия'");

            await _eventsRepository.CancelEventAsync(
                eventId,
                _accountDataHolder.AccountId,
                "organizer",
                null);

            await _invitationsRepository.CancelAllInvitationsAsync(eventId);

            await _notificationsService.NotifyEventCancelledAsync(eventId);

            logger.Debug(correlationId, null, methodName, $"Method finished", null, execTime.Elapsed);
            return CommandResult.OK;
        }
        #endregion
    }
}
