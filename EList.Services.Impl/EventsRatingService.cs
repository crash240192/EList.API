using EList.Common.CorrelationId;
using EList.Common.Logger;
using EList.Common.Models;
using EList.Common.Support;
using EList.Models.Enums;
using EList.Models.EventsRating;
using EList.Repositories.Interfaces;
using EList.Services.Interfaces;
using NLog;
using Org.BouncyCastle.Asn1.Ocsp;
using System.Diagnostics;

namespace EList.Services.Impl
{
    public class EventsRatingService : IEventsRatingService
    {
        private readonly ICorrelationIdProvider _correlationIdProvider;
        private readonly IEventsRatingRepository _eventsRatingRepository;
        private readonly IAccountDataHolder _accountDataHolder;
        private readonly IEventsRepository _eventsRepository;
        #region logger
        private static readonly ILogger log = LogManager.GetCurrentClassLogger();
        private static readonly ILoggerWrapper logger = new NLogLoggerWrapper(log);
        private const string LOGGER_NAME = "EList.Services.Impl.EventsRatingService.";
        #endregion

        public EventsRatingService(
            ICorrelationIdProvider correlationIdProvider,
            IEventsRatingRepository eventsRatingRepository,
            IEventsRepository eventsRepository,
            IAccountDataHolder accountDataHolder)
        {
            _correlationIdProvider = correlationIdProvider;
            _eventsRatingRepository = eventsRatingRepository;
            _accountDataHolder = accountDataHolder;
            _eventsRepository = eventsRepository;
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

        public Task<CommandResult<int?>> GetOrganizatorRatingAsync(Guid accountId)
        {
            throw new NotImplementedException();
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

            logger.Debug(correlationId, null, methodName, $"Method finished", null, execTime.Elapsed);
            return new CommandResult<Guid>(eventRating);
        }
    }
}
