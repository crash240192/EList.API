using EList.DbDataProvider.Models;
using EList.Models.Wallets;

namespace EList.Repositories.Interfaces
{
    public interface IWalletsRepository
    {
        Task<Guid> CreateTariffAsync(Tariff item);
        Task UpdateTariffAsync(Tariff item);

        Task<Tariff?> GetTariffAsync(Guid tariffId);
        Task<List<Tariff>?> GetTariffsAsync();
        Task<Tariff?> GetWalletTariffAsync(Guid walletId);

        Task<Guid> CreateTariffValidatorAsync(TariffValidator item);
        Task UpdateTariffValidatorAsync(TariffValidator item);
        Task<TariffValidator?> GetTariffValidatorAsync(Guid tariffValidatorId);
        Task<TariffValidator?> GetTariffValidatorByTariffIdAsync(Guid tariffId);

        Task<Guid> CreateWalletAsync(Wallet item);
        Task SetWalletTariffAsync(Guid walletId, Guid tariffId);

        Task<Wallet?> GetWalletAsync(Guid walletId);
        Task<Wallet?> GetAccountWalletAsync(Guid accountId);
        Task<Wallet?> GetOrganizationWalletAsync(Guid organizationId);

        Task<List<Wallet>> GetOverdueWalletsAsync();
        Task DepositeAsync(Guid walletId, double value);
        Task<bool> ChargeByTariffAsync(Guid walletId);
    }
}
