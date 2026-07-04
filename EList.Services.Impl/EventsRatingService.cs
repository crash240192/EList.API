using EList.Common.CorrelationId;
using EList.Common.Logger;
using EList.Common.Models;
using EList.Common.Support;
using EList.Models.Enums;
using EList.Models.EventsRating;
using EList.Repositories.Interfaces;
using EList.Services.Interfaces;
using NetTopologySuite.Index.HPRtree;
using NLog;
using System.Diagnostics;

namespace EList.Services.Impl
{
    public class EventsRatingService : IEventsRatingService
    {
        private readonly ICorrelationIdProvider _correlationIdProvider;
        private readonly IEventsRatingRepository _eventsRatingRepository;
        private readonly IAccountDataHolder _accountDataHolder;
        private readonly IEventsRepository _eventsRepository;
        private readonly INotificationsService _notificationsService;
        private readonly IEventOrganizatorsRepository _eventOrganizatorsRepository;

        #region logger
        private static readonly ILogger log = LogManager.GetCurrentClassLogger();
        private static readonly ILoggerWrapper logger = new NLogLoggerWrapper(log);
        private const string LOGGER_NAME = "EList.Services.Impl.EventsRatingService.";
        #endregion

        public EventsRatingService(
            ICorrelationIdProvider correlationIdProvider,
            IEventsRatingRepository eventsRatingRepository,
            IEventsRepository eventsRepository,
            IAccountDataHolder accountDataHolder,
            INotificationsService notificationsService,
            IEventOrganizatorsRepository eventOrganizatorsRepository)
        {
            _correlationIdProvider = correlationIdProvider;
            _eventsRatingRepository = eventsRatingRepository;
            _accountDataHolder = accountDataHolder;
            _eventsRepository = eventsRepository;
            _notificationsService = notificationsService;
            _eventOrganizatorsRepository = eventOrganizatorsRepository;
        }   

        public async Task<CommandResult<EventRating>> GetEventRatingAsync(Guid eventId, EventRatingType eventRatingType, int? pageIndex, int? pageSize)
        {
            var correlationId = _correlationIdProvider.Get();
            var execTime = Stopwatch.StartNew();
            var methodName = $"{LOGGER_NAME}{nameof(GetEventRatingAsync)}";
            logger.Debug(correlationId, null, methodName, $"Method started", null);

            var eventRating = await _eventsRatingRepository.GetEventRatingAcync(eventId, eventRatingType, pageIndex, pageSize);

            if (eventRating == null)
                return CommandResult<EventRating>.Fail(ErrorCode.EventCategoryNotFound, "Рейтинг мероприятия пуст");

            logger.Debug(correlationId, null, methodName, $"Method finished", null, execTime.Elapsed);
            return new CommandResult<EventRating>(eventRating);
        }

        public async Task<CommandResult<double?>> GetOrganizatorRatingAsync(Guid accountId)
        {
            var correlationId = _correlationIdProvider.Get();
            var execTime = Stopwatch.StartNew();
            var methodName = $"{LOGGER_NAME}{nameof(GetOrganizatorRatingAsync)}";
            logger.Debug(correlationId, null, methodName, $"Method started", null);

            var eventRating = await _eventsRatingRepository.GetOrganizatorRatingAsync(accountId);

            if (eventRating == null)
                return new CommandResult<double?>(null);

            logger.Debug(correlationId, null, methodName, $"Method finished", null, execTime.Elapsed);
            return new CommandResult<double?>(eventRating);
        }

        public async Task<CommandResult> DeleteAsync(Guid itemId)
        {
            var correlationId = _correlationIdProvider.Get();
            var execTime = Stopwatch.StartNew();
            var methodName = $"{LOGGER_NAME}{nameof(DeleteAsync)}";
            logger.Debug(correlationId, null, methodName, $"Method started", null);

            var item = await _eventsRatingRepository.GetRatingItemAsync(itemId);
            if (item == null)
                return CommandResult.Fail(ErrorCode.RatingItemNotFound, $"Оценка с указанным id='{itemId}' не найдена");

            if (item.AccountId != _accountDataHolder.AccountId)
                return CommandResult.Fail(ErrorCode.AccessError, $"Нельзя удалять оценку другого пользователя");

            await _eventsRatingRepository.DeleteEventRatingAsync(itemId);

            var organizators = (await _eventOrganizatorsRepository.GetOrganizatorIdsByEventIdAsync(item.EventId))
                ?.ToList();
            organizators = organizators?.Where(i => i != _accountDataHolder.AccountId)
                ?.ToList();

            await _notificationsService.NotifyEventRatingDeletedAsync(item.EventId, organizators);

            logger.Debug(correlationId, null, methodName, $"Method finished", null, execTime.Elapsed);
            return CommandResult.OK;
        }

        public async Task<CommandResult<Guid>> VoteAsync(EventsRatingItem request)
        {
            var correlationId = _correlationIdProvider.Get();
            var execTime = Stopwatch.StartNew();
            var methodName = $"{LOGGER_NAME}{nameof(VoteAsync)}";
            logger.Debug(correlationId, null, methodName, $"Method started", null);

            var curEvent = await _eventsRepository.GetEventAsync(request.EventId);
            if (curEvent == null)
                return CommandResult<Guid>.Fail(ErrorCode.EventNotFound, $"Событие {request.EventId} не найдено");

            if (curEvent.StartTime > DateTimeOffset.Now)
                request.RatingType = EventRatingType.Expectation;            
            else
                request.RatingType = EventRatingType.Summary;

            request.AccountId = _accountDataHolder.AccountId;
            var eventRating = await _eventsRatingRepository.CreateEventRatingAsync(request);

            var organizators = (await _eventOrganizatorsRepository.GetOrganizatorIdsByEventIdAsync(request.EventId))
                ?.ToList();
            organizators = organizators?.Where(i => i != _accountDataHolder.AccountId)
                ?.ToList();

            await _notificationsService.NotifyNewEventRatingAsync(request.EventId, eventRating);

            logger.Debug(correlationId, null, methodName, $"Method finished", null, execTime.Elapsed);
            return new CommandResult<Guid>(eventRating);
        }
    }
}
