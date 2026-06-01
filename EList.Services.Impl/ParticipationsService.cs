using EList.Common.CorrelationId;
using EList.Common.Logger;
using EList.Common.Models;
using EList.Common.Support;
using EList.Models.Accounts;
using EList.Models.Invitations;
using EList.Models.Participation;
using EList.Models.Person;
using EList.Models.Subscriptions;
using EList.Repositories.Interfaces;
using EList.Services.Interfaces;
using NLog;
using Org.BouncyCastle.Asn1.Ocsp;
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
        private readonly ISystemNotificationsService _notificationsService;
        private readonly IInvitationsRepository _invitationsRepository;
        private readonly IAccountDataHolder _accountDataHolder;
        private readonly IParticipantsBWListRepository _participantsBWListRepository;
        private readonly IEventOrganizatorsRepository _eventOrganizatorsRepository;

        public ParticipationsService(ICorrelationIdProvider correlationIdProvider,
            IEventsRepository eventsRepository,
            IAccountsRepository accountsRepository,
            IAuthorizationRepository authorizationRepository,
            IParticipationsRepository participationRepository,
            ISystemNotificationsService notificationsService,
            IInvitationsRepository invitationsRepository,
            IAccountDataHolder accountDataHolder,
            IParticipantsBWListRepository participantsBWListRepository,
            IEventOrganizatorsRepository eventOrganizatorsRepository)
        {
            _correlationIdProvider = correlationIdProvider ?? throw new ArgumentNullException(nameof(correlationIdProvider));
            _eventsRepository = eventsRepository ?? throw new ArgumentNullException(nameof(eventsRepository));
            _accountsRepository = accountsRepository ?? throw new ArgumentNullException(nameof(accountsRepository));
            _authorizationRepository = authorizationRepository ?? throw new ArgumentNullException(nameof(authorizationRepository));
            _participationRepository = participationRepository ?? throw new ArgumentNullException(nameof(participationRepository));
            _invitationsRepository = invitationsRepository ?? throw new ArgumentNullException(nameof(invitationsRepository));
            _participantsBWListRepository = participantsBWListRepository ?? throw new ArgumentNullException(nameof(participantsBWListRepository));
            _eventOrganizatorsRepository = eventOrganizatorsRepository ?? throw new ArgumentNullException(nameof(eventOrganizatorsRepository));
            _accountDataHolder = accountDataHolder;
        }

        public async Task<CommandResult<Guid?>> ParticipateAsync(Guid eventId)
        {
            var correlationId = _correlationIdProvider.Get();
            var execTime = Stopwatch.StartNew();
            var methodName = $"{LOGGER_NAME}{nameof(ParticipateAsync)}";
            logger.Debug(correlationId, null, methodName, $"Method started", null);

            var curEvent = await _eventsRepository.GetEventAsync(eventId);

            if (curEvent == null)
                return CommandResult<Guid?>.Fail(ErrorCode.EventNotFound, $"Событие с id='{eventId}' не найдено");

            if (curEvent.Active == false)
                return CommandResult<Guid?>.Fail(ErrorCode.EventCancelled, "Мероприятие было отменено");

            if (curEvent.Parameters.Private ?? false)
            {
                var whiteListCount = await _participantsBWListRepository.WhiteListPersonsCountAsync(eventId);
                if (whiteListCount == 0)
                {// если белый список пуст, проверяем приглашения
                    var isUserInvited = await _invitationsRepository.IsUserInvitatedAsync(eventId, _accountDataHolder.AccountId);
                    if (!isUserInvited)
                        return CommandResult<Guid?>.Fail(ErrorCode.AccessError, "Принять участие в закрытом мероприятии можно только по приглашению");
                }
                else
                {
                    if (!await _participantsBWListRepository.IsUserInWhiteListAsync(eventId, _accountDataHolder.AccountId))
                        return CommandResult<Guid?>.Fail(ErrorCode.AccessError, "Участвовать в закрытом мероприятии могут только пользователи из белого списка");
                }
            }
            else
            {
                if (await _participantsBWListRepository.IsUserInBlackListAsync(eventId, _accountDataHolder.AccountId))
                    return CommandResult<Guid?>.Fail(ErrorCode.AccessError, "Организатор добавил вас в чёрный список мероприятия");
            }

            if (curEvent.Parameters?.MaxPersonsCount > 0)
            {
                var participationsCount = await _participationRepository.GetParticipantsCountAsync(eventId);
                if (participationsCount >= curEvent.Parameters.MaxPersonsCount)
                    return CommandResult<Guid?>.Fail(ErrorCode.EventIsFull, "В мероприятии уже участвует максимальное количество человек");
            }

            var result = await _participationRepository.ParticipateAsync(_accountDataHolder.AccountId, eventId);

            var thisEventInvitations = await _invitationsRepository.SearchInvitationsAsync(new InvitationsSearchRequest
            {
                InvitedAccountIds = new List<Guid> { _accountDataHolder.AccountId },
                EventIds = new List<Guid> { eventId }
            });
            if (thisEventInvitations.Result?.Any() ?? false)
            {
                thisEventInvitations.Result.ForEach(async i => await _invitationsRepository.DeleteInvitationAsync(eventId, _accountDataHolder.AccountId));
            }

            logger.Debug(correlationId, null, methodName, $"Method finished", null, execTime.Elapsed);
            return new CommandResult<Guid?>(result);
        }

        public async Task<CommandResult> LeaveEventAsync(Guid eventId)
        {
            var correlationId = _correlationIdProvider.Get();
            var execTime = Stopwatch.StartNew();
            var methodName = $"{LOGGER_NAME}{nameof(LeaveEventAsync)}";
            logger.Debug(correlationId, null, methodName, $"Method started", null);

            var curEvent = await _eventsRepository.GetEventAsync(eventId);

            if (curEvent == null)
                return CommandResult<Guid?>.Fail(ErrorCode.EventNotFound, $"Событие с id='{eventId}' не найдено");

            //TODO: Реализовать проверку на то что пользователь является инициатором события

            await _participationRepository.LeaveEventAsync(_accountDataHolder.AccountId, eventId);

            logger.Debug(correlationId, null, methodName, $"Method finished", null, execTime.Elapsed);
            return CommandResult.OK;
        }

        public async Task<CommandResult<PagedList<Participant>>> GetEventParticipantsAsync(EventParticipantsSearchRequest request)
        {
            var correlationId = _correlationIdProvider.Get();
            var execTime = Stopwatch.StartNew();
            var methodName = $"{LOGGER_NAME}{nameof(GetEventParticipantsAsync)}";
            logger.Debug(correlationId, null, methodName, $"Method started", null);

            var curEvent = await _eventsRepository.GetEventAsync(request.EventId);

            if (curEvent == null)
                return CommandResult<PagedList<Participant>>.Fail(ErrorCode.EventNotFound, $"Событие с id='{request.EventId}' не найдено");

            //TODO: Реализовать проверку, доступен ли пользователю просмотр списка участников

            var result = await _participationRepository.GetEventParticipantsAsync(request);

            logger.Debug(correlationId, null, methodName, $"Method finished", null, execTime.Elapsed);
            return new CommandResult<PagedList<Participant>>(result);
        }



        public async Task<CommandResult<PagedList<ParticipantBlackListItem>>> GetEventBlackListAsync(Guid eventId, int? pageIndex, int? pageSize)
        {
            var correlationId = _correlationIdProvider.Get();
            var execTime = Stopwatch.StartNew();
            var methodName = $"{LOGGER_NAME}{nameof(GetEventBlackListAsync)}";
            logger.Debug(correlationId, null, methodName, $"Method started", null);

            var result = await _participantsBWListRepository.GetEventBlackListAsync(eventId, pageIndex, pageSize);

            logger.Debug(correlationId, null, methodName, $"Method finished", null, execTime.Elapsed);
            return new CommandResult<PagedList<ParticipantBlackListItem>>(result);
        }

        public async Task<CommandResult<PagedList<ParticipantWhiteListItem>>> GetEventWhiteListAsync(Guid eventId, int? pageIndex, int? pageSize)
        {
            var correlationId = _correlationIdProvider.Get();
            var execTime = Stopwatch.StartNew();
            var methodName = $"{LOGGER_NAME}{nameof(GetEventWhiteListAsync)}";
            logger.Debug(correlationId, null, methodName, $"Method started", null);

            var result = await _participantsBWListRepository.GetEventWhiteListAsync(eventId, pageIndex, pageSize);

            logger.Debug(correlationId, null, methodName, $"Method finished", null, execTime.Elapsed);
            return new CommandResult<PagedList<ParticipantWhiteListItem>>(result);
        }


        public async Task<CommandResult<List<Guid>>> GetEventBlackListShortAsync(Guid eventId)
        {
            var correlationId = _correlationIdProvider.Get();
            var execTime = Stopwatch.StartNew();
            var methodName = $"{LOGGER_NAME}{nameof(GetEventBlackListShortAsync)}";
            logger.Debug(correlationId, null, methodName, $"Method started", null);

            var result = await _participantsBWListRepository.GetEventBlackListShortAsync(eventId);

            logger.Debug(correlationId, null, methodName, $"Method finished", null, execTime.Elapsed);
            return new CommandResult<List<Guid>>(result);
        }

        public async Task<CommandResult<List<Guid>>> GetEventWhiteListShortAsync(Guid eventId)
        {
            var correlationId = _correlationIdProvider.Get();
            var execTime = Stopwatch.StartNew();
            var methodName = $"{LOGGER_NAME}{nameof(GetEventWhiteListShortAsync)}";
            logger.Debug(correlationId, null, methodName, $"Method started", null);

            var result = await _participantsBWListRepository.GetEventWhiteListShortAsync(eventId);

            logger.Debug(correlationId, null, methodName, $"Method finished", null, execTime.Elapsed);
            return new CommandResult<List<Guid>>(result);
        }


        public async Task<CommandResult> AddToBlackListAsync(AddUsersToBWListRequest request)
        {
            var correlationId = _correlationIdProvider.Get();
            var execTime = Stopwatch.StartNew();
            var methodName = $"{LOGGER_NAME}{nameof(AddToBlackListAsync)}";
            logger.Debug(correlationId, null, methodName, $"Method started", null);

            if (!request.AccountIds?.Any() ?? true)
                return CommandResult.Fail(ErrorCode.IsNullOrEmpty, "Список пользователей не указан");

            var eventOrganizators = await _eventOrganizatorsRepository.GetByEventIdAsync(request.EventId);
            if (!eventOrganizators?.Any(i => i.Account?.Id == _accountDataHolder.AccountId) ?? true)
                return CommandResult.Fail(ErrorCode.AccessError, "Пользователь не является организатором события");

            await _participantsBWListRepository.AddToBlackListAsync(request);
            await _invitationsRepository.DeleteInvitationAsync(request.EventId, request.AccountIds);
            await _participationRepository.DropParticipationsAsync(request.EventId, request.AccountIds);

            //TODO: Сформировать удаление о том что пользователя исключили, если он участвовал в мероприятии или у него было приглашение

            logger.Debug(correlationId, null, methodName, $"Method finished", null, execTime.Elapsed);
            return CommandResult.OK;
        }

        public async Task<CommandResult> AddToWhiteListAsync(AddUsersToBWListRequest request)
        {
            var correlationId = _correlationIdProvider.Get();
            var execTime = Stopwatch.StartNew();
            var methodName = $"{LOGGER_NAME}{nameof(AddToWhiteListAsync)}";
            logger.Debug(correlationId, null, methodName, $"Method started", null);

            if (!request.AccountIds?.Any() ?? true)
                return CommandResult.Fail(ErrorCode.IsNullOrEmpty, "Список пользователей не указан");

            var eventOrganizators = await _eventOrganizatorsRepository.GetByEventIdAsync(request.EventId);
            if (!eventOrganizators?.Any(i => i.Account?.Id == _accountDataHolder.AccountId) ?? true)
                return CommandResult.Fail(ErrorCode.AccessError, "Пользователь не является организатором события");

            await _participantsBWListRepository.AddToWhiteListAsync(request);
            await _invitationsRepository.CancelAllInvitationsExceptThisUsersAsync(request.EventId, request.AccountIds);
            await _participationRepository.DropAllParticipationsExceptThisUsersAsync(request.EventId, request.AccountIds);

            //TODO: Сформировать удаление о том что пользователя исключили, если он участвовал в мероприятии или у него было приглашение

            logger.Debug(correlationId, null, methodName, $"Method finished", null, execTime.Elapsed);
            return CommandResult.OK;
        }


        public async Task<CommandResult> DeleteFromBlackListAsync(Guid eventId, Guid accountId)
        {
            var correlationId = _correlationIdProvider.Get();
            var execTime = Stopwatch.StartNew();
            var methodName = $"{LOGGER_NAME}{nameof(DeleteFromBlackListAsync)}";
            logger.Debug(correlationId, null, methodName, $"Method started", null);

            var eventOrganizators = await _eventOrganizatorsRepository.GetByEventIdAsync(eventId);
            if (!eventOrganizators?.Any(i => i.Account?.Id == _accountDataHolder.AccountId) ?? true)
                return CommandResult.Fail(ErrorCode.AccessError, "Пользователь не является организатором события");

            await _participantsBWListRepository.DeleteFromBlackListAsync(eventId, accountId);

            logger.Debug(correlationId, null, methodName, $"Method finished", null, execTime.Elapsed);
            return CommandResult.OK;
        }

        public async Task<CommandResult> DeleteFromWhiteListAsync(Guid eventId, Guid accountId)
        {
            var correlationId = _correlationIdProvider.Get();
            var execTime = Stopwatch.StartNew();
            var methodName = $"{LOGGER_NAME}{nameof(DeleteFromWhiteListAsync)}";
            logger.Debug(correlationId, null, methodName, $"Method started", null);

            var eventOrganizators = await _eventOrganizatorsRepository.GetByEventIdAsync(eventId);
            if (!eventOrganizators?.Any(i => i.Account?.Id == _accountDataHolder.AccountId) ?? true)
                return CommandResult.Fail(ErrorCode.AccessError, "Пользователь не является организатором события");

            await _participantsBWListRepository.DeleteFromWhiteListAsync(eventId, accountId);

            logger.Debug(correlationId, null, methodName, $"Method finished", null, execTime.Elapsed);
            return CommandResult.OK;
        }
    }
}
