using EList.Common.Models;
using EList.Common.Support;
using EList.Repositories.Interfaces;
using EList.Validators.Interfaces;

namespace EList.Validators.Impl
{
    public class SubscriptionAccessValidator : ISubscriptionAccessValidator
    {
        private readonly IAccountsRepository _accountsRepository;
        private readonly ISubscriptionsRepository _subscriptionsRepository;

        public SubscriptionAccessValidator(
            IAccountsRepository accountsRepository,
            ISubscriptionsRepository subscriptionsRepository)
        {
            _accountsRepository = accountsRepository;
            _subscriptionsRepository = subscriptionsRepository;
        }

        public async Task<CommandResult> AssertCanSubscribeAsync(Guid subscriberId, Guid subscribeToId)
        {
            if (subscriberId == subscribeToId)
                return CommandResult.Fail(ErrorCode.AccessError, "Нельзя подписаться на свой аккаунт");

            var target = await _accountsRepository.GetAccountAsync(subscribeToId);
            if (target == null)
                return CommandResult.Fail(ErrorCode.AccountNotFound, "Аккаунт для подписки не найден");

            if (await _subscriptionsRepository.IsSubscriptionExistAsync(subscriberId, subscribeToId))
                return CommandResult.Fail(ErrorCode.SubscriptionAlreadyExists, "Подписка уже существует");

            return CommandResult.OK;
        }

        public async Task<CommandResult> AssertCanManageOwnSubscriptionAsync(Guid subscriberId, Guid subscribedToId)
        {
            if (!await _subscriptionsRepository.IsSubscriptionExistAsync(subscriberId, subscribedToId))
                return CommandResult.Fail(ErrorCode.SubscriptionNotExists, "Подписка не найдена");

            return CommandResult.OK;
        }

        public async Task<CommandResult> AssertCanViewSubscriptionsAsync(Guid accountId, Guid? viewerAccountId)
        {
            var account = await _accountsRepository.GetAccountAsync(accountId);
            if (account == null)
                return CommandResult.Fail(ErrorCode.AccountNotFound, "Аккаунт не найден");

            // Приватность списков подписок пока не предусмотрена — доступ открыт.
            return CommandResult.OK;
        }
    }
}
