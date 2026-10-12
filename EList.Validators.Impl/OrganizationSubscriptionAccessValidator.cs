using EList.Common.Models;
using EList.Common.Support;
using EList.Repositories.Interfaces;
using EList.Validators.Interfaces;

namespace EList.Validators.Impl
{
    public class OrganizationSubscriptionAccessValidator : IOrganizationSubscriptionAccessValidator
    {
        private readonly IOrganizationsRepository _organizationsRepository;
        private readonly IOrganizationSubscriptionsRepository _organizationSubscriptionsRepository;
        private readonly IAccountsRepository _accountsRepository;

        public OrganizationSubscriptionAccessValidator(
            IOrganizationsRepository organizationsRepository,
            IOrganizationSubscriptionsRepository organizationSubscriptionsRepository,
            IAccountsRepository accountsRepository)
        {
            _organizationsRepository = organizationsRepository;
            _organizationSubscriptionsRepository = organizationSubscriptionsRepository;
            _accountsRepository = accountsRepository;
        }

        public async Task<CommandResult> AssertCanSubscribeAsync(Guid subscriberId, Guid organizationId)
        {
            var organization = await _organizationsRepository.GetOrganizationAsync(organizationId);
            if (organization == null)
                return CommandResult.Fail(ErrorCode.OrganizationNotFound, "Организация не найдена");

            if (!organization.Active)
                return CommandResult.Fail(ErrorCode.AccessError, "Нельзя подписаться на неактивную организацию");

            if (await _organizationSubscriptionsRepository.IsSubscriptionExistAsync(subscriberId, organizationId))
                return CommandResult.Fail(ErrorCode.SubscriptionAlreadyExists, "Подписка уже существует");

            return CommandResult.OK;
        }

        public async Task<CommandResult> AssertCanManageOwnSubscriptionAsync(Guid subscriberId, Guid organizationId)
        {
            if (!await _organizationSubscriptionsRepository.IsSubscriptionExistAsync(subscriberId, organizationId))
                return CommandResult.Fail(ErrorCode.SubscriptionNotExists, "Подписка не найдена");

            return CommandResult.OK;
        }

        public async Task<CommandResult> AssertCanViewOrganizationSubscribersAsync(Guid organizationId)
        {
            var organization = await _organizationsRepository.GetOrganizationAsync(organizationId);
            if (organization == null)
                return CommandResult.Fail(ErrorCode.OrganizationNotFound, "Организация не найдена");

            return CommandResult.OK;
        }

        public async Task<CommandResult> AssertCanViewAccountOrgSubscriptionsAsync(Guid accountId)
        {
            var account = await _accountsRepository.GetAccountAsync(accountId);
            if (account == null)
                return CommandResult.Fail(ErrorCode.AccountNotFound, "Аккаунт не найден");

            return CommandResult.OK;
        }
    }
}