using AutoMapper;
using EList.DbDataProvider.Interfaces;
using EList.DbDataProvider.Models;
using EList.Models.Wallets;
using EList.Repositories.Interfaces;

namespace EList.Repositories.Impl
{
    public class WalletsRepository : IWalletsRepository
    {
        private readonly IMapper _mapper;
        private readonly IWalletsDataProvider _walletsDataProvider;

        public WalletsRepository(IMapper mapper,
            IWalletsDataProvider walletsDataProvider) 
        { 
            _mapper = mapper;
            _walletsDataProvider = walletsDataProvider;
        }


        public async Task<Guid> CreateTariffAsync(Tariff item)
        {
            var mappedRequest = _mapper.Map<TariffDto>(item);
            var result = await _walletsDataProvider.CreateTariffAsync(mappedRequest);
            return result;
        }

        public async Task UpdateTariffAsync(Tariff item)
        {
            var mappedRequest = _mapper.Map<TariffDto>(item);
            await _walletsDataProvider.UpdateTariffAsync(mappedRequest);
        }

        public async Task<Tariff?> GetTariffAsync(Guid tariffId)
        {
            var tariff = await _walletsDataProvider.GetTariffAsync(tariffId);
            var result = _mapper.Map<Tariff>(tariff);
            return result;
        }
        
        public async Task<List<Tariff>?> GetTariffsAsync(bool? forOrganization = null)
        {
            var tariff = await _walletsDataProvider.GetTariffsAsync(forOrganization);
            var result = _mapper.Map<List<Tariff>>(tariff);
            return result;
        }

        public async Task<Tariff?> GetDefaultFreeTariffAsync(bool forOrganization)
        {
            var tariff = await _walletsDataProvider.GetDefaultFreeTariffAsync(forOrganization);
            return _mapper.Map<Tariff?>(tariff);
        }

        public async Task<Tariff?> FindOtherZeroCostTariffAsync(bool forOrganization, Guid? excludeTariffId)
        {
            var tariff = await _walletsDataProvider.FindOtherZeroCostTariffAsync(forOrganization, excludeTariffId);
            return _mapper.Map<Tariff?>(tariff);
        }


        public async Task<TariffValidator?> GetAccountTariffValidatorAsync(Guid accountId)
        {
            var wallet = await _walletsDataProvider.GetAccountTariffValidatorAsync(accountId);
            var result = _mapper.Map<TariffValidator?>(wallet);
            return result;
        }

        public async Task<TariffValidator?> GetOrganizationTariffValidatorAsync(Guid organizationId)
        {
            var validator = await _walletsDataProvider.GetOrganizationTariffValidatorAsync(organizationId);
            var result = _mapper.Map<TariffValidator?>(validator);
            return result;
        }

        public async Task<TariffValidator?> GetEffectiveTariffValidatorForWalletAsync(Guid walletId)
        {
            var validator = await _walletsDataProvider.GetEffectiveTariffValidatorForWalletAsync(walletId);
            return _mapper.Map<TariffValidator?>(validator);
        }

        public async Task<Guid> CreateTariffValidatorAsync(TariffValidator item)
        {
            var mappedRequest = _mapper.Map<TariffValidatorDto>(item);
            var result = await _walletsDataProvider.CreateTariffValidatorAsync(mappedRequest);
            return result;
        }
        
        public async Task UpdateTariffValidatorAsync(TariffValidator item)
        {
            var mappedRequest = _mapper.Map<TariffValidatorDto>(item);
            await _walletsDataProvider.UpdateTariffValidatorAsync(mappedRequest);
        }

        public async Task<TariffValidator?> GetTariffValidatorAsync(Guid tariffValidatorId)
        {
            var tariffValidator = await _walletsDataProvider.GetTariffValidatorAsync(tariffValidatorId);
            var result = _mapper.Map<TariffValidator>(tariffValidator);
            return result;
        }

        public async Task<TariffValidator?> GetTariffValidatorByTariffIdAsync(Guid tariffId)
        {
            var tariffValidator = await _walletsDataProvider.GetTariffValidatorByTariffIdAsync(tariffId);
            var result = _mapper.Map<TariffValidator>(tariffValidator);
            return result;
        }



        public async Task<Guid> CreateWalletAsync(bool forOrganization = false)
        {
            var result = await _walletsDataProvider.CreateWalletAsync(forOrganization);
            return result;
        }
        
        public async Task<Wallet?> GetWalletAsync(Guid walletId)
        {
            var wallet = await _walletsDataProvider.GetWalletAsync(walletId);
            var result = _mapper.Map<Wallet>(wallet);
            return result;
        }

        public async Task<Wallet?> GetAccountWalletAsync(Guid accountId)
        {
            var wallet = await _walletsDataProvider.GetAccountWalletAsync(accountId);
            var result = _mapper.Map<Wallet>(wallet);
            return result;
        }

        public async Task<Wallet?> GetOrganizationWalletAsync(Guid organizationId)
        {
            var wallet = await _walletsDataProvider.GetOrganizationWalletAsync(organizationId);
            var result = _mapper.Map<Wallet>(wallet);
            return result;
        }

        public async Task<Tariff?> GetWalletTariffAsync(Guid walletId)
        {
            var tariff = await _walletsDataProvider.GetWalletTariffAsync(walletId);
            var result = _mapper.Map<Tariff>(tariff);
            return result;
        }

        public async Task SetWalletTariffAsync(Guid walletId, Guid tariffId)
        {
            await _walletsDataProvider.SetWalletTariffAsync(walletId, tariffId);
        }



        public async Task<List<Wallet>> GetOverdueWalletsAsync()
        {
            var wallet = await _walletsDataProvider.GetOverdueWalletsAsync();
            var result = _mapper.Map<List<Wallet>>(wallet);
            return result;
        }

        public async Task DepositeAsync(Guid walletId, double value)
        {
            await _walletsDataProvider.DepositeAsync(walletId, value);
        }

        public async Task<bool> ChargeByTariffAsync(Guid walletId)
        {
            var result = await _walletsDataProvider.ChargeByTariffAsync(walletId);
            return result;
        }

        public async Task<Guid> CreateWalletDepositAsync(WalletDeposit item)
        {
            var mapped = _mapper.Map<WalletDepositDto>(item);
            return await _walletsDataProvider.CreateWalletDepositAsync(mapped);
        }

        public async Task<WalletDeposit?> GetWalletDepositAsync(Guid depositId)
        {
            var dto = await _walletsDataProvider.GetWalletDepositAsync(depositId);
            return _mapper.Map<WalletDeposit>(dto);
        }

        public async Task<WalletDeposit?> GetWalletDepositByProviderPaymentAsync(
            Models.Enums.PaymentProvider provider, string providerPaymentId)
        {
            var mappedProvider = _mapper.Map<DbDataProvider.Models.Enums.PaymentProvider>(provider);
            var dto = await _walletsDataProvider.GetWalletDepositByProviderPaymentAsync(
                mappedProvider, providerPaymentId);
            return _mapper.Map<WalletDeposit>(dto);
        }

        public async Task<WalletDeposit?> GetWalletDepositByIdempotencyAsync(Guid walletId, string idempotencyKey)
        {
            var dto = await _walletsDataProvider.GetWalletDepositByIdempotencyAsync(walletId, idempotencyKey);
            return _mapper.Map<WalletDeposit>(dto);
        }

        public async Task UpdateWalletDepositAsync(
            Guid depositId,
            Models.Enums.WalletDepositStatus status,
            string? providerPaymentId,
            DateTimeOffset? paidAt,
            double? balanceAfter = null)
        {
            var mappedStatus = _mapper.Map<DbDataProvider.Models.Enums.WalletDepositStatus>(status);
            await _walletsDataProvider.UpdateWalletDepositAsync(
                depositId, mappedStatus, providerPaymentId, paidAt, balanceAfter);
        }

        public async Task<List<WalletDeposit>> GetWalletDepositsAsync(Guid walletId)
        {
            var list = await _walletsDataProvider.GetWalletDepositsAsync(walletId);
            return _mapper.Map<List<WalletDeposit>>(list);
        }

        public Task ClearNextChargeAtAsync(Guid walletId)
            => _walletsDataProvider.ClearNextChargeAtAsync(walletId);

        public async Task<List<WalletTariffCharge>> GetWalletTariffChargesAsync(Guid walletId)
        {
            var list = await _walletsDataProvider.GetWalletTariffChargesAsync(walletId);
            return _mapper.Map<List<WalletTariffCharge>>(list);
        }

        public Task<Guid?> FindAccountIdByWalletAsync(Guid walletId)
            => _walletsDataProvider.FindAccountIdByWalletAsync(walletId);

        public Task<Guid?> FindOrganizationIdByWalletAsync(Guid walletId)
            => _walletsDataProvider.FindOrganizationIdByWalletAsync(walletId);
    }
}
