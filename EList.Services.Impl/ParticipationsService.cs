using EList.Common.CorrelationId;
using EList.Common.Logger;
using EList.Common.Models;
using EList.Common.Support;
using EList.Models.Enums;
using EList.Models.Invitations;
using EList.Models.Participation;
using EList.Repositories.Interfaces;
using EList.Services.Interfaces;
using EList.Validators.Interfaces;
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
        private readonly IParticipationsRepository _participationRepository;
        private readonly IInvitationsRepository _invitationsRepository;
        private readonly IAccountDataHolder _accountDataHolder;
        private readonly IParticipantsBWListRepository _participantsBWListRepository;
        private readonly INotificationsService _notificationsService;
        private readonly IModerationPenaltiesService _moderationPenaltiesService;
        private readonly IParticipationAccessValidator _participationAccessValidator;

        public ParticipationsService(ICorrelationIdProvider correlationIdProvider,
            IEventsRepository eventsRepository,
            IParticipationsRepository participationRepository,
            IInvitationsRepository invitationsRepository,
            IAccountDataHolder accountDataHolder,
            IParticipantsBWListRepository participantsBWListRepository,
            INotificationsService notificationsService,
            IModerationPenaltiesService moderationPenaltiesService,
            IParticipationAccessValidator participationAccessValidator)
        {
            _correlationIdProvider = correlationIdProvider ?? throw new ArgumentNullException(nameof(correlationIdProvider));
            _eventsRepository = eventsRepository ?? throw new ArgumentNullException(nameof(eventsRepository));
            _participationRepository = participationRepository ?? throw new ArgumentNullException(nameof(participationRepository));
            _invitationsRepository = invitationsRepository ?? throw new ArgumentNullException(nameof(invitationsRepository));
            _participantsBWListRepository = participantsBWListRepository ?? throw new ArgumentNullException(nameof(participantsBWListRepository));
            _notificationsService = notificationsService ?? throw new ArgumentNullException(nameof(notificationsService));
            _moderationPenaltiesService = moderationPenaltiesService ?? throw new ArgumentNullException(nameof(moderationPenaltiesService));
            _participationAccessValidator = participationAccessValidator ?? throw new ArgumentNullException(nameof(participationAccessValidator));
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

            var participateBan = await _moderationPenaltiesService.AssertNotRestrictedAsync(
                _accountDataHolder.AccountId.Value, ModerationPenaltyType.BanEventParticipate);
            if (!participateBan.Success)
                return CommandResult<Guid?>.Fail(participateBan.ErrorCode, participateBan.Message);

            var eventBan = await _moderationPenaltiesService.AssertNotRestrictedAsync(
                _accountDataHolder.AccountId.Value, ModerationPenaltyType.BanFromEvent, eventId);
            if (!eventBan.Success)
                return CommandResult<Guid?>.Fail(eventBan.ErrorCode, eventBan.Message);

            if (curEvent.Parameters.Private ?? false)
            {
                var whiteListCount = await _participantsBWListRepository.WhiteListPersonsCountAsync(eventId);
                if (whiteListCount == 0)
                {// если белый список пуст, проверяем приглашения
                    var isUserInvited = await _invitationsRepository.IsUserInvitatedAsync(_accountDataHolder.AccountId.Value, eventId);
                    if (!isUserInvited)
                        return CommandResult<Guid?>.Fail(ErrorCode.AccessError, "Принять участие в закрытом мероприятии можно только по приглашению");
                }
                else
                {
                    if (!await _participantsBWListRepository.IsUserInWhiteListAsync(eventId, _accountDataHolder.AccountId.Value))
                        return CommandResult<Guid?>.Fail(ErrorCode.AccessError, "Участвовать в закрытом мероприятии могут только пользователи из белого списка");
                }
            }
            else
            {
                if (await _participantsBWListRepository.IsUserInBlackListAsync(eventId, _accountDataHolder.AccountId.Value))
                    return CommandResult<Guid?>.Fail(ErrorCode.AccessError, "Организатор добавил вас в чёрный список мероприятия");
            }

            if (curEvent.Parameters?.MaxPersonsCount > 0)
            {
                var participationsCount = await _participationRepository.GetParticipantsCountAsync(eventId);
                if (participationsCount >= curEvent.Parameters.MaxPersonsCount)
                    return CommandResult<Guid?>.Fail(ErrorCode.EventIsFull, "В мероприятии уже участвует максимальное количество человек");
            }

            var result = await _participationRepository.ParticipateAsync(_accountDataHolder.AccountId.Value, eventId);

            var thisEventInvitations = await _invitationsRepository.SearchInvitationsAsync(new InvitationsSearchRequest
            {
                InvitedAccountIds = new List<Guid> { _accountDataHolder.AccountId.Value },
                EventIds = new List<Guid> { eventId }
            });
            if (thisEventInvitations.Result?.Any() ?? false)
            {
                thisEventInvitations.Result.ForEach(async i => await _invitationsRepository.DeleteInvitationAsync(eventId, _accountDataHolder.AccountId.Value));
            }

            await _notificationsService.NotifyParticipatedAsync(eventId);

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

            await _participationRepository.LeaveEventAsync(_accountDataHolder.AccountId.Value, eventId);

            await _notificationsService.NotifyEventLeftAsync(eventId);

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

            var accessError = await _participationAccessValidator.AssertCanViewParticipantsAsync(
                curEvent, _accountDataHolder.AccountId, _accountDataHolder.AdultConfirmed);
            if (!accessError.Success)
                return CommandResult<PagedList<Participant>>.Fail(accessError.ErrorCode, accessError.Message);

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

            var accessError = await _participationAccessValidator.AssertCanManageBwListsAsync(
                eventId, _accountDataHolder.AccountId);
            if (!accessError.Success)
                return CommandResult<PagedList<ParticipantBlackListItem>>.Fail(accessError.ErrorCode, accessError.Message);

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

            var accessError = await _participationAccessValidator.AssertCanManageBwListsAsync(
                eventId, _accountDataHolder.AccountId);
            if (!accessError.Success)
                return CommandResult<PagedList<ParticipantWhiteListItem>>.Fail(accessError.ErrorCode, accessError.Message);

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

            var accessError = await _participationAccessValidator.AssertCanManageBwListsAsync(
                eventId, _accountDataHolder.AccountId);
            if (!accessError.Success)
                return CommandResult<List<Guid>>.Fail(accessError.ErrorCode, accessError.Message);

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

            var accessError = await _participationAccessValidator.AssertCanManageBwListsAsync(
                eventId, _accountDataHolder.AccountId);
            if (!accessError.Success)
                return CommandResult<List<Guid>>.Fail(accessError.ErrorCode, accessError.Message);

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

            var accessError = await _participationAccessValidator.AssertCanManageBwListsAsync(
                request.EventId, _accountDataHolder.AccountId);
            if (!accessError.Success)
                return accessError;

            await _participantsBWListRepository.AddToBlackListAsync(request);
            var existingParticipants = await _participationRepository.GetEventParticipantIdsAsync(request.EventId);
            var bannedUsers = request.AccountIds?.Intersect(existingParticipants)?.ToList();

            await _invitationsRepository.DeleteInvitationAsync(request.EventId, request.AccountIds);
            await _participationRepository.DropParticipationsAsync(request.EventId, request.AccountIds);

            await _notificationsService.NotifyAddedToBlackListAsync(request.EventId, bannedUsers);

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

            var accessError = await _participationAccessValidator.AssertCanManageBwListsAsync(
                request.EventId, _accountDataHolder.AccountId);
            if (!accessError.Success)
                return accessError;

            await _participantsBWListRepository.AddToWhiteListAsync(request);
            var whiteList = await _participantsBWListRepository.GetEventWhiteListShortAsync(request.EventId);

            var existingParticipants = await _participationRepository.GetEventParticipantIdsAsync(request.EventId);
            var bannedUsers = existingParticipants.Where(i => !whiteList.Contains(i)).ToList();

            await _invitationsRepository.CancelAllInvitationsExceptThisUsersAsync(request.EventId, whiteList);
            await _participationRepository.DropAllParticipationsExceptThisUsersAsync(request.EventId, whiteList);

            await _notificationsService.NotifyNotInWhiteListAsync(request.EventId, bannedUsers);

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

            var accessError = await _participationAccessValidator.AssertCanManageBwListsAsync(
                eventId, _accountDataHolder.AccountId);
            if (!accessError.Success)
                return accessError;

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

            var accessError = await _participationAccessValidator.AssertCanManageBwListsAsync(
                eventId, _accountDataHolder.AccountId);
            if (!accessError.Success)
                return accessError;

            await _participantsBWListRepository.DeleteFromWhiteListAsync(eventId, accountId);

            logger.Debug(correlationId, null, methodName, $"Method finished", null, execTime.Elapsed);
            return CommandResult.OK;
        }
    }
}
