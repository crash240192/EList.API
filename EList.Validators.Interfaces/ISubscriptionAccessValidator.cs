using EList.Common.Models;

namespace EList.Validators.Interfaces
{
    /// <summary>
    /// Базовая валидация подписок. Приватность списков подписчиков пока не предусмотрена.
    /// </summary>
    public interface ISubscriptionAccessValidator
    {
        Task<CommandResult> AssertCanSubscribeAsync(Guid subscriberId, Guid subscribeToId);

        Task<CommandResult> AssertCanManageOwnSubscriptionAsync(Guid subscriberId, Guid subscribedToId);

        /// <summary>
        /// Просмотр подписок/подписчиков аккаунта (пока общедоступно при существующем аккаунте).
        /// </summary>
        Task<CommandResult> AssertCanViewSubscriptionsAsync(Guid accountId, Guid? viewerAccountId);
    }
}
