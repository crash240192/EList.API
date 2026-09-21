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

        Task<TariffValidatorDto?> GetAccountTariffValidatorAsync(Guid accountId);
        Task<TariffValidatorDto?> GetOrganizationTariffValidatorAsync(Guid organizationId);
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

        Task<Guid> CreateWalletDepositAsync(WalletDepositDto item);
        Task<WalletDepositDto?> GetWalletDepositAsync(Guid depositId);
        Task<WalletDepositDto?> GetWalletDepositByProviderPaymentAsync(PaymentProvider provider, string providerPaymentId);
        Task<WalletDepositDto?> GetWalletDepositByIdempotencyAsync(Guid walletId, string idempotencyKey);
        Task UpdateWalletDepositAsync(Guid depositId, WalletDepositStatus status, string? providerPaymentId, DateTimeOffset? paidAt);
        Task<List<WalletDepositDto>> GetWalletDepositsAsync(Guid walletId);

        /// <summary>Аккаунт, к которому привязан кошелёк (если есть).</summary>
        Task<Guid?> FindAccountIdByWalletAsync(Guid walletId);

        /// <summary>Организация, к которой привязан кошелёк (если есть).</summary>
        Task<Guid?> FindOrganizationIdByWalletAsync(Guid walletId);
    }
}
