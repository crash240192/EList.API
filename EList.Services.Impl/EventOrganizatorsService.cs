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
        private readonly IModerationPenaltiesService _moderationPenaltiesService;
        private readonly INotificationsService _notificationsService;
        private readonly IOrganizationsRepository _organizationsRepository;

        public EventOrganizatorsService(ICorrelationIdProvider correlationIdProvider,
            IEventsRepository eventsRepository,
            IAccountDataHolder accountDataHolder,
            IEventOrganizatorsRepository organizatorsRepository,
            ISubscriptionsRepository subscriptionsRepository,
            IModerationPenaltiesService moderationPenaltiesService,
            INotificationsService notificationsService,
            IOrganizationsRepository organizationsRepository)
        {
            _correlationIdProvider = correlationIdProvider ?? throw new ArgumentNullException(nameof(correlationIdProvider));
            _eventsRepository = eventsRepository ?? throw new ArgumentNullException(nameof(eventsRepository));
            _accountDataHolder = accountDataHolder;
            _organizatorsRepository = organizatorsRepository ?? throw new ArgumentNullException(nameof(organizatorsRepository));
            _subscriptionsRepository = subscriptionsRepository ?? throw new ArgumentNullException(nameof(subscriptionsRepository));
            _moderationPenaltiesService = moderationPenaltiesService ?? throw new ArgumentNullException(nameof(moderationPenaltiesService));
            _notificationsService = notificationsService ?? throw new ArgumentNullException(nameof(notificationsService));
            _organizationsRepository = organizationsRepository ?? throw new ArgumentNullException(nameof(organizationsRepository));
        }

        public async Task<CommandResult<EventOrganizator?>> GetByIdAsync(Guid id)
        {
            var correlationId = _correlationIdProvider.Get();
            var execTime = Stopwatch.StartNew();
            var methodName = $"{LOGGER_NAME}{nameof(GetByIdAsync)}";
            logger.Debug(correlationId, null, methodName, "Method started", null);

            if (_accountDataHolder.AccountId == null)
                return CommandResult<EventOrganizator?>.Fail(ErrorCode.AccessError, "Необходимо авторизоваться");

            var item = await _organizatorsRepository.GetByIdAsync(id);
            if (item == null)
                return CommandResult<EventOrganizator?>.Fail(ErrorCode.InvalidValue, "Запись организатора не найдена");

            logger.Debug(correlationId, null, methodName, "Method finished", null, execTime.Elapsed);
            return new CommandResult<EventOrganizator?>(item);
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

        public async Task<CommandResult> AssignEventOrganizatorsAsync(Guid eventId, List<Guid> accountIds, List<Guid> organizationIds)
        {
            var correlationId = _correlationIdProvider.Get();
            var execTime = Stopwatch.StartNew();
            var methodName = $"{LOGGER_NAME}{nameof(AssignEventOrganizatorsAsync)}";
            logger.Debug(correlationId, null, methodName, $"Method started", null);

            var curEvent = await _eventsRepository.GetEventAsync(eventId);

            if (curEvent == null)
                return CommandResult.Fail(ErrorCode.EventNotFound, $"Событие с id='{eventId}' не найдено");

            var eventOrganizators = await _organizatorsRepository.GetByEventIdAsync(eventId);
            if (_accountDataHolder.AccountId == null
                || !await _organizatorsRepository.IsAccountEventOrganizatorAsync(eventId, _accountDataHolder.AccountId.Value))
                return CommandResult.Fail(ErrorCode.AccessError, "Пользователь не является организатором события");

            if (accountIds != null)
            {
                foreach (var accountId in accountIds)
                {
                    var organizeBan = await _moderationPenaltiesService.AssertNotRestrictedAsync(
                        accountId, EList.Models.Enums.ModerationPenaltyType.BanOrganize);
                    if (!organizeBan.Success)
                        return CommandResult.Fail(organizeBan.ErrorCode, organizeBan.Message);
                }
            }

            await _organizatorsRepository.AssignAsync(eventId, accountIds, organizationIds);

            var notifyAccountIds = new HashSet<Guid>();
            if (accountIds != null)
            {
                foreach (var accountId in accountIds)
                    notifyAccountIds.Add(accountId);
            }

            if (organizationIds != null)
            {
                foreach (var organizationId in organizationIds)
                {
                    var members = await _organizationsRepository.GetMembersByOrganizationIdAsync(organizationId, onlyActive: true);
                    if (members == null)
                        continue;
                    foreach (var member in members)
                        notifyAccountIds.Add(member.AccountId);
                }
            }

            await _notificationsService.NotifyEventOrganizatorAssignedAsync(eventId, notifyAccountIds.ToList());

            logger.Debug(correlationId, null, methodName, $"Method finished", null, execTime.Elapsed);
            return CommandResult.OK;
        }

        public async Task<CommandResult> RemoveOrganizatorAsync(Guid eventId, Guid organizatorId)
        {
            var correlationId = _correlationIdProvider.Get();
            var execTime = Stopwatch.StartNew();
            var methodName = $"{LOGGER_NAME}{nameof(RemoveOrganizatorAsync)}";
            logger.Debug(correlationId, null, methodName, "Method started", null);

            if (_accountDataHolder.AccountId == null)
                return CommandResult.Fail(ErrorCode.AccessError, "Необходимо авторизоваться");

            var curEvent = await _eventsRepository.GetEventAsync(eventId);
            if (curEvent == null)
                return CommandResult.Fail(ErrorCode.EventNotFound, $"Событие с id='{eventId}' не найдено");

            if (!await _organizatorsRepository.IsAccountEventOrganizatorAsync(eventId, _accountDataHolder.AccountId.Value))
                return CommandResult.Fail(ErrorCode.AccessError, "Пользователь не является организатором события");

            var organizator = await _organizatorsRepository.GetByIdAsync(organizatorId);
            if (organizator == null || organizator.EventId != eventId)
                return CommandResult.Fail(ErrorCode.InvalidValue, "Запись организатора не найдена для этого мероприятия");

            var allOrganizators = await _organizatorsRepository.GetByEventIdAsync(eventId);
            if (allOrganizators == null || allOrganizators.Count <= 1)
                return CommandResult.Fail(ErrorCode.InvalidValue, "Нельзя удалить последнего организатора мероприятия");

            if (organizator.AccountId == _accountDataHolder.AccountId && !_accountDataHolder.IsPlatformModeratorOrAbove)
                return CommandResult.Fail(ErrorCode.InvalidValue, "Нельзя удалить себя из организаторов");

            await _organizatorsRepository.DeleteAsync(organizatorId);

            if (organizator.AccountId != null)
            {
                await _notificationsService.NotifyEventOrganizatorRemovedAsync(eventId, organizator.AccountId.Value);
            }
            else if (organizator.OrganizationId != null)
            {
                var members = await _organizationsRepository.GetMembersByOrganizationIdAsync(
                    organizator.OrganizationId.Value, onlyActive: true);
                if (members != null)
                {
                    foreach (var member in members)
                        await _notificationsService.NotifyEventOrganizatorRemovedAsync(eventId, member.AccountId);
                }
            }

            logger.Debug(correlationId, null, methodName, "Method finished", null, execTime.Elapsed);
            return CommandResult.OK;
        }

        public async Task<CommandResult<bool>> IsCurrentUserEventOrganizatorAsync(Guid eventId)
        {
            var correlationId = _correlationIdProvider.Get();
            var execTime = Stopwatch.StartNew();
            var methodName = $"{LOGGER_NAME}{nameof(IsCurrentUserEventOrganizatorAsync)}";
            logger.Debug(correlationId, null, methodName, $"Method started", null);

            if (_accountDataHolder.AccountId == null)
                return CommandResult<bool>.Fail(ErrorCode.AccessError, "Необходимо авторизоваться");

            var curEvent = await _eventsRepository.GetEventAsync(eventId);
            if (curEvent == null)
                return CommandResult<bool>.Fail(ErrorCode.EventNotFound, $"Событие с id='{eventId}' не найдено");

            var isOrganizator = await _organizatorsRepository.IsAccountEventOrganizatorAsync(eventId, _accountDataHolder.AccountId.Value);

            logger.Debug(correlationId, null, methodName, $"Method finished", null, execTime.Elapsed);
            return new CommandResult<bool>(isOrganizator);
        }
    }
}
