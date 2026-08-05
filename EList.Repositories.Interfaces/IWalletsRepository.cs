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

        Task<TariffValidator?> GetAccountTariffValidatorAsync(Guid accountId);
        Task<TariffValidator?> GetOrganizationTariffValidatorAsync(Guid organizationId);
        Task<Guid> CreateTariffValidatorAsync(TariffValidator item);
        Task UpdateTariffValidatorAsync(TariffValidator item);
        Task<TariffValidator?> GetTariffValidatorAsync(Guid tariffValidatorId);
        Task<TariffValidator?> GetTariffValidatorByTariffIdAsync(Guid tariffId);

        Task<Guid> CreateWalletAsync();
        Task SetWalletTariffAsync(Guid walletId, Guid tariffId);

        Task<Wallet?> GetWalletAsync(Guid walletId);
        Task<Wallet?> GetAccountWalletAsync(Guid accountId);
        Task<Wallet?> GetOrganizationWalletAsync(Guid organizationId);

        Task<List<Wallet>> GetOverdueWalletsAsync();
        Task DepositeAsync(Guid walletId, double value);
        Task<bool> ChargeByTariffAsync(Guid walletId);
    }
}
