using EList.DbDataProvider.Interfaces;
using EList.DbDataProvider.Models;
using LinqToDB;
using LinqToDB.Async;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EList.DbDataProvider.DataProviders
{
    public class WalletsDataProvider : DataProviderBase, IWalletsDataProvider
    {
        public WalletsDataProvider(IDataConnectionProvider dataConnectionProvider) : base(dataConnectionProvider)
        {
        }

        public async Task<Guid> CreateTariffAsync(TariffDto item)
        {
            var result = (Guid)await _connection.InsertWithIdentityAsync(item);
            return result;
        }

        public async Task UpdateTariffAsync(TariffDto item)
        {
            await _connection.Tariffs.Where(i => i.Id == item.Id)
                .Set(i => i.Cost, item.Cost)
                .Set(i => i.Name, item.Name)
                .Set(i => i.Period, item.Period)
                .Set(i => i.ValidatorId, item.ValidatorId)
                .UpdateAsync();
        }

        public async Task<TariffDto?> GetTariffAsync(Guid tariffId)
        {
            var item = await _connection.Tariffs.FirstOrDefaultAsync(i => i.Id == tariffId);
            return item;
        }

        public async Task<List<TariffDto>?> GetTariffsAsync()
        {
            var result = await _connection.Tariffs
                .LoadWith(i => i.TariffValidator)
                .OrderBy(i => i.Cost)
                .ToListAsync();
            return result;
        }

        public async Task<TariffDto?> GetWalletTariffAsync(Guid walletId)
        {
            var wallet = await _connection.Wallets.FirstOrDefaultAsync(i => i.Id == walletId);
            if (wallet?.TariffId != null)
            {
                var tariff = await _connection.Tariffs.FirstOrDefaultAsync(i => i.Id == wallet.TariffId);
                return tariff;
            }
            return null;
        }


        public async Task<Guid> CreateTariffValidatorAsync(TariffValidatorDto item)
        {
            var result = (Guid)await _connection.InsertWithIdentityAsync(item);
            return result;
        }

        public async Task UpdateTariffValidatorAsync(TariffValidatorDto item)
        {
            await _connection.TariffValidators.Where(i => i.Id == item.Id)
                .Set(i => i.CostLimit, item.CostLimit)
                .Set(i => i.PersonsLimit, item.PersonsLimit)
                .Set(i => i.AllowPrivate, item.AllowPrivate)
                .Set(i => i.AllowGenderSegregation, item.AllowGenderSegregation)
                .Set(i => i.AgeLimit, item.AgeLimit)
                .Set(i => i.AllowMultidaysEvent, item. AllowMultidaysEvent)
                .Set(i => i.MaxEventsCount, item.MaxEventsCount)
                .Set(i => i.CreateDateMaxPeriod, item.CreateDateMaxPeriod)
                .UpdateAsync();
        }

        public async Task<TariffValidatorDto?> GetTariffValidatorAsync(Guid tariffValidatorId)
        {
            var result = await _connection.TariffValidators.FirstOrDefaultAsync(i => i.Id == tariffValidatorId);
            return result;
        }

        public async Task<TariffValidatorDto?> GetTariffValidatorByTariffIdAsync(Guid tariffId)
        {
            var tariff = await _connection.Tariffs.FirstOrDefaultAsync(i => i.Id == tariffId);
            if (tariff?.ValidatorId != null)
            {
                var validator = await _connection.TariffValidators.FirstOrDefaultAsync(i => i.Id == tariff.ValidatorId);
                return validator;
            }
            return null;
        }


        public async Task<Guid> CreateWalletAsync()
        {
            var result = (Guid)await _connection.InsertWithIdentityAsync(new WalletDto
            {
                Balance = 0,
                LastChargeDate = null,
                TariffId = null,
                PaidDate = null
            });
            return result;
        }

        public async Task SetWalletTariffAsync(Guid walletId, Guid tariffId)
        {
            await _connection.Wallets.Where(i => i.Id == walletId)
                .Set(i => i.TariffId, tariffId)
                .UpdateAsync();
        }

        public async Task<WalletDto?> GetWalletAsync(Guid walletId)
        {
            var result = await _connection.Wallets.FirstOrDefaultAsync(i => i.Id == walletId);
            return result;
        }

        public async Task<WalletDto?> GetAccountWalletAsync(Guid accountId)
        {
            var account = await _connection.Accounts.FirstOrDefaultAsync(i => i.Id == accountId);
            if (account?.WalletId != null)
            {
                var wallet = await _connection.Wallets.FirstOrDefaultAsync(i => i.Id == account.WalletId);
                return wallet;
            }
            return null;
        }

        public async Task<WalletDto?> GetOrganizationWalletAsync(Guid organizationId)
        {
            var organization = await _connection.Organizations.FirstOrDefaultAsync(i => i.Id == organizationId);
            if (organization?.WalletId != null)
            {
                var wallet = await _connection.Wallets.FirstOrDefaultAsync(i => i.Id == organization.WalletId);
                return wallet;
            }
            return null;
        }

        
        public async Task<List<WalletDto>> GetOverdueWalletsAsync()
        {
            var wallets = await _connection.Wallets.Where(i => i.TariffId != null && i.PaidDate>i.LastChargeDate).ToListAsync();
            return wallets;
        }

        public async Task DepositeAsync(Guid walletId, double value)
        {
            await _connection.Wallets.Where(i => i.Id == walletId)
                .Set(i => i.PaidDate, DateTimeOffset.Now)
                .Set(i => i.Balance, value)
                .UpdateAsync();
        }

        public async Task<bool> ChargeByTariffAsync(Guid walletId)
        {
            var wallet = await _connection.Wallets.FirstOrDefaultAsync(i => i.Id == walletId);
            if (wallet != null && (wallet.LastChargeDate == null || wallet.LastChargeDate < wallet.PaidDate))
            {
                if (wallet.TariffId != null)
                {
                    var tariff = await _connection.Tariffs.FirstOrDefaultAsync(i => i.Id == wallet.TariffId);

                    if (tariff != null && tariff?.Cost > 0)
                    {
                        if (DateTimeOffset.Now - wallet.PaidDate >= tariff.Period)
                        {
                            await _connection.Wallets.Where(i => i.Id == walletId)
                                .Set(i => i.LastChargeDate, DateTimeOffset.Now)
                                .Set(i => i.Balance, wallet.Balance - tariff.Cost)
                                .UpdateAsync();

                            return true;
                        }
                    }
                }
            }

            return false;
        }
    }
}
