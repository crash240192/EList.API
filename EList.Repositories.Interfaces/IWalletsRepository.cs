using EList.DbDataProvider.Models;
using EList.Models.Wallets;

namespace EList.Repositories.Interfaces
{
    public interface IWalletsRepository
    {
        Task<Guid> CreateTariffAsync(Tariff item);
        Task UpdateTariffAsync(Tariff item);

        Task<Tariff?> GetTariffAsync(Guid tariffId);
        Task<List<Tariff>?> GetTariffsAsync(bool? forOrganization = null);
        Task<Tariff?> GetWalletTariffAsync(Guid walletId);
        Task<Tariff?> GetDefaultFreeTariffAsync(bool forOrganization);
        Task<Tariff?> FindOtherZeroCostTariffAsync(bool forOrganization, Guid? excludeTariffId);

        Task<TariffValidator?> GetAccountTariffValidatorAsync(Guid accountId);
        Task<TariffValidator?> GetOrganizationTariffValidatorAsync(Guid organizationId);
        Task<TariffValidator?> GetEffectiveTariffValidatorForWalletAsync(Guid walletId);
        Task<Guid> CreateTariffValidatorAsync(TariffValidator item);
        Task UpdateTariffValidatorAsync(TariffValidator item);
        Task<TariffValidator?> GetTariffValidatorAsync(Guid tariffValidatorId);
        Task<TariffValidator?> GetTariffValidatorByTariffIdAsync(Guid tariffId);

        Task<Guid> CreateWalletAsync(bool forOrganization = false);
        Task SetWalletTariffAsync(Guid walletId, Guid tariffId);

        Task<Wallet?> GetWalletAsync(Guid walletId);
        Task<Wallet?> GetAccountWalletAsync(Guid accountId);
        Task<Wallet?> GetOrganizationWalletAsync(Guid organizationId);

        Task<List<Wallet>> GetOverdueWalletsAsync();
        Task DepositeAsync(Guid walletId, double value);
        Task<bool> ChargeByTariffAsync(Guid walletId);

        Task<Guid> CreateWalletDepositAsync(WalletDeposit item);
        Task<WalletDeposit?> GetWalletDepositAsync(Guid depositId);
        Task<WalletDeposit?> GetWalletDepositByProviderPaymentAsync(Models.Enums.PaymentProvider provider, string providerPaymentId);
        Task<WalletDeposit?> GetWalletDepositByIdempotencyAsync(Guid walletId, string idempotencyKey);
        Task UpdateWalletDepositAsync(Guid depositId, Models.Enums.WalletDepositStatus status, string? providerPaymentId, DateTimeOffset? paidAt);
        Task<List<WalletDeposit>> GetWalletDepositsAsync(Guid walletId);
        Task ClearNextChargeAtAsync(Guid walletId);
        Task<List<WalletTariffCharge>> GetWalletTariffChargesAsync(Guid walletId);
        Task<Guid?> FindAccountIdByWalletAsync(Guid walletId);
        Task<Guid?> FindOrganizationIdByWalletAsync(Guid walletId);
    }
}
