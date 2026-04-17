using EList.DbDataProvider.Models;

namespace EList.DbDataProvider.Interfaces
{
    public interface IWalletsDataProvider
    {
        Task<Guid> CreateTariffAsync(TariffDto item);
        Task UpdateTariffAsync(TariffDto item);
        
        Task<TariffDto?> GetTariffAsync(Guid tariffId);
        Task<List<TariffDto>?> GetTariffsAsync();
        Task<TariffDto?> GetWalletTariffAsync(Guid walletId);

        Task<Guid> CreateTariffValidatorAsync(TariffValidatorDto item);
        Task UpdateTariffValidatorAsync(TariffValidatorDto item);
        Task<TariffValidatorDto?> GetTariffValidatorAsync(Guid tariffValidatorId);
        Task<TariffValidatorDto?> GetTariffValidatorByTariffIdAsync(Guid tariffId);


        Task<Guid> CreateWalletAsync();
        Task SetWalletTariffAsync(Guid walletId, Guid tariffId);

        Task<WalletDto?> GetWalletAsync(Guid walletId);
        Task<WalletDto?> GetAccountWalletAsync(Guid accountId);
        Task<WalletDto?> GetOrganizationWalletAsync(Guid organizationId);

        Task<List<WalletDto>> GetOverdueWalletsAsync();
        Task DepositeAsync(Guid walletId, double value);
        Task<bool> ChargeByTariffAsync(Guid walletId);
    }
}
