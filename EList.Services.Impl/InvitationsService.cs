using System.Diagnostics;
using EList.Common.CorrelationId;
using EList.Common.Logger;
using EList.Common.Models;
using EList.Common.Support;
using EList.Models.Invitations;
using EList.Repositories.Interfaces;
using EList.Services.Interfaces;
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
        private readonly IAccountsRepository _accountsRepository;
        private readonly IAuthorizationRepository _authorizationRepository;
        private readonly IEventsRepository _eventsRepository;
        private readonly IEventsMetadataRepository _eventsMetadataRepository;
        private readonly IInvitationsRepository _invitationsRepository;
        private readonly IEventOrganizatorsRepository _eventOrganizatorsRepository;
        private readonly IParticipationsRepository _participationsRepository;
        private readonly IAccountDataHolder _accountDataHolder;
        private readonly IParticipantsBWListRepository _participantsBWListRepository;
        private readonly INotificationsService _notificationsService;

        public InvitationsService(ICorrelationIdProvider correlationIdProvider,
            IAccountsRepository accountsRepository,
            IAuthorizationRepository authorizationRepository,
            IEventsRepository eventsRepository,
            IEventsMetadataRepository eventsMetadataRepository,
            IInvitationsRepository invitationsRepository,
            IEventOrganizatorsRepository eventOrganizatorsRepository,
            IParticipationsRepository participationsRepository,
            IAccountDataHolder accountDataHolder,
            IParticipantsBWListRepository participantsBWListRepository,
            INotificationsService notificationsService)
        {
            _correlationIdProvider = correlationIdProvider ?? throw new ArgumentNullException(nameof(correlationIdProvider));
            _accountsRepository = accountsRepository ?? throw new ArgumentNullException(nameof(accountsRepository));
            _authorizationRepository = authorizationRepository ?? throw new ArgumentNullException(nameof(authorizationRepository));
            _eventsRepository = eventsRepository ?? throw new ArgumentNullException(nameof(eventsRepository));
            _eventsMetadataRepository = eventsMetadataRepository ?? throw new ArgumentNullException(nameof(eventsMetadataRepository));
            _invitationsRepository = invitationsRepository ?? throw new ArgumentNullException(nameof(invitationsRepository));
            _eventOrganizatorsRepository = eventOrganizatorsRepository ?? throw new ArgumentNullException(nameof(eventOrganizatorsRepository));
            _participationsRepository = participationsRepository ?? throw new ArgumentNullException(nameof(participationsRepository));
            _participantsBWListRepository = participantsBWListRepository ?? throw new ArgumentNullException(nameof(participantsBWListRepository));
            _notificationsService = notificationsService ?? throw new ArgumentNullException(nameof(notificationsService));
            _accountDataHolder = accountDataHolder;
        }

        public async Task<CommandResult> CreateAsync(CreateInvitationsRequest request)
        {
            var correlationId = _correlationIdProvider.Get();
            var execTime = Stopwatch.StartNew();
            var methodName = $"{LOGGER_NAME}{nameof(CreateAsync)}";

            logger.Debug(correlationId, null, methodName, $"Method started", null);

            if (!request.AccountIds?.Any() ?? true)
                return CommandResult.Fail(ErrorCode.IsNullOrEmpty, "Список пользователей пуст");

            var curEvent = await _eventsRepository.GetEventAsync(request.EventId);
            if (curEvent == null)
                return CommandResult.Fail(ErrorCode.EventNotFound, $"Мероприятие с id='{request.EventId}' не найдено");

            var eventOrganizators = await _eventOrganizatorsRepository.GetByEventIdAsync(curEvent.Id);

            var isOrganizator = eventOrganizators?.Any(i => i.Account?.Id == _accountDataHolder.AccountId) ?? false;

            if (!isOrganizator)
            {
                //TODO: Тут надо реализовать проверку что указанным пользователям можно выслать приглашения по black/white list
                //if (curEvent.Parameters?.Private ?? false)
                //{
                //    if (await _participantsBWListRepository.IsUserInWhiteListAsync(request.AccountIds))
                //}

                //if (await _participantsBWListRepository.IsUserInBlackListAsync)
            }

            if (request.InviterOrganizationId != null)
            {
                //TODO Проверить, входит ли текущий аккаунт в состав организации
                //TODO Проверить, является ли текущая организация организатором для данного мероприятия
            }

            if (!curEvent.Parameters?.AllowUsersToInvite ?? false && !isOrganizator)
            {
                return CommandResult.Fail(ErrorCode.AccessError, $"Приглашения на текущее события запрещены администратором");

                //TODO Сделать аналогичную проверку для организаций
            }

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
                    var filteredAccounts = await _participantsBWListRepository.FilterUsersNotInWhiteListAsync(curEvent.Id, request.AccountIds);
                    someInvitationsFiltered = filteredAccounts.Count() != request.AccountIds.Count();
                    request.AccountIds = filteredAccounts;
                    if (someInvitationsFiltered)
                        message = "Некоторых пользователей нет в белом списках. Им не удалось отправить приглашение";
                }
            }
            else
            {
                var filteredAccounts = await _participantsBWListRepository.FilterUsersNotInBlackListAsync(curEvent.Id, request.AccountIds);
                someInvitationsFiltered = filteredAccounts.Count() != request.AccountIds.Count();
                request.AccountIds = filteredAccounts;
                if (someInvitationsFiltered)
                    message = "Некоторые пользователи находятся в чёрном списке. Им не удалось отправить приглашение";
            }
            #endregion

            await _invitationsRepository.CreateInvitationsAsync(request, _accountDataHolder.AccountId);

            // TODO: Выслать уведомления о приглашениях

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

            var curEvent = await _eventsRepository.GetEventAsync(invitation.EventId);
            if (curEvent == null)
                return CommandResult.Fail(ErrorCode.EventNotFound, $"Мероприятие с id='{invitation.EventId}' не найдено");

            if (curEvent.Active == false)
                return CommandResult.Fail(ErrorCode.EventCancelled, $"Мероприятие было отменено");

            if (curEvent.Parameters.Private ?? false)
            {
                if (!await _participantsBWListRepository.IsUserInWhiteListAsync(curEvent.Id, _accountDataHolder.AccountId))
                    return CommandResult<Guid?>.Fail(ErrorCode.AccessError, "Участвовать в закрытом мероприятии могут только пользователи из белого списка");
            }
            else
            {
                if (await _participantsBWListRepository.IsUserInBlackListAsync(curEvent.Id, _accountDataHolder.AccountId))
                    return CommandResult<Guid?>.Fail(ErrorCode.AccessError, "Организатор добавил вас в чёрный список мероприятия");
            }

            if (curEvent.Parameters?.MaxPersonsCount > 0)
            {
                var participantsCount = await _participationsRepository.GetParticipantsCountAsync(curEvent.Id);
                if (participantsCount >= curEvent.Parameters.MaxPersonsCount)
                    return CommandResult.Fail(ErrorCode.EventIsFull, $"В мероприятии уже участвует максимальное количество человек");
            }

            if (invitation.InvitedAccountId != _accountDataHolder.AccountId)
                return CommandResult.Fail(ErrorCode.AccessError, $"У текущего пользователя нет доступа для взаимодействия с этим приглашением");

            await _participationsRepository.ParticipateAsync(_accountDataHolder.AccountId, invitation.EventId);

            await _invitationsRepository.DeleteInvitationAsync(invitationId);

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

            if (invitation.InvitedAccountId != _accountDataHolder.AccountId)
                return CommandResult.Fail(ErrorCode.AccessError, $"У текущего пользователя нет доступа для взаимодействия с этим приглашением");

            await _invitationsRepository.DeleteInvitationAsync(invitationId);

            logger.Debug(correlationId, null, methodName, $"Method finished", null, execTime.Elapsed);
            return CommandResult.OK;
        }

        public async Task<CommandResult> CancelInvitationAsync(Guid invitationId)
        {
            var correlationId = _correlationIdProvider.Get();
            var execTime = Stopwatch.StartNew();
            var methodName = $"{LOGGER_NAME}{nameof(DeclineAsync)}";

            logger.Debug(correlationId, null, methodName, $"Method started", null);

            var invitation = await _invitationsRepository.GetInvitationAsync(invitationId);
            if (invitation == null)
                return CommandResult.Fail(ErrorCode.InvitationNotFound, $"Приглашение с id='{invitationId}' не найдено");

            if (invitation.InviterAccountId != _accountDataHolder.AccountId)
            {
                if (invitation.InviterOrganizationId != null)
                {
                    //TODO: Проверить, является ли текущий пользователь администратором организации, если она указана
                }
                else
                {
                    return CommandResult.Fail(ErrorCode.AccessError, $"У текущего пользователя нет доступа отмены этого приглашения");
                }
            }

            await _invitationsRepository.DeleteInvitationAsync(invitationId);

            logger.Debug(correlationId, null, methodName, $"Method finished", null, execTime.Elapsed);
            return CommandResult.OK;
        }

        public async Task<CommandResult<PagedList<Invitation>>> GetUserInvitationsAsync(int pageIndex = 0, int pageSize = 20)
        {
            var correlationId = _correlationIdProvider.Get();
            var execTime = Stopwatch.StartNew();
            var methodName = $"{LOGGER_NAME}{nameof(GetUserInvitationsAsync)}";

            logger.Debug(correlationId, null, methodName, $"Method started", null);

            var invitations = await _invitationsRepository.SearchInvitationsAsync(new InvitationsSearchRequest
            {
                InvitedAccountIds = new List<Guid> { _accountDataHolder.AccountId },
                PageIndex = pageIndex,
                PageSize = pageSize,
            });

            logger.Debug(correlationId, null, methodName, $"Method finished", null, execTime.Elapsed);
            return new CommandResult<PagedList<Invitation>>(invitations);
        }

        public async Task<CommandResult<PagedList<Invitation>>> SearchAsync(InvitationsSearchRequest request)
        {
            var correlationId = _correlationIdProvider.Get();
            var execTime = Stopwatch.StartNew();
            var methodName = $"{LOGGER_NAME}{nameof(GetUserInvitationsAsync)}";

            logger.Debug(correlationId, null, methodName, $"Method started", null);

            var invitations = await _invitationsRepository.SearchInvitationsAsync(request);

            logger.Debug(correlationId, null, methodName, $"Method finished", null, execTime.Elapsed);
            return new CommandResult<PagedList<Invitation>>(invitations);
        }

        public async Task<CommandResult<int>> GetNotViewedInvitationsCountAsync()
        {
            var correlationId = _correlationIdProvider.Get();
            var execTime = Stopwatch.StartNew();
            var methodName = $"{LOGGER_NAME}{nameof(ViewInvitationAsync)}";

            logger.Debug(correlationId, null, methodName, $"Method started", null);

            var notViewedInvitationsCount = await _invitationsRepository.GetNotViewedInvitationsCountAsync(_accountDataHolder.AccountId);

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

            if (invitation.InvitedAccountId != _accountDataHolder.AccountId)
                return CommandResult.Fail(ErrorCode.AccessError, $"Пометить приглашение прочитанным может только приглашённый");
            
            await _invitationsRepository.ViewInvitationAsync(invitationId);

            logger.Debug(correlationId, null, methodName, $"Method finished", null, execTime.Elapsed);
            return CommandResult.OK;
        }
    }
}
