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
        private const string LOGGER_NAME = "EList.Services.Impl.SubscriptionsService.";
        #endregion

        private readonly ICorrelationIdProvider _correlationIdProvider;
        private readonly ISubscriptionsRepository _subscriptionsRepository;
        private readonly IAccountDataHolder _accountDataHolder;
        private readonly INotificationsService _notificationsService;
        private readonly ISubscriptionAccessValidator _subscriptionAccessValidator;

        public SubscriptionsService(
            ICorrelationIdProvider correlationIdProvider,
            ISubscriptionsRepository subscriptionsRepository,
            IAccountDataHolder accountDataHolder,
            INotificationsService notificationsService,
            ISubscriptionAccessValidator subscriptionAccessValidator)
        {
            _correlationIdProvider = correlationIdProvider ?? throw new ArgumentNullException(nameof(correlationIdProvider));
            _subscriptionsRepository = subscriptionsRepository ?? throw new ArgumentNullException(nameof(subscriptionsRepository));
            _notificationsService = notificationsService ?? throw new ArgumentNullException(nameof(notificationsService));
            _subscriptionAccessValidator = subscriptionAccessValidator ?? throw new ArgumentNullException(nameof(subscriptionAccessValidator));
            _accountDataHolder = accountDataHolder;
        }

        public async Task<CommandResult> SubscribeToAccountAsync(Guid subscribeToId)
        {
            var correlationId = _correlationIdProvider.Get();
            var execTime = Stopwatch.StartNew();
            var methodName = $"{LOGGER_NAME}{nameof(SubscribeToAccountAsync)}";

            logger.Debug(correlationId, null, methodName, $"Method started", null);

            if (_accountDataHolder.AccountId == null)
                return CommandResult.Fail(ErrorCode.AccessError, "Необходимо авторизоваться");

            var accessError = await _subscriptionAccessValidator.AssertCanSubscribeAsync(
                _accountDataHolder.AccountId.Value, subscribeToId);
            if (!accessError.Success)
                return accessError;

            await _subscriptionsRepository.SubscribeToAccountAsync(_accountDataHolder.AccountId.Value, subscribeToId);

            await _notificationsService.NotifySubscribedAsync(subscribeToId);

            logger.Debug(correlationId, null, methodName, $"Method finished", null, execTime.Elapsed);
            return CommandResult.OK;
        }

        public async Task<CommandResult> UpdateSubscriptionAsync(Guid subscribedToId, UpdateSubscriptionRequestBase request)
        {
            var correlationId = _correlationIdProvider.Get();
            var execTime = Stopwatch.StartNew();
            var methodName = $"{LOGGER_NAME}{nameof(UpdateSubscriptionAsync)}";

            logger.Debug(correlationId, null, methodName, $"Method started", null);

            if (_accountDataHolder.AccountId == null)
                return CommandResult.Fail(ErrorCode.AccessError, "Необходимо авторизоваться");

            var accessError = await _subscriptionAccessValidator.AssertCanManageOwnSubscriptionAsync(
                _accountDataHolder.AccountId.Value, subscribedToId);
            if (!accessError.Success)
                return accessError;

            await _subscriptionsRepository.UpdateSubscriptionAsync(new UpdateSubscriptionRequest
            {
                SubscribedToId = subscribedToId,
                SubscriberId = _accountDataHolder.AccountId.Value,
                NotifyEventCreated = request.NotifyEventCreated,
                NotifyParticipated = request.NotifyParticipated,
                NotifySubscribed = request.NotifySubscribed
            });

            logger.Debug(correlationId, null, methodName, $"Method finished", null, execTime.Elapsed);
            return CommandResult.OK;
        }

        public async Task<CommandResult<PagedList<Subscription>?>> GetSubscriptionsAsync(SubscriptionsSearchRequest request)
        {
            var correlationId = _correlationIdProvider.Get();
            var execTime = Stopwatch.StartNew();
            var methodName = $"{LOGGER_NAME}{nameof(GetSubscriptionsAsync)}";

            logger.Debug(correlationId, null, methodName, $"Method started", null);

            var accessError = await _subscriptionAccessValidator.AssertCanViewSubscriptionsAsync(
                request.AccountId, _accountDataHolder.AccountId);
            if (!accessError.Success)
                return CommandResult<PagedList<Subscription>?>.Fail(accessError.ErrorCode, accessError.Message);

            var subscriptions = await _subscriptionsRepository.GetSubscriptionsAsync(request);

            logger.Debug(correlationId, null, methodName, $"Method finished", null, execTime.Elapsed);
            return new CommandResult<PagedList<Subscription>?>(subscriptions);
        }

        public async Task<CommandResult<int>> GetSubscriptionsCountAsync(Guid accountId)
        {
            var correlationId = _correlationIdProvider.Get();
            var execTime = Stopwatch.StartNew();
            var methodName = $"{LOGGER_NAME}{nameof(GetSubscriptionsCountAsync)}";

            logger.Debug(correlationId, null, methodName, $"Method started", null);

            var accessError = await _subscriptionAccessValidator.AssertCanViewSubscriptionsAsync(
                accountId, _accountDataHolder.AccountId);
            if (!accessError.Success)
                return CommandResult<int>.Fail(accessError.ErrorCode, accessError.Message);

            var subscriptionsCount = await _subscriptionsRepository.GetSubscriptionsCountAsync(accountId);

            logger.Debug(correlationId, null, methodName, $"Method finished", null, execTime.Elapsed);
            return new CommandResult<int>(subscriptionsCount);
        }

        public async Task<CommandResult<PagedList<Subscription>?>> GetSubscribersAsync(SubscriptionsSearchRequest request)
        {
            var correlationId = _correlationIdProvider.Get();
            var execTime = Stopwatch.StartNew();
            var methodName = $"{LOGGER_NAME}{nameof(GetSubscribersAsync)}";

            logger.Debug(correlationId, null, methodName, $"Method started", null);

            var accessError = await _subscriptionAccessValidator.AssertCanViewSubscriptionsAsync(
                request.AccountId, _accountDataHolder.AccountId);
            if (!accessError.Success)
                return CommandResult<PagedList<Subscription>?>.Fail(accessError.ErrorCode, accessError.Message);

            var subscriptions = await _subscriptionsRepository.GetSubscribersAsync(request);

            logger.Debug(correlationId, null, methodName, $"Method finished", null, execTime.Elapsed);
            return new CommandResult<PagedList<Subscription>?>(subscriptions);
        }

        public async Task<CommandResult<int>> GetSubscribersCountAsync(Guid accountId)
        {
            var correlationId = _correlationIdProvider.Get();
            var execTime = Stopwatch.StartNew();
            var methodName = $"{LOGGER_NAME}{nameof(GetSubscribersCountAsync)}";

            logger.Debug(correlationId, null, methodName, $"Method started", null);

            var accessError = await _subscriptionAccessValidator.AssertCanViewSubscriptionsAsync(
                accountId, _accountDataHolder.AccountId);
            if (!accessError.Success)
                return CommandResult<int>.Fail(accessError.ErrorCode, accessError.Message);

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

            if (_accountDataHolder.AccountId == null)
                return CommandResult.Fail(ErrorCode.AccessError, "Необходимо авторизоваться");

            var accessError = await _subscriptionAccessValidator.AssertCanManageOwnSubscriptionAsync(
                _accountDataHolder.AccountId.Value, subscribedToId);
            if (!accessError.Success)
                return accessError;

            await _subscriptionsRepository.DeleteSubscriptionAsync(_accountDataHolder.AccountId.Value, subscribedToId);

            await _notificationsService.NotifyUnsubscribedAsync(subscribedToId);

            logger.Debug(correlationId, null, methodName, $"Method finished", null, execTime.Elapsed);
            return CommandResult.OK;
        }
    }
}
