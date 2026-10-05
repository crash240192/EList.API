using System.Diagnostics;
using EList.Common.CorrelationId;
using EList.Common.Logger;
using EList.Common.Models;
using EList.Common.Support;
using EList.Models.Enums;
using EList.Models.Events;
using EList.Models.Invitations;
using EList.Models.Person;
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
        private readonly IEventAccessValidator _eventAccessValidator;
        private readonly IPagingValidator _pagingValidator;
        private readonly IProfilePrivacyService _profilePrivacyService;
        private readonly IPersonsRepository _personsRepository;

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
            IEventAccessValidator eventAccessValidator,
            IPagingValidator pagingValidator,
            IProfilePrivacyService profilePrivacyService,
            IPersonsRepository personsRepository)
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
            _eventAccessValidator = eventAccessValidator ?? throw new ArgumentNullException(nameof(eventAccessValidator));
            _pagingValidator = pagingValidator ?? throw new ArgumentNullException(nameof(pagingValidator));
            _profilePrivacyService = profilePrivacyService ?? throw new ArgumentNullException(nameof(profilePrivacyService));
            _personsRepository = personsRepository ?? throw new ArgumentNullException(nameof(personsRepository));
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
                return CommandResult.Fail(ErrorCode.EventCancelled, "Мероприятие отменено");

            if (curEvent.Parameters?.MaxPersonsCount > 0)
            {
                var participantsCount = await _participationsRepository.GetParticipantsCountAsync(curEvent.Id);
                if (participantsCount >= curEvent.Parameters.MaxPersonsCount)
                    return CommandResult.Fail(ErrorCode.EventIsFull, "Мероприятие заполнено");
            }

            var inviterId = _accountDataHolder.AccountId!.Value;
            var someInvitationsFiltered = false;
            var messages = new List<string>();
            var allowedIds = new List<Guid>();

            foreach (var accountId in (request.AccountIds ?? new List<Guid>()).Distinct())
            {
                var eligibility = await EvaluateInviteeForEventAsync(
                    inviterId,
                    accountId,
                    curEvent,
                    checkPrivacy: true,
                    checkAlreadyInvitedOrParticipating: true,
                    checkBwLists: true,
                    checkAgeGender: true);

                if (eligibility.Allowed)
                {
                    allowedIds.Add(accountId);
                }
                else
                {
                    someInvitationsFiltered = true;
                    if (!string.IsNullOrWhiteSpace(eligibility.Reason) && !messages.Contains(eligibility.Reason!))
                        messages.Add(eligibility.Reason!);
                }
            }

            request.AccountIds = allowedIds;
            var message = messages.Count == 0
                ? string.Empty
                : (messages.Count == 1
                    ? messages[0]
                    : "Некоторым пользователям нельзя отправить приглашение: " + string.Join("; ", messages));

            if (!request.AccountIds?.Any() ?? true)
                return CommandResult.Fail(ErrorCode.IsNullOrEmpty, message.Length > 0 ? message : "Список пользователей пуст");

            await _invitationsRepository.CreateInvitationsAsync(request, inviterId);

            await _notificationsService.NotifyUsersInvitedAsync(request.EventId, request.AccountIds);

            logger.Debug(correlationId, null, methodName, $"Method finished", null, execTime.Elapsed);

            var result = CommandResult.OK;
            if (someInvitationsFiltered)
                result.Message = message;
            return result;
        }

        public async Task<CommandResult<CreateInvitationsToAccountResult>> CreateToAccountAsync(
            CreateInvitationsToAccountRequest request)
        {
            var correlationId = _correlationIdProvider.Get();
            var execTime = Stopwatch.StartNew();
            var methodName = $"{LOGGER_NAME}{nameof(CreateToAccountAsync)}";
            logger.Debug(correlationId, null, methodName, "Method started", null);

            var dataError = _invitationDataValidator.ValidateCreateToAccountRequest(request);
            if (!dataError.Success)
                return CommandResult<CreateInvitationsToAccountResult>.Fail(dataError.ErrorCode, dataError.Message);

            if (_accountDataHolder.AccountId == null)
                return CommandResult<CreateInvitationsToAccountResult>.Fail(ErrorCode.AccessError, "Необходимо авторизоваться");

            var inviterId = _accountDataHolder.AccountId.Value;
            var outcome = new CreateInvitationsToAccountResult();
            var distinctEventIds = request.EventIds.Distinct().Take(100).ToList();

            foreach (var eventId in distinctEventIds)
            {
                var eligibilityResult = await AssertCanInviteToEventAsync(
                    inviterId,
                    request.InvitedAccountId,
                    eventId,
                    request.InviterOrganizationId);

                if (!eligibilityResult.Success || eligibilityResult.Result == null || !eligibilityResult.Result.Allowed)
                {
                    outcome.Failures.Add(new InvitationToAccountFailure
                    {
                        EventId = eventId,
                        ErrorCode = eligibilityResult.Result?.ErrorCode
                            ?? (eligibilityResult.Success ? (int)ErrorCode.InvitationForbidden : eligibilityResult.ErrorCode),
                        Message = eligibilityResult.Result?.Reason
                            ?? eligibilityResult.Message
                            ?? "Не удалось отправить приглашение"
                    });
                    continue;
                }

                var single = await CreateAsync(new CreateInvitationsRequest
                {
                    EventId = eventId,
                    AccountIds = new List<Guid> { request.InvitedAccountId },
                    InviterOrganizationId = request.InviterOrganizationId
                });

                if (single.Success)
                {
                    outcome.SucceededEventIds.Add(eventId);
                }
                else
                {
                    outcome.Failures.Add(new InvitationToAccountFailure
                    {
                        EventId = eventId,
                        ErrorCode = single.ErrorCode,
                        Message = single.Message ?? "Не удалось отправить приглашение"
                    });
                }
            }

            logger.Debug(correlationId, null, methodName, "Method finished", null, execTime.Elapsed);
            return new CommandResult<CreateInvitationsToAccountResult>(outcome);
        }

        public async Task<CommandResult<InviteToEventEligibility>> AssertCanInviteToEventAsync(
            Guid inviterAccountId,
            Guid inviteeAccountId,
            Guid eventId,
            Guid? inviterOrganizationId = null)
        {
            var curEvent = await _eventsRepository.GetEventAsync(eventId);
            if (curEvent == null)
            {
                return new CommandResult<InviteToEventEligibility>(new InviteToEventEligibility
                {
                    EventId = eventId,
                    AccountId = inviteeAccountId,
                    Allowed = false,
                    Reason = "Мероприятие не найдено",
                    ErrorCode = (int)ErrorCode.EventNotFound
                });
            }

            var createAccess = await _invitationAccessValidator.AssertCanCreateInvitationsAsync(
                curEvent, inviterAccountId, inviterOrganizationId);
            if (!createAccess.Success)
            {
                return new CommandResult<InviteToEventEligibility>(Deny(
                    eventId, inviteeAccountId, createAccess.Message ?? "Нет прав на приглашение", createAccess.ErrorCode));
            }

            if (curEvent.Active == false)
            {
                return new CommandResult<InviteToEventEligibility>(Deny(
                    eventId, inviteeAccountId, "Мероприятие отменено", ErrorCode.EventCancelled,
                    ticketsRequired: curEvent.Parameters?.TicketsEnabled == true));
            }

            if (curEvent.Parameters?.MaxPersonsCount > 0)
            {
                var participantsCount = await _participationsRepository.GetParticipantsCountAsync(curEvent.Id);
                if (participantsCount >= curEvent.Parameters.MaxPersonsCount)
                {
                    return new CommandResult<InviteToEventEligibility>(Deny(
                        eventId, inviteeAccountId,
                        "Мероприятие заполнено",
                        ErrorCode.EventIsFull,
                        ticketsRequired: curEvent.Parameters?.TicketsEnabled == true));
                }
            }

            var inviteeCheck = await EvaluateInviteeForEventAsync(
                inviterAccountId,
                inviteeAccountId,
                curEvent,
                checkPrivacy: true,
                checkAlreadyInvitedOrParticipating: true,
                checkBwLists: true,
                checkAgeGender: true);

            return new CommandResult<InviteToEventEligibility>(inviteeCheck);
        }

        public async Task<CommandResult<List<InviteToEventEligibility>>> CanInviteToEventByEventAsync(
            CanInviteToEventByEventRequest request)
        {
            if (_accountDataHolder.AccountId == null)
                return CommandResult<List<InviteToEventEligibility>>.Fail(ErrorCode.AccessError, "Необходимо авторизоваться");

            if (request == null || request.EventId == Guid.Empty)
                return CommandResult<List<InviteToEventEligibility>>.Fail(ErrorCode.IsNullOrEmpty, "Не указано мероприятие");

            var ids = (request.AccountIds ?? new List<Guid>())
                .Where(id => id != Guid.Empty)
                .Distinct()
                .Take(100)
                .ToList();

            var inviterId = _accountDataHolder.AccountId.Value;
            var results = new List<InviteToEventEligibility>(ids.Count);
            foreach (var accountId in ids)
            {
                var item = await AssertCanInviteToEventAsync(
                    inviterId, accountId, request.EventId, request.InviterOrganizationId);
                results.Add(item.Result ?? Deny(
                    request.EventId, accountId, item.Message ?? "Ошибка проверки", item.ErrorCode));
            }

            return new CommandResult<List<InviteToEventEligibility>>(results);
        }

        public async Task<CommandResult<List<InviteToEventEligibility>>> CanInviteToEventByAccountAsync(
            CanInviteToEventByAccountRequest request)
        {
            if (_accountDataHolder.AccountId == null)
                return CommandResult<List<InviteToEventEligibility>>.Fail(ErrorCode.AccessError, "Необходимо авторизоваться");

            if (request == null || request.AccountId == Guid.Empty)
                return CommandResult<List<InviteToEventEligibility>>.Fail(ErrorCode.IsNullOrEmpty, "Не указан аккаунт");

            var eventIds = (request.EventIds ?? new List<Guid>())
                .Where(id => id != Guid.Empty)
                .Distinct()
                .Take(100)
                .ToList();

            var inviterId = _accountDataHolder.AccountId.Value;
            var results = new List<InviteToEventEligibility>(eventIds.Count);
            foreach (var eventId in eventIds)
            {
                var item = await AssertCanInviteToEventAsync(
                    inviterId, request.AccountId, eventId, request.InviterOrganizationId);
                results.Add(item.Result ?? Deny(
                    eventId, request.AccountId, item.Message ?? "Ошибка проверки", item.ErrorCode));
            }

            return new CommandResult<List<InviteToEventEligibility>>(results);
        }

        private async Task<InviteToEventEligibility> EvaluateInviteeForEventAsync(
            Guid inviterAccountId,
            Guid inviteeAccountId,
            Event curEvent,
            bool checkPrivacy,
            bool checkAlreadyInvitedOrParticipating,
            bool checkBwLists,
            bool checkAgeGender)
        {
            var ticketsRequired = curEvent.Parameters?.TicketsEnabled == true;

            if (inviteeAccountId == Guid.Empty)
                return Deny(curEvent.Id, inviteeAccountId, "Некорректный аккаунт", ErrorCode.InvalidValue, ticketsRequired);

            if (inviterAccountId == inviteeAccountId)
                return Deny(curEvent.Id, inviteeAccountId, "Нельзя пригласить самого себя", ErrorCode.InvitationForbidden, ticketsRequired);

            if (checkPrivacy)
            {
                var privacy = await _profilePrivacyService.AssertCanSendInvitationAsync(inviterAccountId, inviteeAccountId);
                if (!privacy.Success)
                    return Deny(curEvent.Id, inviteeAccountId, privacy.Message ?? "Приглашение запрещено настройками приватности", privacy.ErrorCode, ticketsRequired);
            }

            if (checkAlreadyInvitedOrParticipating)
            {
                if (await _invitationsRepository.IsUserInvitatedAsync(inviteeAccountId, curEvent.Id))
                    return Deny(curEvent.Id, inviteeAccountId, "Уже приглашён", ErrorCode.InvitationForbidden, ticketsRequired);

                if (await _participationsRepository.IsUserParticipatedAsync(inviteeAccountId, curEvent.Id))
                    return Deny(curEvent.Id, inviteeAccountId, "Уже участвует", ErrorCode.InvitationForbidden, ticketsRequired);
            }

            if (checkBwLists)
            {
                if (curEvent.Parameters?.Private == true)
                {
                    var whiteListIsEmpty = await _participantsBWListRepository.IsWhiteListEmptyAsync(curEvent.Id);
                    if (!whiteListIsEmpty
                        && !await _participantsBWListRepository.IsUserInWhiteListAsync(curEvent.Id, inviteeAccountId))
                    {
                        return Deny(curEvent.Id, inviteeAccountId, "Пользователя нет в белом списке", ErrorCode.InvitationForbidden, ticketsRequired);
                    }
                }
                else if (await _participantsBWListRepository.IsUserInBlackListAsync(curEvent.Id, inviteeAccountId))
                {
                    return Deny(curEvent.Id, inviteeAccountId, "Пользователь в чёрном списке мероприятия", ErrorCode.InvitationForbidden, ticketsRequired);
                }
            }

            if (checkAgeGender)
            {
                var person = await _personsRepository.GetPersonInfoAsync(inviteeAccountId);
                var ageGender = AssertAgeAndGender(curEvent, person);
                if (!ageGender.Success)
                    return Deny(curEvent.Id, inviteeAccountId, ageGender.Message ?? "Не подходит по возрасту или полу", ageGender.ErrorCode, ticketsRequired);
            }

            // Бан модерации invitee — на create/eligibility, не только на accept.
            var participateBan = await _moderationPenaltiesService.AssertNotRestrictedAsync(
                inviteeAccountId, ModerationPenaltyType.BanEventParticipate);
            if (!participateBan.Success)
            {
                return Deny(
                    curEvent.Id,
                    inviteeAccountId,
                    participateBan.Message ?? "Пользователю запрещено участвовать в мероприятиях",
                    participateBan.ErrorCode,
                    ticketsRequired);
            }

            var eventBan = await _moderationPenaltiesService.AssertNotRestrictedAsync(
                inviteeAccountId, ModerationPenaltyType.BanFromEvent, curEvent.Id);
            if (!eventBan.Success)
            {
                return Deny(
                    curEvent.Id,
                    inviteeAccountId,
                    eventBan.Message ?? "Пользователю запрещено участвовать в этом мероприятии",
                    eventBan.ErrorCode,
                    ticketsRequired);
            }

            return new InviteToEventEligibility
            {
                EventId = curEvent.Id,
                AccountId = inviteeAccountId,
                Allowed = true,
                Reason = null,
                ErrorCode = 0,
                TicketsRequired = ticketsRequired
            };
        }

        private static CommandResult AssertAgeAndGender(Event curEvent, PersonInfo? person)
        {
            var ageLimit = curEvent.Parameters?.AgeLimit ?? 0;
            if (ageLimit > 0)
            {
                if (person?.BirthDate == null)
                    return CommandResult.Fail(ErrorCode.InvitationForbidden, "Не указан возраст приглашаемого");

                var ageYears = CalculateAgeYears(person.BirthDate.Value);
                if (ageYears == null || ageYears < ageLimit)
                    return CommandResult.Fail(
                        ErrorCode.InvitationForbidden,
                        $"Мероприятие доступно с {ageLimit}+ лет");
            }

            var allowedGender = curEvent.Parameters?.AllowedGender;
            if (allowedGender != null)
            {
                if (person?.Gender == null)
                    return CommandResult.Fail(ErrorCode.InvitationForbidden, "Не указан пол приглашаемого");

                if (person.Gender != allowedGender)
                {
                    var label = allowedGender == Gender.Male ? "мужчин" : "женщин";
                    return CommandResult.Fail(ErrorCode.InvitationForbidden, $"Мероприятие только для {label}");
                }
            }

            return CommandResult.OK;
        }

        private static int? CalculateAgeYears(DateTime birthDate)
        {
            var today = DateTime.UtcNow.Date;
            var birth = birthDate.Date;
            var age = today.Year - birth.Year;
            if (birth > today.AddYears(-age))
                age--;
            return age < 0 ? null : age;
        }

        private static InviteToEventEligibility Deny(
            Guid eventId,
            Guid accountId,
            string reason,
            ErrorCode errorCode,
            bool ticketsRequired = false)
            => Deny(eventId, accountId, reason, (int)errorCode, ticketsRequired);

        private static InviteToEventEligibility Deny(
            Guid eventId,
            Guid accountId,
            string reason,
            int errorCode,
            bool ticketsRequired = false)
        {
            return new InviteToEventEligibility
            {
                EventId = eventId,
                AccountId = accountId,
                Allowed = false,
                Reason = reason,
                ErrorCode = errorCode,
                TicketsRequired = ticketsRequired
            };
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
                return CommandResult.Fail(ErrorCode.EventCancelled, "Мероприятие отменено");

            // Модель A: TicketsEnabled — приглашение не даёт бесплатный вход; нужен билет.
            if (curEvent.Parameters?.TicketsEnabled == true)
            {
                return CommandResult.Fail(ErrorCode.OrganizationPaymentRequired,
                    "Для участия нужно купить билет");
            }

            var participateBan = await _moderationPenaltiesService.AssertNotRestrictedAsync(
                _accountDataHolder.AccountId.Value, EList.Models.Enums.ModerationPenaltyType.BanEventParticipate);
            if (!participateBan.Success)
                return CommandResult.Fail(participateBan.ErrorCode, participateBan.Message);

            var eventBan = await _moderationPenaltiesService.AssertNotRestrictedAsync(
                _accountDataHolder.AccountId.Value, EList.Models.Enums.ModerationPenaltyType.BanFromEvent, invitation.EventId);
            if (!eventBan.Success)
                return CommandResult.Fail(eventBan.ErrorCode, eventBan.Message);

            // Согласовано с ParticipateAsync / AssertCanJoinEventAsync:
            // пустой WL → достаточно приглашения; непустой WL → только из списка.
            var joinAccess = await _eventAccessValidator.AssertCanJoinEventAsync(
                curEvent,
                _accountDataHolder.AccountId.Value,
                hasProvenInvitation: true);
            if (!joinAccess.Success)
                return joinAccess;

            if (curEvent.Parameters?.MaxPersonsCount > 0)
            {
                var participantsCount = await _participationsRepository.GetParticipantsCountAsync(curEvent.Id);
                if (participantsCount >= curEvent.Parameters.MaxPersonsCount)
                    return CommandResult.Fail(ErrorCode.EventIsFull, "Мероприятие заполнено");
            }

            await _participationsRepository.ParticipateAsync(_accountDataHolder.AccountId.Value, invitation.EventId);

            await _invitationsRepository.DeleteInvitationAsync(invitationId);

            // InvitationAccepted + Participated (без дубля inviter/organizers) — внутри NotifyInvitationAcceptedAsync.
            await _notificationsService.NotifyInvitationAcceptedAsync(
                invitation.EventId,
                invitation.InvitedAccountId,
                invitation.InviterAccountId);

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
