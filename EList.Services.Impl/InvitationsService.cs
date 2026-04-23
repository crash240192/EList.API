using EList.Common.CorrelationId;
using EList.Common.Logger;
using EList.Common.Models;
using EList.Common.Support;
using EList.Models.Invitations;
using EList.Repositories.Interfaces;
using EList.Services.Interfaces;
using NLog;
using System.Diagnostics;

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

        public InvitationsService(ICorrelationIdProvider correlationIdProvider,
            IAccountsRepository accountsRepository,
            IAuthorizationRepository authorizationRepository,
            IEventsRepository eventsRepository,
            IEventsMetadataRepository eventsMetadataRepository,
            IInvitationsRepository invitationsRepository,
            IEventOrganizatorsRepository eventOrganizatorsRepository,
            IParticipationsRepository participationsRepository,
            IAccountDataHolder accountDataHolder)
        {
            _correlationIdProvider = correlationIdProvider ?? throw new ArgumentNullException(nameof(correlationIdProvider));
            _accountsRepository = accountsRepository ?? throw new ArgumentNullException(nameof(accountsRepository));
            _authorizationRepository = authorizationRepository ?? throw new ArgumentNullException(nameof(authorizationRepository));
            _eventsRepository = eventsRepository ?? throw new ArgumentNullException(nameof(eventsRepository));
            _eventsMetadataRepository = eventsMetadataRepository ?? throw new ArgumentNullException(nameof(eventsMetadataRepository));
            _invitationsRepository = invitationsRepository ?? throw new ArgumentNullException(nameof(invitationsRepository));
            _eventOrganizatorsRepository = eventOrganizatorsRepository ?? throw new ArgumentNullException(nameof(eventOrganizatorsRepository));
            _participationsRepository = participationsRepository ?? throw new ArgumentNullException(nameof(participationsRepository));
            _accountDataHolder = accountDataHolder;
        }

        public async Task<CommandResult> CreateAsync(CreateInvitationsRequest request)
        {
            var correlationId = _correlationIdProvider.Get();
            var execTime = Stopwatch.StartNew();
            var methodName = $"{LOGGER_NAME}{nameof(CreateAsync)}";

            logger.Debug(correlationId, null, methodName, $"Method started", null);

            var curEvent = await _eventsRepository.GetEventAsync(request.EventId);
            if (curEvent == null)
                return CommandResult.Fail(ErrorCode.EventNotFound, $"Мероприятие с id='{request.EventId}' не найдено");

            if (request.InviterOrganizationId != null)
            {
                //TODO Проверить, входит ли текущий аккаунт в состав организации
                //TODO Проверить, является ли текущая организация организатором для данного мероприятия
            }

            var eventParameters = await _eventsMetadataRepository.GetEventParametersAsync(request.EventId);
            if (!eventParameters?.AllowUsersToInvite ?? false)
            {
                var organizators = await _eventOrganizatorsRepository.GetByEventIdAsync(request.EventId);
                if (!organizators?.Any(i => i.Account?.Id == _accountDataHolder.AccountId) ?? true)
                    return CommandResult.Fail(ErrorCode.AccessError, $"Приглашения на текущее события запрещены администратором");

                //TODO Сделать аналогичную проверку для организаций
            }

            //TODO: Осуществить проверку для каждого пользователя, можно ли его пригласить в указанный ивент, исходя из параметров этого ивента

            await _invitationsRepository.CreateInvitationsAsync(request, _accountDataHolder.AccountId);

            logger.Debug(correlationId, null, methodName, $"Method finished", null, execTime.Elapsed);
            return CommandResult.OK;
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
    }
}
