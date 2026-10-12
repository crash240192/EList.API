using System.Diagnostics;
using EList.Common.CorrelationId;
using EList.Common.Logger;
using EList.Common.Models;
using EList.Common.Support;
using EList.Models.Subscriptions;
using EList.Repositories.Interfaces;
using EList.Services.Interfaces;
using EList.Validators.Interfaces;
using NLog;

namespace EList.Services.Impl
{
    public class OrganizationSubscriptionsService : IOrganizationSubscriptionsService
    {
        #region logger
        private static readonly ILogger log = LogManager.GetCurrentClassLogger();
        private static readonly ILoggerWrapper logger = new NLogLoggerWrapper(log);
        private const string LOGGER_NAME = "EList.Services.Impl.OrganizationSubscriptionsService.";
        #endregion

        private readonly ICorrelationIdProvider _correlationIdProvider;
        private readonly IOrganizationSubscriptionsRepository _organizationSubscriptionsRepository;
        private readonly IAccountDataHolder _accountDataHolder;
        private readonly INotificationsService _notificationsService;
        private readonly IOrganizationSubscriptionAccessValidator _accessValidator;
        private readonly IPagingValidator _pagingValidator;

        public OrganizationSubscriptionsService(
            ICorrelationIdProvider correlationIdProvider,
            IOrganizationSubscriptionsRepository organizationSubscriptionsRepository,
            IAccountDataHolder accountDataHolder,
            INotificationsService notificationsService,
            IOrganizationSubscriptionAccessValidator accessValidator,
            IPagingValidator pagingValidator)
        {
            _correlationIdProvider = correlationIdProvider ?? throw new ArgumentNullException(nameof(correlationIdProvider));
            _organizationSubscriptionsRepository = organizationSubscriptionsRepository ?? throw new ArgumentNullException(nameof(organizationSubscriptionsRepository));
            _accountDataHolder = accountDataHolder;
            _notificationsService = notificationsService ?? throw new ArgumentNullException(nameof(notificationsService));
            _accessValidator = accessValidator ?? throw new ArgumentNullException(nameof(accessValidator));
            _pagingValidator = pagingValidator ?? throw new ArgumentNullException(nameof(pagingValidator));
        }

        public async Task<CommandResult> SubscribeAsync(Guid organizationId)
        {
            var correlationId = _correlationIdProvider.Get();
            var execTime = Stopwatch.StartNew();
            var methodName = $"{LOGGER_NAME}{nameof(SubscribeAsync)}";
            logger.Debug(correlationId, null, methodName, "Method started", null);

            if (_accountDataHolder.AccountId == null)
                return CommandResult.Fail(ErrorCode.AccessError, "Необходимо авторизоваться");

            var accessError = await _accessValidator.AssertCanSubscribeAsync(
                _accountDataHolder.AccountId.Value, organizationId);
            if (!accessError.Success)
                return accessError;

            await _organizationSubscriptionsRepository.SubscribeAsync(
                _accountDataHolder.AccountId.Value, organizationId);

            await _notificationsService.NotifyOrganizationSubscribedAsync(organizationId);

            logger.Debug(correlationId, null, methodName, "Method finished", null, execTime.Elapsed);
            return CommandResult.OK;
        }

        public async Task<CommandResult> UpdateAsync(Guid organizationId, UpdateOrganizationSubscriptionRequest request)
        {
            var correlationId = _correlationIdProvider.Get();
            var execTime = Stopwatch.StartNew();
            var methodName = $"{LOGGER_NAME}{nameof(UpdateAsync)}";
            logger.Debug(correlationId, null, methodName, "Method started", null);

            if (_accountDataHolder.AccountId == null)
                return CommandResult.Fail(ErrorCode.AccessError, "Необходимо авторизоваться");

            var accessError = await _accessValidator.AssertCanManageOwnSubscriptionAsync(
                _accountDataHolder.AccountId.Value, organizationId);
            if (!accessError.Success)
                return accessError;

            await _organizationSubscriptionsRepository.UpdateAsync(
                _accountDataHolder.AccountId.Value,
                organizationId,
                request.NotifyEventCreated);

            logger.Debug(correlationId, null, methodName, "Method finished", null, execTime.Elapsed);
            return CommandResult.OK;
        }

        public async Task<CommandResult> DeleteAsync(Guid organizationId)
        {
            var correlationId = _correlationIdProvider.Get();
            var execTime = Stopwatch.StartNew();
            var methodName = $"{LOGGER_NAME}{nameof(DeleteAsync)}";
            logger.Debug(correlationId, null, methodName, "Method started", null);

            if (_accountDataHolder.AccountId == null)
                return CommandResult.Fail(ErrorCode.AccessError, "Необходимо авторизоваться");

            var accessError = await _accessValidator.AssertCanManageOwnSubscriptionAsync(
                _accountDataHolder.AccountId.Value, organizationId);
            if (!accessError.Success)
                return accessError;

            await _organizationSubscriptionsRepository.DeleteAsync(
                _accountDataHolder.AccountId.Value, organizationId);

            await _notificationsService.NotifyOrganizationUnsubscribedAsync(organizationId);

            logger.Debug(correlationId, null, methodName, "Method finished", null, execTime.Elapsed);
            return CommandResult.OK;
        }

        public async Task<CommandResult<bool>> IsSubscribedAsync(Guid organizationId)
        {
            var correlationId = _correlationIdProvider.Get();
            var execTime = Stopwatch.StartNew();
            var methodName = $"{LOGGER_NAME}{nameof(IsSubscribedAsync)}";
            logger.Debug(correlationId, null, methodName, "Method started", null);

            if (_accountDataHolder.AccountId == null)
                return CommandResult<bool>.Fail(ErrorCode.AccessError, "Необходимо авторизоваться");

            var accessError = await _accessValidator.AssertCanViewOrganizationSubscribersAsync(organizationId);
            if (!accessError.Success)
                return CommandResult<bool>.Fail(accessError.ErrorCode, accessError.Message);

            var exists = await _organizationSubscriptionsRepository.IsSubscriptionExistAsync(
                _accountDataHolder.AccountId.Value, organizationId);

            logger.Debug(correlationId, null, methodName, "Method finished", null, execTime.Elapsed);
            return new CommandResult<bool>(exists);
        }

        public async Task<CommandResult<PagedList<OrganizationSubscription>?>> GetSubscriptionsAsync(
            OrganizationSubscriptionsSearchRequest request)
        {
            var correlationId = _correlationIdProvider.Get();
            var execTime = Stopwatch.StartNew();
            var methodName = $"{LOGGER_NAME}{nameof(GetSubscriptionsAsync)}";
            logger.Debug(correlationId, null, methodName, "Method started", null);

            if (request.AccountId == null)
                return CommandResult<PagedList<OrganizationSubscription>?>.Fail(
                    ErrorCode.IsNullOrEmpty, "Не указан accountId");

            var accessError = await _accessValidator.AssertCanViewAccountOrgSubscriptionsAsync(request.AccountId.Value);
            if (!accessError.Success)
                return CommandResult<PagedList<OrganizationSubscription>?>.Fail(accessError.ErrorCode, accessError.Message);

            var pageIndex = request.PageIndes;
            var pageSize = request.PageSize;
            var pagingError = _pagingValidator.Validate(pageIndex, pageSize);
            if (!pagingError.Success)
                return CommandResult<PagedList<OrganizationSubscription>?>.Fail(pagingError.ErrorCode, pagingError.Message);

            _pagingValidator.Normalize(ref pageIndex, ref pageSize);
            request.PageIndes = pageIndex;
            request.PageSize = pageSize;

            var result = await _organizationSubscriptionsRepository.GetSubscriptionsAsync(request);

            logger.Debug(correlationId, null, methodName, "Method finished", null, execTime.Elapsed);
            return new CommandResult<PagedList<OrganizationSubscription>?>(result);
        }

        public async Task<CommandResult<int>> GetSubscriptionsCountAsync(Guid accountId)
        {
            var correlationId = _correlationIdProvider.Get();
            var execTime = Stopwatch.StartNew();
            var methodName = $"{LOGGER_NAME}{nameof(GetSubscriptionsCountAsync)}";
            logger.Debug(correlationId, null, methodName, "Method started", null);

            var accessError = await _accessValidator.AssertCanViewAccountOrgSubscriptionsAsync(accountId);
            if (!accessError.Success)
                return CommandResult<int>.Fail(accessError.ErrorCode, accessError.Message);

            var count = await _organizationSubscriptionsRepository.GetSubscriptionsCountAsync(accountId);

            logger.Debug(correlationId, null, methodName, "Method finished", null, execTime.Elapsed);
            return new CommandResult<int>(count);
        }

        public async Task<CommandResult<PagedList<OrganizationSubscription>?>> GetSubscribersAsync(
            OrganizationSubscriptionsSearchRequest request)
        {
            var correlationId = _correlationIdProvider.Get();
            var execTime = Stopwatch.StartNew();
            var methodName = $"{LOGGER_NAME}{nameof(GetSubscribersAsync)}";
            logger.Debug(correlationId, null, methodName, "Method started", null);

            if (request.OrganizationId == null)
                return CommandResult<PagedList<OrganizationSubscription>?>.Fail(
                    ErrorCode.IsNullOrEmpty, "Не указан organizationId");

            var accessError = await _accessValidator.AssertCanViewOrganizationSubscribersAsync(request.OrganizationId.Value);
            if (!accessError.Success)
                return CommandResult<PagedList<OrganizationSubscription>?>.Fail(accessError.ErrorCode, accessError.Message);

            var pageIndex = request.PageIndes;
            var pageSize = request.PageSize;
            var pagingError = _pagingValidator.Validate(pageIndex, pageSize);
            if (!pagingError.Success)
                return CommandResult<PagedList<OrganizationSubscription>?>.Fail(pagingError.ErrorCode, pagingError.Message);

            _pagingValidator.Normalize(ref pageIndex, ref pageSize);
            request.PageIndes = pageIndex;
            request.PageSize = pageSize;

            var result = await _organizationSubscriptionsRepository.GetSubscribersAsync(request);

            logger.Debug(correlationId, null, methodName, "Method finished", null, execTime.Elapsed);
            return new CommandResult<PagedList<OrganizationSubscription>?>(result);
        }

        public async Task<CommandResult<int>> GetSubscribersCountAsync(Guid organizationId)
        {
            var correlationId = _correlationIdProvider.Get();
            var execTime = Stopwatch.StartNew();
            var methodName = $"{LOGGER_NAME}{nameof(GetSubscribersCountAsync)}";
            logger.Debug(correlationId, null, methodName, "Method started", null);

            var accessError = await _accessValidator.AssertCanViewOrganizationSubscribersAsync(organizationId);
            if (!accessError.Success)
                return CommandResult<int>.Fail(accessError.ErrorCode, accessError.Message);

            var count = await _organizationSubscriptionsRepository.GetSubscribersCountAsync(organizationId);

            logger.Debug(correlationId, null, methodName, "Method finished", null, execTime.Elapsed);
            return new CommandResult<int>(count);
        }
    }
}
