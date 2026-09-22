using EList.DbDataProvider.Models;
using EList.DbDataProvider.Models.Enums;

namespace EList.DbDataProvider.Interfaces
{
    public interface IWalletsDataProvider
    {
        Task<Guid> CreateTariffAsync(TariffDto item);
        Task UpdateTariffAsync(TariffDto item);
        
        Task<TariffDto?> GetTariffAsync(Guid tariffId);
        Task<List<TariffDto>?> GetTariffsAsync(bool? forOrganization = null);
        Task<TariffDto?> GetWalletTariffAsync(Guid walletId);
        /// <summary>Бесплатный тариф контура (cost=0). Не больше одного на forOrganization.</summary>
        Task<TariffDto?> GetDefaultFreeTariffAsync(bool forOrganization);
        /// <summary>Другой тариф с cost=0 в том же контуре (для валидации уникальности).</summary>
        Task<TariffDto?> FindOtherZeroCostTariffAsync(bool forOrganization, Guid? excludeTariffId);

        Task<TariffValidatorDto?> GetAccountTariffValidatorAsync(Guid accountId);
        Task<TariffValidatorDto?> GetOrganizationTariffValidatorAsync(Guid organizationId);
        /// <summary>Валидатор effective-тарифа кошелька (выбранный если активен, иначе free default).</summary>
        Task<TariffValidatorDto?> GetEffectiveTariffValidatorForWalletAsync(Guid walletId);
        Task<Guid> CreateTariffValidatorAsync(TariffValidatorDto item);
        Task UpdateTariffValidatorAsync(TariffValidatorDto item);
        Task<TariffValidatorDto?> GetTariffValidatorAsync(Guid tariffValidatorId);
        Task<TariffValidatorDto?> GetTariffValidatorByTariffIdAsync(Guid tariffId);


        Task<Guid> CreateWalletAsync(bool forOrganization = false);
        Task SetWalletTariffAsync(Guid walletId, Guid tariffId);

        Task<WalletDto?> GetWalletAsync(Guid walletId);
        Task<WalletDto?> GetAccountWalletAsync(Guid accountId);
        Task<WalletDto?> GetOrganizationWalletAsync(Guid organizationId);

        Task<List<WalletDto>> GetOverdueWalletsAsync();
        /// <summary>Только +balance. Период тарифа не сдвигается.</summary>
        Task DepositeAsync(Guid walletId, double value);
        /// <summary>
        /// Списать тариф если due и хватает баланса. Без минуса.
        /// Период стартует с момента успешного списания.
        /// </summary>
        Task<bool> ChargeByTariffAsync(Guid walletId);

        Task<Guid> CreateWalletDepositAsync(WalletDepositDto item);
        Task<WalletDepositDto?> GetWalletDepositAsync(Guid depositId);
        Task<WalletDepositDto?> GetWalletDepositByProviderPaymentAsync(PaymentProvider provider, string providerPaymentId);
        Task<WalletDepositDto?> GetWalletDepositByIdempotencyAsync(Guid walletId, string idempotencyKey);
        Task UpdateWalletDepositAsync(
            Guid depositId,
            WalletDepositStatus status,
            string? providerPaymentId,
            DateTimeOffset? paidAt,
            double? balanceAfter = null);
        Task<List<WalletDepositDto>> GetWalletDepositsAsync(Guid walletId);

        Task ClearNextChargeAtAsync(Guid walletId);
        Task<List<WalletTariffChargeDto>> GetWalletTariffChargesAsync(Guid walletId);

        /// <summary>Аккаунт, к которому привязан кошелёк (если есть).</summary>
        Task<Guid?> FindAccountIdByWalletAsync(Guid walletId);

        /// <summary>Организация, к которой привязан кошелёк (если есть).</summary>
        Task<Guid?> FindOrganizationIdByWalletAsync(Guid walletId);
    }
}
