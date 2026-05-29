using EList.Common.CorrelationId;
using EList.Common.Logger;
using EList.Common.Models;
using EList.Common.Support;
using EList.Models.EventOrganizators;
using EList.Repositories.Interfaces;
using EList.Services.Interfaces;
using NLog;
using Org.BouncyCastle.Asn1.Ocsp;
using System.Diagnostics;

namespace EList.Services.Impl
{
    public class EventOrganizatorsService : IEventOrganizatorsService
    {
        #region logger
        private static readonly ILogger log = LogManager.GetCurrentClassLogger();
        private static readonly ILoggerWrapper logger = new NLogLoggerWrapper(log);
        private const string LOGGER_NAME = "EList.Services.Impl.EventOrganizatorsService.";
        #endregion

        private readonly IEventsRepository _eventsRepository;
        private readonly ICorrelationIdProvider _correlationIdProvider;
        private readonly IAccountDataHolder _accountDataHolder;
        private readonly IEventOrganizatorsRepository _organizatorsRepository;
        private readonly ISubscriptionsRepository _subscriptionsRepository;

        public EventOrganizatorsService(ICorrelationIdProvider correlationIdProvider,
            IEventsRepository eventsRepository,
            IAccountDataHolder accountDataHolder,
            IEventOrganizatorsRepository organizatorsRepository,
            ISubscriptionsRepository subscriptionsRepository)
        {
            _correlationIdProvider = correlationIdProvider ?? throw new ArgumentNullException(nameof(correlationIdProvider));
            _eventsRepository = eventsRepository ?? throw new ArgumentNullException(nameof(eventsRepository));
            _accountDataHolder = accountDataHolder;
            _organizatorsRepository = organizatorsRepository ?? throw new ArgumentNullException(nameof(organizatorsRepository));
            _subscriptionsRepository = subscriptionsRepository ?? throw new ArgumentNullException(nameof(subscriptionsRepository));
        }

        public async Task<CommandResult<List<EventOrganizator>>> GetByEventIdAsync(Guid eventId)
        {
            var correlationId = _correlationIdProvider.Get();
            var execTime = Stopwatch.StartNew();
            var methodName = $"{LOGGER_NAME}{nameof(GetByEventIdAsync)}";
            logger.Debug(correlationId, null, methodName, $"Method started", null);

            var curEvent = await _eventsRepository.GetEventAsync(eventId);

            if (curEvent == null)
                return CommandResult<List<EventOrganizator>>.Fail(ErrorCode.EventNotFound, $"Событие с id='{eventId}' не найдено");

            //TODO: Реализовать проверку, доступен ли пользователю просмотр списка участников

            var result = await _organizatorsRepository.GetByEventIdAsync(eventId);

            logger.Debug(correlationId, null, methodName, $"Method finished", null, execTime.Elapsed);
            return new CommandResult<List<EventOrganizator>>(result);
        }

        public async Task<CommandResult> AssignEventOrganizatorsAsync(Guid eventId, List<Guid> accountIds)
        {
            var correlationId = _correlationIdProvider.Get();
            var execTime = Stopwatch.StartNew();
            var methodName = $"{LOGGER_NAME}{nameof(AssignEventOrganizatorsAsync)}";
            logger.Debug(correlationId, null, methodName, $"Method started", null);

            var curEvent = await _eventsRepository.GetEventAsync(eventId);

            if (curEvent == null)
                return CommandResult.Fail(ErrorCode.EventNotFound, $"Событие с id='{eventId}' не найдено");

            var eventOrganizators = await _organizatorsRepository.GetByEventIdAsync(eventId);
            if (!eventOrganizators?.Any(i => i.Account?.Id == _accountDataHolder.AccountId) ?? true)
                return CommandResult.Fail(ErrorCode.AccessError, "Пользователь не является организатором события");

            await _organizatorsRepository.AssignAsync(eventId, accountIds);

            logger.Debug(correlationId, null, methodName, $"Method finished", null, execTime.Elapsed);
            return CommandResult.OK;
        }
    }
}
