using EList.Common.CorrelationId;
using EList.Common.Logger;
using EList.Common.Models;
using EList.Common.Support;
using EList.Models.Subscriptions;
using EList.Repositories.Interfaces;
using EList.Services.Interfaces;
using EList.Validators.Interfaces;
using NLog;
using System.Diagnostics;

namespace EList.Services.Impl
{
    public class SubscriptionsService : ISubscriptionsService
    {
        #region logger
        private static readonly ILogger log = LogManager.GetCurrentClassLogger();
        private static readonly ILoggerWrapper logger = new NLogLoggerWrapper(log);
        private const string LOGGER_NAME = "EList.Services.Impl.AccountsService.";
        #endregion

        private readonly ICorrelationIdProvider _correlationIdProvider;
        private readonly IAccountsRepository _accountsRepository;
        private readonly IAuthorizationRepository _authorizationRepository;
        private readonly IContactsRepository _contactsRepository;
        private readonly IUserDataValidator _userDataValidationService;
        private readonly INotificationsService _notificationsService;
        private readonly ISubscriptionsRepository _subscriptionsRepository;
        private readonly IAccountDataHolder _accountDataHolder;
        public SubscriptionsService(ICorrelationIdProvider correlationIdProvider,
            IAccountsRepository accountsRepository,
            IAuthorizationRepository authorizationRepository,
            IContactsRepository contactsRepository,
            IUserDataValidator userDataValidationService,
            INotificationsService notificationsService,
            ISubscriptionsRepository subscriptionsRepository,
            IAccountDataHolder accountDataHolder)
        {
            _correlationIdProvider = correlationIdProvider ?? throw new ArgumentNullException(nameof(correlationIdProvider));
            _accountsRepository = accountsRepository ?? throw new ArgumentNullException(nameof(accountsRepository));
            _authorizationRepository = authorizationRepository ?? throw new ArgumentNullException(nameof(authorizationRepository));
            _contactsRepository = contactsRepository ?? throw new ArgumentNullException(nameof(contactsRepository));
            _userDataValidationService = userDataValidationService ?? throw new ArgumentNullException(nameof(userDataValidationService));
            _notificationsService = notificationsService ?? throw new ArgumentNullException(nameof(notificationsService));
            _subscriptionsRepository = subscriptionsRepository ?? throw new ArgumentNullException(nameof(subscriptionsRepository));
            _accountDataHolder = accountDataHolder;
        }

        public async Task<CommandResult> SubscribeToAccountAsync(Guid subscribeToId)
        {
            var correlationId = _correlationIdProvider.Get();
            var execTime = Stopwatch.StartNew();
            var methodName = $"{LOGGER_NAME}{nameof(SubscribeToAccountAsync)}";

            logger.Debug(correlationId, null, methodName, $"Method started", null);

            var isSubscribed = await _subscriptionsRepository.IsSubscriptionExistAsync(_accountDataHolder.AccountId, subscribeToId);

            if (isSubscribed)
                return CommandResult.Fail(ErrorCode.SubscriptionAlreadyExists, "Подписка уже существует");

            await _subscriptionsRepository.SubscribeToAccountAsync(_accountDataHolder.AccountId, subscribeToId);

            //TODO: Уведомить подписчиков (у которых стоит флаг notify_subscriberd)
            var getSubscription = await _subscriptionsRepository.GetSubscribersAsync(_accountDataHolder.AccountId, null, null, true);

            logger.Debug(correlationId, null, methodName, $"Method finished", null, execTime.Elapsed);
            return CommandResult.OK;
        }

        public async Task<CommandResult> UpdateSubscriptionAsync(Guid subscribedToId, UpdateSubscriptionRequestBase request)
        {
            var correlationId = _correlationIdProvider.Get();
            var execTime = Stopwatch.StartNew();
            var methodName = $"{LOGGER_NAME}{nameof(UpdateSubscriptionAsync)}";

            logger.Debug(correlationId, null, methodName, $"Method started", null);

            var isSubscribed = await _subscriptionsRepository.IsSubscriptionExistAsync(_accountDataHolder.AccountId, subscribedToId);

            if (!isSubscribed)
                return CommandResult.Fail(ErrorCode.SubscriptionAlreadyExists, "Подписка не найдена");

            await _subscriptionsRepository.UpdateSubscriptionAsync(new UpdateSubscriptionRequest
            { 
                SubscribedToId = subscribedToId,
                SubscriberId = _accountDataHolder.AccountId,
                NotifyEventCreated = request.NotifyEventCreated,
                NotifyParticipated = request.NotifyParticipated,
                NotifySubscribed = request.NotifySubscribed
            });

            logger.Debug(correlationId, null, methodName, $"Method finished", null, execTime.Elapsed);
            return CommandResult.OK;
        }

        public async Task<CommandResult<PagedList<Subscription>?>> GetSubscriptionsAsync(Guid accountId)
        {
            var correlationId = _correlationIdProvider.Get();
            var execTime = Stopwatch.StartNew();
            var methodName = $"{LOGGER_NAME}{nameof(GetSubscriptionsAsync)}";

            logger.Debug(correlationId, null, methodName, $"Method started", null);

            var subscriptions = await _subscriptionsRepository.GetSubscriptionsAsync(accountId);

            logger.Debug(correlationId, null, methodName, $"Method finished", null, execTime.Elapsed);
            return new CommandResult<PagedList<Subscription>?>(subscriptions);
        }

        public async Task<CommandResult<int>> GetSubscriptionsCountAsync(Guid accountId)
        {
            var correlationId = _correlationIdProvider.Get();
            var execTime = Stopwatch.StartNew();
            var methodName = $"{LOGGER_NAME}{nameof(GetSubscriptionsCountAsync)}";

            logger.Debug(correlationId, null, methodName, $"Method started", null);

            var subscriptionsCount = await _subscriptionsRepository.GetSubscriptionsCountAsync(accountId);

            logger.Debug(correlationId, null, methodName, $"Method finished", null, execTime.Elapsed);
            return new CommandResult<int>(subscriptionsCount);
        }

        public async Task<CommandResult<PagedList<Subscription>?>> GetSubscribersAsync(Guid accountId)
        {
            var correlationId = _correlationIdProvider.Get();
            var execTime = Stopwatch.StartNew();
            var methodName = $"{LOGGER_NAME}{nameof(GetSubscribersAsync)}";

            logger.Debug(correlationId, null, methodName, $"Method started", null);

            var subscriptions = await _subscriptionsRepository.GetSubscribersAsync(accountId);

            logger.Debug(correlationId, null, methodName, $"Method finished", null, execTime.Elapsed);
            return new CommandResult<PagedList<Subscription>?>(subscriptions);
        }

        public async Task<CommandResult<int>> GetSubscribersCountAsync(Guid accountId)
        {
            var correlationId = _correlationIdProvider.Get();
            var execTime = Stopwatch.StartNew();
            var methodName = $"{LOGGER_NAME}{nameof(GetSubscribersCountAsync)}";

            logger.Debug(correlationId, null, methodName, $"Method started", null);

            var subscriptionsCount = await _subscriptionsRepository.GetSubscribersCountAsync(accountId);

            logger.Debug(correlationId, null, methodName, $"Method finished", null, execTime.Elapsed);
            return new CommandResult<int>(subscriptionsCount);
        }

        public async Task<CommandResult> DeleteSubscriptionAsync(Guid subscribedToId)
        {
            var correlationId = _correlationIdProvider.Get();
            var execTime = Stopwatch.StartNew();
            var methodName = $"{LOGGER_NAME}{nameof(DeleteSubscriptionAsync)}";

            logger.Debug(correlationId, null, methodName, $"Method started", null);
                
            var isSubscribed = await _subscriptionsRepository.IsSubscriptionExistAsync(_accountDataHolder.AccountId, subscribedToId);
            if (!isSubscribed)
                return CommandResult.Fail(ErrorCode.SubscriptionAlreadyExists, "Подписка не найдена");

            await _subscriptionsRepository.DeleteSubscriptionAsync(_accountDataHolder.AccountId, subscribedToId);

            var subscriptions = await _subscriptionsRepository.GetSubscribersAsync(_accountDataHolder.AccountId);

            logger.Debug(correlationId, null, methodName, $"Method finished", null, execTime.Elapsed);
            return CommandResult.OK;
        }
    }
}
