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

        /// <summary>
        /// Внутреннее зачисление на тарифный кошелёк (инкремент). Не билетный контур.
        /// </summary>
        Task<CommandResult> DepositeAsync(Guid walletId, double value);

        Task<CommandResult<bool>> ChargeByTariffAsync(Guid walletId);

        /// <summary>
        /// Создать пополнение тарифного кошелька через платёжный провайдер (сейчас stub).
        /// Билеты / сплит сюда не входят.
        /// </summary>
        Task<CommandResult<CreateWalletDepositResponse>> CreateWalletDepositAsync(CreateWalletDepositRequest request);

        /// <summary>Stub/debug: имитация успешной оплаты пополнения.</summary>
        Task<CommandResult<WalletDepositResponse>> CompleteWalletDepositAsync(CompleteWalletDepositRequest request);

        Task<CommandResult<List<WalletDepositResponse>>> GetWalletDepositsAsync(Guid walletId);

        Task<CommandResult<List<WalletTariffChargeResponse>>> GetWalletTariffChargesAsync(Guid walletId);
    }
}
