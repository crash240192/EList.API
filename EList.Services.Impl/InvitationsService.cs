using System.Diagnostics;
using EList.Common.CorrelationId;
using EList.Common.Logger;
using EList.Common.Models;
using EList.Common.Support;
using EList.Models.Invitations;
using EList.Repositories.Interfaces;
using EList.Services.Interfaces;
using EList.Validators.Interfaces;
using NLog;

namespace EList.Services.Impl
{
    public class InvitationsService : IInvitationsService
    {
        #region logger
        private static readonly ILogger log = LogManager.GetCurrentClassLogger();
        private static readonly ILoggerWrapper logger = new NLogLoggerWrapper(log);
        private const string LOGGER_NAME = "EList.Services.Impl.InvitationsService.";
        #endregion

        private readonly ICorrelationIdProvider _correlationIdProvider;
        private readonly IEventsRepository _eventsRepository;
        private readonly IInvitationsRepository _invitationsRepository;
        private readonly IParticipationsRepository _participationsRepository;
        private readonly IAccountDataHolder _accountDataHolder;
        private readonly IParticipantsBWListRepository _participantsBWListRepository;
        private readonly INotificationsService _notificationsService;
        private readonly IModerationPenaltiesService _moderationPenaltiesService;
        private readonly IInvitationAccessValidator _invitationAccessValidator;
        private readonly IInvitationDataValidator _invitationDataValidator;
        private readonly IPagingValidator _pagingValidator;

        public InvitationsService(ICorrelationIdProvider correlationIdProvider,
            IEventsRepository eventsRepository,
            IInvitationsRepository invitationsRepository,
            IParticipationsRepository participationsRepository,
            IAccountDataHolder accountDataHolder,
            IParticipantsBWListRepository participantsBWListRepository,
            INotificationsService notificationsService,
            IModerationPenaltiesService moderationPenaltiesService,
            IInvitationAccessValidator invitationAccessValidator,
            IInvitationDataValidator invitationDataValidator,
            IPagingValidator pagingValidator)
        {
            _correlationIdProvider = correlationIdProvider ?? throw new ArgumentNullException(nameof(correlationIdProvider));
            _eventsRepository = eventsRepository ?? throw new ArgumentNullException(nameof(eventsRepository));
            _invitationsRepository = invitationsRepository ?? throw new ArgumentNullException(nameof(invitationsRepository));
            _participationsRepository = participationsRepository ?? throw new ArgumentNullException(nameof(participationsRepository));
            _participantsBWListRepository = participantsBWListRepository ?? throw new ArgumentNullException(nameof(participantsBWListRepository));
            _notificationsService = notificationsService ?? throw new ArgumentNullException(nameof(notificationsService));
            _moderationPenaltiesService = moderationPenaltiesService ?? throw new ArgumentNullException(nameof(moderationPenaltiesService));
            _invitationAccessValidator = invitationAccessValidator ?? throw new ArgumentNullException(nameof(invitationAccessValidator));
            _invitationDataValidator = invitationDataValidator ?? throw new ArgumentNullException(nameof(invitationDataValidator));
            _pagingValidator = pagingValidator ?? throw new ArgumentNullException(nameof(pagingValidator));
            _accountDataHolder = accountDataHolder;
        }

        public async Task<CommandResult> CreateAsync(CreateInvitationsRequest request)
        {
            var correlationId = _correlationIdProvider.Get();
            var execTime = Stopwatch.StartNew();
            var methodName = $"{LOGGER_NAME}{nameof(CreateAsync)}";

            logger.Debug(correlationId, null, methodName, $"Method started", null);

            var dataError = _invitationDataValidator.ValidateCreateRequest(request);
            if (!dataError.Success)
                return dataError;

            var curEvent = await _eventsRepository.GetEventAsync(request.EventId);
            if (curEvent == null)
                return CommandResult.Fail(ErrorCode.EventNotFound, $"Мероприятие с id='{request.EventId}' не найдено");

            var createAccess = await _invitationAccessValidator.AssertCanCreateInvitationsAsync(
                curEvent, _accountDataHolder.AccountId, request.InviterOrganizationId);
            if (!createAccess.Success)
                return createAccess;

            if (curEvent.Active == false)
                return CommandResult.Fail(ErrorCode.EventCancelled, $"Мероприятие было отменено");

            if (curEvent.Parameters?.MaxPersonsCount > 0)
            {
                var participantsCount = await _participationsRepository.GetParticipantsCountAsync(curEvent.Id);
                if (participantsCount >= curEvent.Parameters.MaxPersonsCount)
                    return CommandResult.Fail(ErrorCode.EventIsFull, $"В мероприятии уже участвует максимальное количество человек");
            }

            #region filterInvitations
            var someInvitationsFiltered = false;
            var message = string.Empty;
            if (curEvent.Parameters?.Private ?? false)
            {
                var whiteListIsEmpty = await _participantsBWListRepository.IsWhiteListEmptyAsync(curEvent.Id);
                if (!whiteListIsEmpty)
                {
                    var filteredAccounts = await _participantsBWListRepository.FilterUsersByWhiteListAsync(curEvent.Id, request.AccountIds);
                    someInvitationsFiltered = filteredAccounts.Count() != request.AccountIds.Count();
                    request.AccountIds = filteredAccounts;
                    if (someInvitationsFiltered)
                        message = "Некоторых пользователей нет в белом списках. Им не удалось отправить приглашение";
                }
            }
            else
            {
                var filteredAccounts = await _participantsBWListRepository.FilterUsersByBlackListAsync(curEvent.Id, request.AccountIds);
                someInvitationsFiltered = filteredAccounts.Count() != request.AccountIds.Count();
                request.AccountIds = filteredAccounts;
                if (someInvitationsFiltered)
                    message = "Некоторые пользователи находятся в чёрном списке. Им не удалось отправить приглашение";
            }
            #endregion

            if (!request.AccountIds?.Any() ?? true)
                return CommandResult.Fail(ErrorCode.IsNullOrEmpty, message.Length > 0 ? message : "Список пользователей пуст");

            await _invitationsRepository.CreateInvitationsAsync(request, _accountDataHolder.AccountId.Value);

            await _notificationsService.NotifyUsersInvitedAsync(request.EventId, request.AccountIds);

            logger.Debug(correlationId, null, methodName, $"Method finished", null, execTime.Elapsed);

            var result = CommandResult.OK;
            if (someInvitationsFiltered)
                result.Message = message;
            return result;
        }

        public async Task<CommandResult> AcceptAsync(Guid invitationId)
        {
            var correlationId = _correlationIdProvider.Get();
            var execTime = Stopwatch.StartNew();
            var methodName = $"{LOGGER_NAME}{nameof(AcceptAsync)}";

            logger.Debug(correlationId, null, methodName, $"Method started", null);

            var invitation = await _invitationsRepository.GetInvitationAsync(invitationId);
            if (invitation == null)
                return CommandResult.Fail(ErrorCode.InvitationNotFound, $"Приглашение с id='{invitationId}' не найдено");

            var acceptAccess = await _invitationAccessValidator.AssertCanAcceptOrDeclineAsync(
                invitation, _accountDataHolder.AccountId);
            if (!acceptAccess.Success)
                return acceptAccess;

            var curEvent = await _eventsRepository.GetEventAsync(invitation.EventId);
            if (curEvent == null)
                return CommandResult.Fail(ErrorCode.EventNotFound, $"Мероприятие с id='{invitation.EventId}' не найдено");

            if (curEvent.Active == false)
                return CommandResult.Fail(ErrorCode.EventCancelled, $"Мероприятие было отменено");

            var participateBan = await _moderationPenaltiesService.AssertNotRestrictedAsync(
                _accountDataHolder.AccountId.Value, EList.Models.Enums.ModerationPenaltyType.BanEventParticipate);
            if (!participateBan.Success)
                return CommandResult.Fail(participateBan.ErrorCode, participateBan.Message);

            var eventBan = await _moderationPenaltiesService.AssertNotRestrictedAsync(
                _accountDataHolder.AccountId.Value, EList.Models.Enums.ModerationPenaltyType.BanFromEvent, invitation.EventId);
            if (!eventBan.Success)
                return CommandResult.Fail(eventBan.ErrorCode, eventBan.Message);

            // Согласовано с ParticipateAsync: при пустом WL достаточно самого приглашения.
            if (curEvent.Parameters?.Private ?? false)
            {
                var whiteListCount = await _participantsBWListRepository.WhiteListPersonsCountAsync(curEvent.Id);
                if (whiteListCount > 0
                    && !await _participantsBWListRepository.IsUserInWhiteListAsync(curEvent.Id, _accountDataHolder.AccountId.Value))
                    return CommandResult.Fail(ErrorCode.AccessError, "Участвовать в закрытом мероприятии могут только пользователи из белого списка");
            }
            else if (await _participantsBWListRepository.IsUserInBlackListAsync(curEvent.Id, _accountDataHolder.AccountId.Value))
            {
                return CommandResult.Fail(ErrorCode.AccessError, "Организатор добавил вас в чёрный список мероприятия");
            }

            if (curEvent.Parameters?.MaxPersonsCount > 0)
            {
                var participantsCount = await _participationsRepository.GetParticipantsCountAsync(curEvent.Id);
                if (participantsCount >= curEvent.Parameters.MaxPersonsCount)
                    return CommandResult.Fail(ErrorCode.EventIsFull, $"В мероприятии уже участвует максимальное количество человек");
            }

            await _participationsRepository.ParticipateAsync(_accountDataHolder.AccountId.Value, invitation.EventId);

            await _invitationsRepository.DeleteInvitationAsync(invitationId);

            await _notificationsService.NotifyInvitationAcceptedAsync(
                invitation.EventId,
                invitation.InvitedAccountId,
                invitation.InviterAccountId);
            await _notificationsService.NotifyParticipatedAsync(invitation.EventId);

            logger.Debug(correlationId, null, methodName, $"Method finished", null, execTime.Elapsed);
            return CommandResult.OK;
        }

        public async Task<CommandResult> DeclineAsync(Guid invitationId)
        {
            var correlationId = _correlationIdProvider.Get();
            var execTime = Stopwatch.StartNew();
            var methodName = $"{LOGGER_NAME}{nameof(DeclineAsync)}";

            logger.Debug(correlationId, null, methodName, $"Method started", null);

            var invitation = await _invitationsRepository.GetInvitationAsync(invitationId);
            if (invitation == null)
                return CommandResult.Fail(ErrorCode.InvitationNotFound, $"Приглашение с id='{invitationId}' не найдено");

            var accessError = await _invitationAccessValidator.AssertCanAcceptOrDeclineAsync(
                invitation, _accountDataHolder.AccountId);
            if (!accessError.Success)
                return accessError;

            await _invitationsRepository.DeleteInvitationAsync(invitationId);

            await _notificationsService.NotifyInvitationDeclinedAsync(
                invitation.EventId,
                invitation.InvitedAccountId,
                invitation.InviterAccountId);

            logger.Debug(correlationId, null, methodName, $"Method finished", null, execTime.Elapsed);
            return CommandResult.OK;
        }

        public async Task<CommandResult> CancelInvitationAsync(Guid invitationId)
        {
            var correlationId = _correlationIdProvider.Get();
            var execTime = Stopwatch.StartNew();
            var methodName = $"{LOGGER_NAME}{nameof(CancelInvitationAsync)}";

            logger.Debug(correlationId, null, methodName, $"Method started", null);

            var invitation = await _invitationsRepository.GetInvitationAsync(invitationId);
            if (invitation == null)
                return CommandResult.Fail(ErrorCode.InvitationNotFound, $"Приглашение с id='{invitationId}' не найдено");

            var accessError = await _invitationAccessValidator.AssertCanCancelInvitationAsync(
                invitation, _accountDataHolder.AccountId);
            if (!accessError.Success)
                return accessError;

            await _invitationsRepository.DeleteInvitationAsync(invitationId);

            await _notificationsService.NotifyInvitationCancelledAsync(
                invitation.EventId,
                invitation.InvitedAccountId);

            logger.Debug(correlationId, null, methodName, $"Method finished", null, execTime.Elapsed);
            return CommandResult.OK;
        }

        public async Task<CommandResult<PagedList<Invitation>>> GetUserInvitationsAsync(int pageIndex = 0, int pageSize = 20)
        {
            var correlationId = _correlationIdProvider.Get();
            var execTime = Stopwatch.StartNew();
            var methodName = $"{LOGGER_NAME}{nameof(GetUserInvitationsAsync)}";

            logger.Debug(correlationId, null, methodName, $"Method started", null);

            int? pageIndexValue = pageIndex;
            int? pageSizeValue = pageSize;
            var pagingError = _pagingValidator.Validate(pageIndexValue, pageSizeValue);
            if (!pagingError.Success)
                return CommandResult<PagedList<Invitation>>.Fail(pagingError.ErrorCode, pagingError.Message);

            _pagingValidator.Normalize(ref pageIndexValue, ref pageSizeValue);

            var invitations = await _invitationsRepository.SearchInvitationsAsync(new InvitationsSearchRequest
            {
                InvitedAccountIds = new List<Guid> { _accountDataHolder.AccountId.Value },
                PageIndex = pageIndexValue,
                PageSize = pageSizeValue,
            });

            logger.Debug(correlationId, null, methodName, $"Method finished", null, execTime.Elapsed);
            return new CommandResult<PagedList<Invitation>>(invitations);
        }

        public async Task<CommandResult<PagedList<Invitation>>> SearchAsync(InvitationsSearchRequest request)
        {
            var correlationId = _correlationIdProvider.Get();
            var execTime = Stopwatch.StartNew();
            var methodName = $"{LOGGER_NAME}{nameof(SearchAsync)}";

            logger.Debug(correlationId, null, methodName, $"Method started", null);

            if (_accountDataHolder.AccountId == null)
                return CommandResult<PagedList<Invitation>>.Fail(ErrorCode.AccessError, "Необходимо авторизоваться");

            var pageIndex = request.PageIndex;
            var pageSize = request.PageSize;
            var pagingError = _pagingValidator.Validate(pageIndex, pageSize);
            if (!pagingError.Success)
                return CommandResult<PagedList<Invitation>>.Fail(pagingError.ErrorCode, pagingError.Message);

            _pagingValidator.Normalize(ref pageIndex, ref pageSize);
            request.PageIndex = pageIndex;
            request.PageSize = pageSize;

            var invitations = await _invitationsRepository.SearchInvitationsAsync(request);
            var visible = new List<Invitation>();
            foreach (var invitation in invitations.Result ?? Enumerable.Empty<Invitation>())
            {
                if (await _invitationAccessValidator.CanViewInvitationAsync(invitation, _accountDataHolder.AccountId))
                    visible.Add(invitation);
            }

            var filtered = new PagedList<Invitation>(
                visible.Count,
                visible,
                request.PageIndex ?? 0,
                request.PageSize ?? visible.Count);

            logger.Debug(correlationId, null, methodName, $"Method finished", null, execTime.Elapsed);
            return new CommandResult<PagedList<Invitation>>(filtered);
        }

        public async Task<CommandResult<int>> GetNotViewedInvitationsCountAsync()
        {
            var correlationId = _correlationIdProvider.Get();
            var execTime = Stopwatch.StartNew();
            var methodName = $"{LOGGER_NAME}{nameof(GetNotViewedInvitationsCountAsync)}";

            logger.Debug(correlationId, null, methodName, $"Method started", null);

            var notViewedInvitationsCount = await _invitationsRepository.GetNotViewedInvitationsCountAsync(_accountDataHolder.AccountId.Value);

            logger.Debug(correlationId, null, methodName, $"Method finished", null, execTime.Elapsed);
            return new CommandResult<int>(notViewedInvitationsCount);
        }

        public async Task<CommandResult> ViewInvitationAsync(Guid invitationId)
        {
            var correlationId = _correlationIdProvider.Get();
            var execTime = Stopwatch.StartNew();
            var methodName = $"{LOGGER_NAME}{nameof(ViewInvitationAsync)}";

            logger.Debug(correlationId, null, methodName, $"Method started", null);

            var invitation = await _invitationsRepository.GetInvitationAsync(invitationId);
            if (invitation == null)
                return CommandResult.Fail(ErrorCode.InvitationNotFound, $"Приглашение с id='{invitationId}' не найдено");

            var accessError = await _invitationAccessValidator.AssertCanAcceptOrDeclineAsync(
                invitation, _accountDataHolder.AccountId);
            if (!accessError.Success)
                return CommandResult.Fail(ErrorCode.AccessError, "Пометить приглашение прочитанным может только приглашённый");

            await _invitationsRepository.ViewInvitationAsync(invitationId);

            logger.Debug(correlationId, null, methodName, $"Method finished", null, execTime.Elapsed);
            return CommandResult.OK;
        }

        public async Task<CommandResult> ViewAllInvitationsAsync()
        {
            var correlationId = _correlationIdProvider.Get();
            var execTime = Stopwatch.StartNew();
            var methodName = $"{LOGGER_NAME}{nameof(ViewAllInvitationsAsync)}";

            logger.Debug(correlationId, null, methodName, $"Method started", null);

            await _invitationsRepository.ViewAllInvitationsAsync(_accountDataHolder.AccountId.Value);

            logger.Debug(correlationId, null, methodName, $"Method finished", null, execTime.Elapsed);
            return CommandResult.OK;
        }
    }
}
