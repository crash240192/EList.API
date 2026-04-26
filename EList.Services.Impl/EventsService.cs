using AutoMapper;
using EList.Common.CorrelationId;
using EList.Common.Logger;
using EList.Common.Models;
using EList.Common.Support;
using EList.Localization;
using EList.Models.EventOrganizators;
using EList.Models.Events;
using EList.Models.Events.EventMetadata;
using EList.Models.Participation;
using EList.Repositories.Interfaces;
using EList.Services.Interfaces;
using NLog;
using Org.BouncyCastle.Ocsp;
using System.Diagnostics;

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

        public EventsService(ICorrelationIdProvider correlationIdProvider,
            IEventsMetadataRepository eventsMetadataRepository,
            IEventsRepository eventsRepository,
            IEventOrganizatorsRepository eventOrganizatorsRepository,
            IAuthorizationRepository authorizationRepository,
            IMapper mapper,
            IInvitationsRepository invitationsRepository,
            IParticipationsRepository participationsRepository,
            IAccountDataHolder accountDataHolder)
        {
            _correlationIdProvider = correlationIdProvider ?? throw new ArgumentNullException(nameof(correlationIdProvider));
            _eventsMetadataRepository = eventsMetadataRepository ?? throw new ArgumentNullException(nameof(eventsMetadataRepository));
            _eventsRepository = eventsRepository ?? throw new ArgumentNullException(nameof(eventsRepository));
            _eventOrganizatorsRepository = eventOrganizatorsRepository ?? throw new ArgumentNullException(nameof(eventOrganizatorsRepository));
            _authorizationRepository = authorizationRepository ?? throw new Exception(nameof(authorizationRepository));
            _invitationsRepository = invitationsRepository ?? throw new Exception(nameof(invitationsRepository));
            _participationsRepository = participationsRepository ?? throw new Exception(nameof(participationsRepository));
            _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
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

            //TODO: Добавить сюда проверку на наличие события

            var result = await _eventsMetadataRepository.GetEventTypesByEventIdAsync(eventId);
            result.ForEach(i => i.Name = Localizator.GetProperty(i.LocalizationPath, i.Name));

            logger.Debug(correlationId, null, methodName, $"Method finished", null, execTime.Elapsed);
            return new CommandResult<List<EventType>>(result);
        }

        public async Task<CommandResult<List<EventType>?>> GetEventTypesByCategoryIdAsync(Guid categoryId)
        {
            var correlationId = _correlationIdProvider.Get();
            var execTime = Stopwatch.StartNew();
            var methodName = $"{LOGGER_NAME}{nameof(GetEventTypesByCategoryIdAsync)}";
            logger.Debug(correlationId, null, methodName, $"Method started", null);

            //TODO: Добавить сюда проверку на наличие события

            var result = await _eventsMetadataRepository.GetEventTypesByCategoryIdAsync(categoryId);
            result.ForEach(i => i.Name = Localizator.GetProperty(i.LocalizationPath, i.Name));

            logger.Debug(correlationId, null, methodName, $"Method finished", null, execTime.Elapsed);
            return new CommandResult<List<EventType>>(result);
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

            //TODO: Добавить сюда проверку на наличие события

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

        #region events
        public async Task<CommandResult<Guid?>> CreateEventAsync(CreateEventRequest request)
        {
            var correlationId = _correlationIdProvider.Get();
            var execTime = Stopwatch.StartNew();
            var methodName = $"{LOGGER_NAME}{nameof(CreateEventAsync)}";
            logger.Debug(correlationId, null, methodName, $"Method started", null);

            //TODO: Сюда нужно поместить проверку на то что текущий пользователь может создавать событие с указанными параметрами и от имени указанных организаторов

            var eventId = await _eventsRepository.CreateEventAsync(request.Event);

            if (request.EventParameters != null)
            {
                var createEventParametersResult = await SetEventParametersAsync(eventId, request.EventParameters);

                if (!createEventParametersResult.Success)
                    return CommandResult<Guid?>.Fail(createEventParametersResult.ErrorCode, createEventParametersResult.Message);
            }

            await _eventsMetadataRepository.BindEventTypesAsync(eventId, request.EventTypes);

            //TODO: присрать сюда accountId текущего пользователя

            //var account = await _authorizationRepository.GetAuthorizationDataAsync(_accountDataHolder.Token);

            #region Привязываем идентификаторы организаторов к событию
            if (request.OrganizatorAccountIds == null)
                request.OrganizatorOrganizationIds = new List<Guid>();

            if (!request.OrganizatorAccountIds.Contains(_accountDataHolder.AccountId))
                request.OrganizatorAccountIds.Add(_accountDataHolder.AccountId);

            foreach (var accountId in request.OrganizatorAccountIds)
            {
                await _eventOrganizatorsRepository.CreateAsync(new EventOrganizatorRequest
                {
                    AccountId = accountId,
                    EventId = eventId,
                });
            }
            #endregion

            //TODO: С организациями разберёмся позже 

            //#region привязываем идентификаторы организаций к событию
            //if (request.OrganizatorOrganizationIds?.Count > 0)
            //{
            //    foreach (var organizationId in request.OrganizatorOrganizationIds)
            //    {
            //        //TODO: Сделать проверку, что пользователь имеет отношение к указанной организации
            //        await _eventOrganizatorsRepository.CreateAsync(new EventOrganizatorRequest
            //        {
            //            OrganizationId = organizationId,
            //            EventId = eventId,
            //        });
            //    }
            //}
            //#endregion

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

            //var accountInfo = await _authorizationRepository.GetAuthorizationDataAsync(token);

            var eventOrganizators = await _eventOrganizatorsRepository.GetByEventIdAsync(eventId);

            if (!eventOrganizators?.Any(i => i.Account?.Id == _accountDataHolder.AccountId) ?? false)
                return CommandResult.Fail(ErrorCode.AccessError, $"Указанный пользователь не является организатором события с id='{eventId}' ");

            //TODO: Если у ивента организатором является какая-то компания, проверить, является ли accountId её участником

            await _eventsRepository.UpdateEventAsync(eventId, request);

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

            var eventOrganizators = await _eventOrganizatorsRepository.GetByEventIdAsync(eventId);

            if (!eventOrganizators?.Any(i => i.Account?.Id == _accountDataHolder.AccountId) ?? false)
                return CommandResult.Fail(ErrorCode.AccessError, $"Указанный пользователь не является организатором события с id='{eventId}' ");

            //TODO: Если у ивента организатором является какая-то компания, проверить, является ли accountId её участником

            await _eventsRepository.SetEventCoverImageAsync(eventId, imageId);

            logger.Debug(correlationId, null, methodName, $"Method finished", null, execTime.Elapsed);
            return new CommandResult<Guid?>(eventId);
        }

        public async Task<CommandResult<Event>> GetEventAsync(Guid id)
        {
            var correlationId = _correlationIdProvider.Get();
            var execTime = Stopwatch.StartNew();
            var methodName = $"{LOGGER_NAME}{nameof(CreateEventAsync)}";
            logger.Debug(correlationId, null, methodName, $"Method started", null);

            //TODO: Добавить проверку на то что пользователю вообще доступно это событие

            var eventItem = await _eventsRepository.GetEventAsync(id);

            if (eventItem == null)
                return CommandResult<Event>.Fail(ErrorCode.EventNotFound, $"Событие с id='{id}' не найдено");

            if (eventItem.Parameters?.Private == true)
            {
                var organizators = await _eventOrganizatorsRepository.GetByEventIdAsync(id);
                if (!organizators?.Any(i => i.Account?.Id == _accountDataHolder.AccountId) ?? true)
                {
                    var participants = await _participationsRepository.GetEventParticipantsAsync(new EventParticipantsSearchRequest { EventId = id });
                    if (!participants.Result?.Any(p => p.Account.Id == _accountDataHolder.AccountId) ?? true)
                    {
                        var invitation = await _invitationsRepository.GetInvitationAsync(_accountDataHolder.AccountId, id);
                        if (invitation == null)
                            return CommandResult<Event>.Fail(ErrorCode.InvitationNotFound, "Посещать закрытые мероприятия можно только приглашению");
                    }
                }
            }

            logger.Debug(correlationId, null, methodName, $"Method finished", null, execTime.Elapsed);
            return new CommandResult<Event>(eventItem);
        }

        public async Task<CommandResult<PagedList<Event>>> SearchEventsAsync(EventsSearchRequest request)
        {
            var correlationId = _correlationIdProvider.Get();
            var execTime = Stopwatch.StartNew();
            var methodName = $"{LOGGER_NAME}{nameof(SearchEventsAsync)}";
            logger.Debug(correlationId, null, methodName, $"Method started", null);

            var searchResult = await _eventsRepository.SearchEventsAsync(request, _accountDataHolder.AccountId);

            logger.Debug(correlationId, null, methodName, $"Method finished", null, execTime.Elapsed);
            return new CommandResult<PagedList<Event>>(searchResult);
        }
        #endregion
    }
}
