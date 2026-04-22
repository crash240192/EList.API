using EList.Common.CorrelationId;
using EList.Common.Logger;
using EList.Common.Models;
using EList.Common.Support;
using EList.Models.Accounts;
using EList.Models.Participation;
using EList.Models.Person;
using EList.Models.Subscriptions;
using EList.Repositories.Interfaces;
using EList.Services.Interfaces;
using NLog;
using System.Diagnostics;

namespace EList.Services.Impl
{
    public class ParticipationsService : IParticipationsService
    {
        #region logger
        private static readonly ILogger log = LogManager.GetCurrentClassLogger();
        private static readonly ILoggerWrapper logger = new NLogLoggerWrapper(log);
        private const string LOGGER_NAME = "EList.Services.Impl.ParticipationService.";
        #endregion

        private readonly IEventsRepository _eventsRepository;
        private readonly ICorrelationIdProvider _correlationIdProvider;
        private readonly IAccountsRepository _accountsRepository;
        private readonly IAuthorizationRepository _authorizationRepository;
        private readonly IParticipationsRepository _participationRepository;
        private readonly INotificationsService _notificationsService;
        private readonly IAccountDataHolder _accountDataHolder;

        public ParticipationsService(ICorrelationIdProvider correlationIdProvider,
            IEventsRepository eventsRepository,
            IAccountsRepository accountsRepository,
            IAuthorizationRepository authorizationRepository,
            IParticipationsRepository participationRepository,
            INotificationsService notificationsService,
            IAccountDataHolder accountDataHolder)
        {
            _correlationIdProvider = correlationIdProvider ?? throw new ArgumentNullException(nameof(correlationIdProvider));
            _eventsRepository = eventsRepository ?? throw new ArgumentNullException(nameof(eventsRepository));
            _accountsRepository = accountsRepository ?? throw new ArgumentNullException(nameof(accountsRepository));
            _authorizationRepository = authorizationRepository ?? throw new ArgumentNullException(nameof(authorizationRepository));
            _participationRepository = participationRepository ?? throw new ArgumentNullException(nameof(participationRepository));
            _accountDataHolder = accountDataHolder;
        }

        public async Task<CommandResult<Guid?>> ParticipateAsync(Guid id)
        {
            var correlationId = _correlationIdProvider.Get();
            var execTime = Stopwatch.StartNew();
            var methodName = $"{LOGGER_NAME}{nameof(ParticipateAsync)}";
            logger.Debug(correlationId, null, methodName, $"Method started", null);

            var curEvent = await _eventsRepository.GetEventAsync(id);

            if (curEvent == null)
                return CommandResult<Guid?>.Fail(ErrorCode.EventNotFound, $"Событие с id='{id}' не найдено");

            //TODO: Реализовать проверку на наличие пользователя в чёрном списке

            var result = await _participationRepository.ParticipateAsync(_accountDataHolder.AccountId, id);

            logger.Debug(correlationId, null, methodName, $"Method finished", null, execTime.Elapsed);
            return new CommandResult<Guid?>(result);
        }

        public async Task<CommandResult> LeaveEventAsync(Guid id)
        {
            var correlationId = _correlationIdProvider.Get();
            var execTime = Stopwatch.StartNew();
            var methodName = $"{LOGGER_NAME}{nameof(ParticipateAsync)}";
            logger.Debug(correlationId, null, methodName, $"Method started", null);

            var curEvent = await _eventsRepository.GetEventAsync(id);

            if (curEvent == null)
                return CommandResult<Guid?>.Fail(ErrorCode.EventNotFound, $"Событие с id='{id}' не найдено");

            //TODO: Реализовать проверку на то что пользователь является инициатором события

            await _participationRepository.LeaveEventAsync(_accountDataHolder.AccountId, id);

            logger.Debug(correlationId, null, methodName, $"Method finished", null, execTime.Elapsed);
            return CommandResult.OK;
        }

        public async Task<CommandResult<PagedList<Participant>>> GetEventParticipantsAsync(EventParticipantsSearchRequest request)
        {
            var correlationId = _correlationIdProvider.Get();
            var execTime = Stopwatch.StartNew();
            var methodName = $"{LOGGER_NAME}{nameof(ParticipateAsync)}";
            logger.Debug(correlationId, null, methodName, $"Method started", null);

            var curEvent = await _eventsRepository.GetEventAsync(request.EventId);

            if (curEvent == null)
                return CommandResult<PagedList<Participant>>.Fail(ErrorCode.EventNotFound, $"Событие с id='{request.EventId}' не найдено");

            //TODO: Реализовать проверку, доступен ли пользователю просмотр списка участников

            var result = await _participationRepository.GetEventParticipantsAsync(request);

            logger.Debug(correlationId, null, methodName, $"Method finished", null, execTime.Elapsed);
            return new CommandResult<PagedList<Participant>>(result);
        }
    }
}
