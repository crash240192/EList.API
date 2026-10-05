using System.Diagnostics;
using EList.Common.CorrelationId;
using EList.Common.Logger;
using EList.Common.Models;
using EList.Common.Support;
using EList.Models.Enums;
using EList.Models.Privacy;
using EList.Repositories.Interfaces;
using EList.Services.Interfaces;
using NLog;

namespace EList.Services.Impl
{
    public class ProfilePrivacyService : IProfilePrivacyService
    {
        #region logger
        private static readonly ILogger log = LogManager.GetCurrentClassLogger();
        private static readonly ILoggerWrapper logger = new NLogLoggerWrapper(log);
        private const string LOGGER_NAME = "EList.Services.Impl.ProfilePrivacyService.";
        #endregion

        private readonly ICorrelationIdProvider _correlationIdProvider;
        private readonly IAccountPrivacyRepository _privacyRepository;
        private readonly ISubscriptionsRepository _subscriptionsRepository;
        private readonly IAccountsRepository _accountsRepository;
        private readonly IAccountDataHolder _accountDataHolder;

        public ProfilePrivacyService(
            ICorrelationIdProvider correlationIdProvider,
            IAccountPrivacyRepository privacyRepository,
            ISubscriptionsRepository subscriptionsRepository,
            IAccountsRepository accountsRepository,
            IAccountDataHolder accountDataHolder)
        {
            _correlationIdProvider = correlationIdProvider ?? throw new ArgumentNullException(nameof(correlationIdProvider));
            _privacyRepository = privacyRepository ?? throw new ArgumentNullException(nameof(privacyRepository));
            _subscriptionsRepository = subscriptionsRepository ?? throw new ArgumentNullException(nameof(subscriptionsRepository));
            _accountsRepository = accountsRepository ?? throw new ArgumentNullException(nameof(accountsRepository));
            _accountDataHolder = accountDataHolder ?? throw new ArgumentNullException(nameof(accountDataHolder));
        }

        public async Task<AccountPrivacySettings> GetOrDefaultAsync(Guid accountId)
        {
            return await _privacyRepository.GetOrDefaultAsync(accountId);
        }

        public async Task<CommandResult<AccountPrivacySettings>> GetMySettingsAsync()
        {
            var correlationId = _correlationIdProvider.Get();
            var execTime = Stopwatch.StartNew();
            var methodName = $"{LOGGER_NAME}{nameof(GetMySettingsAsync)}";
            logger.Debug(correlationId, null, methodName, "Method started", null);

            if (_accountDataHolder.AccountId == null)
                return CommandResult<AccountPrivacySettings>.Fail(ErrorCode.AccessError, "Необходимо авторизоваться");

            var settings = await _privacyRepository.GetOrDefaultAsync(_accountDataHolder.AccountId.Value);

            logger.Debug(correlationId, null, methodName, "Method finished", null, execTime.Elapsed);
            return new CommandResult<AccountPrivacySettings>(settings);
        }

        public async Task<CommandResult<AccountPrivacySettings>> UpdateMySettingsAsync(UpdatePrivacySettingsRequest request)
        {
            var correlationId = _correlationIdProvider.Get();
            var execTime = Stopwatch.StartNew();
            var methodName = $"{LOGGER_NAME}{nameof(UpdateMySettingsAsync)}";
            logger.Debug(correlationId, null, methodName, "Method started", null);

            if (_accountDataHolder.AccountId == null)
                return CommandResult<AccountPrivacySettings>.Fail(ErrorCode.AccessError, "Необходимо авторизоваться");

            if (request == null)
                return CommandResult<AccountPrivacySettings>.Fail(ErrorCode.IsNullOrEmpty, "Тело запроса пусто");

            var accountId = _accountDataHolder.AccountId.Value;
            var current = await _privacyRepository.GetOrDefaultAsync(accountId);

            if (request.WhoCanInviteMe.HasValue)
            {
                if (!IsDefined(request.WhoCanInviteMe.Value))
                    return CommandResult<AccountPrivacySettings>.Fail(ErrorCode.InvalidValue, "Некорректное значение WhoCanInviteMe");
                current.WhoCanInviteMe = request.WhoCanInviteMe.Value;
            }

            if (request.AgeVisibility.HasValue)
            {
                if (!IsDefined(request.AgeVisibility.Value))
                    return CommandResult<AccountPrivacySettings>.Fail(ErrorCode.InvalidValue, "Некорректное значение AgeVisibility");
                current.AgeVisibility = request.AgeVisibility.Value;
            }

            if (request.GenderVisibility.HasValue)
            {
                if (!IsDefined(request.GenderVisibility.Value))
                    return CommandResult<AccountPrivacySettings>.Fail(ErrorCode.InvalidValue, "Некорректное значение GenderVisibility");
                current.GenderVisibility = request.GenderVisibility.Value;
            }

            if (request.ShowBirthdayToday.HasValue)
                current.ShowBirthdayToday = request.ShowBirthdayToday.Value;

            if (request.LocationVisibility.HasValue)
            {
                if (!IsDefined(request.LocationVisibility.Value))
                    return CommandResult<AccountPrivacySettings>.Fail(ErrorCode.InvalidValue, "Некорректное значение LocationVisibility");
                current.LocationVisibility = request.LocationVisibility.Value;
            }

            if (request.ProfilePhotosVisibility.HasValue)
            {
                if (!IsDefined(request.ProfilePhotosVisibility.Value))
                    return CommandResult<AccountPrivacySettings>.Fail(ErrorCode.InvalidValue, "Некорректное значение ProfilePhotosVisibility");
                current.ProfilePhotosVisibility = request.ProfilePhotosVisibility.Value;
            }

            current.UpdatedAt = DateTimeOffset.UtcNow;
            await _privacyRepository.UpsertAsync(current);

            logger.Debug(correlationId, null, methodName, "Method finished", null, execTime.Elapsed);
            return new CommandResult<AccountPrivacySettings>(current);
        }

        public async Task<CommandResult<CanInviteResult>> CanInviteAsync(Guid targetAccountId)
        {
            var correlationId = _correlationIdProvider.Get();
            var execTime = Stopwatch.StartNew();
            var methodName = $"{LOGGER_NAME}{nameof(CanInviteAsync)}";
            logger.Debug(correlationId, null, methodName, "Method started", null);

            if (_accountDataHolder.AccountId == null)
                return CommandResult<CanInviteResult>.Fail(ErrorCode.AccessError, "Необходимо авторизоваться");

            var assert = await AssertCanSendInvitationAsync(_accountDataHolder.AccountId.Value, targetAccountId);
            var result = new CanInviteResult
            {
                Allowed = assert.Success,
                Reason = assert.Success ? null : assert.Message
            };

            logger.Debug(correlationId, null, methodName, "Method finished", null, execTime.Elapsed);
            return new CommandResult<CanInviteResult>(result);
        }

        public async Task<CommandResult> AssertCanSendInvitationAsync(Guid inviterAccountId, Guid inviteeAccountId)
        {
            if (inviterAccountId == Guid.Empty || inviteeAccountId == Guid.Empty)
                return CommandResult.Fail(ErrorCode.InvalidValue, "Некорректный идентификатор аккаунта");

            if (inviterAccountId == inviteeAccountId)
                return CommandResult.Fail(ErrorCode.InvitationForbidden, "Нельзя пригласить самого себя");

            var invitee = await _accountsRepository.GetAccountAsync(inviteeAccountId);
            if (invitee == null || invitee.Active == false)
                return CommandResult.Fail(ErrorCode.AccountNotFound, "Аккаунт приглашаемого не найден");

            var settings = await _privacyRepository.GetOrDefaultAsync(inviteeAccountId);
            var allowed = await CanViewerSeeAsync(inviteeAccountId, inviterAccountId, settings.WhoCanInviteMe);
            if (!allowed)
            {
                return CommandResult.Fail(
                    ErrorCode.InvitationForbidden,
                    settings.WhoCanInviteMe switch
                    {
                        PrivacyAudience.Nobody => "Пользователь запретил приглашения",
                        PrivacyAudience.Subscriptions => "Пользователь принимает приглашения только от тех, на кого подписан",
                        PrivacyAudience.Subscribers => "Пользователь принимает приглашения только от своих подписчиков",
                        PrivacyAudience.Mutual => "Пользователь принимает приглашения только при взаимной подписке",
                        _ => "Пользователь ограничил круг лиц, которые могут его приглашать"
                    });
            }

            return CommandResult.OK;
        }

        public async Task<bool> CanViewerSeeAsync(Guid ownerAccountId, Guid? viewerAccountId, PrivacyAudience audience)
        {
            if (viewerAccountId == ownerAccountId)
                return true;

            return audience switch
            {
                PrivacyAudience.Everyone => viewerAccountId != null,
                PrivacyAudience.Nobody => false,
                PrivacyAudience.Subscriptions => viewerAccountId != null
                    && await _subscriptionsRepository.IsSubscriptionExistAsync(ownerAccountId, viewerAccountId.Value),
                PrivacyAudience.Subscribers => viewerAccountId != null
                    && await _subscriptionsRepository.IsSubscriptionExistAsync(viewerAccountId.Value, ownerAccountId),
                PrivacyAudience.Mutual => viewerAccountId != null
                    && await _subscriptionsRepository.IsSubscriptionExistAsync(ownerAccountId, viewerAccountId.Value)
                    && await _subscriptionsRepository.IsSubscriptionExistAsync(viewerAccountId.Value, ownerAccountId),
                _ => false
            };
        }

        private static bool IsDefined(PrivacyAudience value) => Enum.IsDefined(typeof(PrivacyAudience), value);
    }
}
