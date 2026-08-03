using EList.Common.Models;
using EList.Models.Wallets;

namespace EList.Services.Interfaces
{
    public interface IWalletsService
    {
        Task<CommandResult<Guid?>> CreateTariffAsync(Tariff item);
        Task<CommandResult> UpdateTariffAsync(Tariff item);

        Task<CommandResult<Tariff?>> GetTariffAsync(Guid tariffId);
        Task<CommandResult<List<Tariff>?>> GetTariffsAsync(bool? forOrganization = null);
        Task<CommandResult<Tariff?>> GetWalletTariffAsync(Guid walletId);

        Task<CommandResult<Guid?>> CreateTariffValidatorAsync(TariffValidator item);
        Task<CommandResult> UpdateTariffValidatorAsync(TariffValidator item);
        Task<CommandResult<TariffValidator?>> GetTariffValidatorAsync(Guid tariffValidatorId);
        Task<CommandResult<TariffValidator?>> GetTariffValidatorByTariffIdAsync(Guid tariffId);

        Task<CommandResult<Guid?>> CreateAccountWalletAsync(Guid? accountId = null);
        Task<CommandResult<Guid?>> CreateOrganizationWalletAsync(Wallet item);
        Task<CommandResult> SetWalletTariffAsync(Guid walletId, Guid tariffId);

        Task<CommandResult<Wallet?>> GetWalletAsync(Guid walletId);
        Task<CommandResult<Wallet?>> GetAccountWalletAsync(Guid accountId);
        Task<CommandResult<Wallet?>> GetOrganizationWalletAsync(Guid organizationId);

        Task<CommandResult<List<Wallet>>> GetOverdueWalletsAsync();
        Task<CommandResult> DepositeAsync(Guid walletId, double value);
        Task<CommandResult<bool>> ChargeByTariffAsync(Guid walletId);
    }
}
