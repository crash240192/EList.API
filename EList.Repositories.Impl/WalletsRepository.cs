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
        
        public async Task<List<Tariff>?> GetTariffsAsync()
        {
            var tariff = await _walletsDataProvider.GetTariffsAsync();
            var result = _mapper.Map<List<Tariff>>(tariff);
            return result;
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



        public async Task<Guid> CreateWalletAsync(Wallet item)
        {
            var mappedRequest = _mapper.Map<WalletDto>(item);
            var result = await _walletsDataProvider.CreateWalletAsync(mappedRequest);
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
    }
}
