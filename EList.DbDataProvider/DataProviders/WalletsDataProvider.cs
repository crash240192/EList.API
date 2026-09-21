using EList.DbDataProvider.Interfaces;
using EList.DbDataProvider.Models;
using EList.DbDataProvider.Models.Enums;
using LinqToDB;
using LinqToDB.Async;


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

        public async Task<List<TariffDto>?> GetTariffsAsync(bool? forOrganization = null)
        {
            var query = _connection.Tariffs
                .LoadWith(i => i.TariffValidator)
                .AsQueryable();
                
            if (forOrganization != null)
                query = query.Where(i => i.ForOrganization == forOrganization.Value);

            var result = await query.OrderBy(i => i.Cost)
                .ToListAsync();

            return result;
        }

        public async Task<TariffDto?> GetWalletTariffAsync(Guid walletId)
        {
            var wallet = await _connection.Wallets.FirstOrDefaultAsync(i => i.Id == walletId);
            if (wallet?.TariffId != null)
            {
                var tariff = await _connection.Tariffs.LoadWith(i => i.TariffValidator)
                    .FirstOrDefaultAsync(i => i.Id == wallet.TariffId);
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
                .Set(i => i.AllowMultidaysEvent, item.AllowMultidaysEvent)
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
            var walletId = await _connection.Accounts
                .Where(i => i.Id == accountId)
                .Select(i => i.WalletId)
                .FirstOrDefaultAsync();
            if (walletId != null)
            {
                var wallet = await _connection.Wallets.FirstOrDefaultAsync(i => i.Id == walletId);
                return wallet;
            }
            return null;
        }

        public async Task<TariffValidatorDto?> GetAccountTariffValidatorAsync(Guid accountId)
        {
            var walletId = await _connection.Accounts
                .Where(i => i.Id == accountId)
                .Select(i => i.WalletId)
                .FirstOrDefaultAsync();

            if (walletId != null)
            {
                var tariffValidator = await _connection.Wallets
                    .Where(i => i.Id == walletId)
                    .Select(i => i.Tariff.TariffValidator)
                    .FirstOrDefaultAsync();
                return tariffValidator;
            }
            return null;
        }

        public async Task<TariffValidatorDto?> GetOrganizationTariffValidatorAsync(Guid organizationId)
        {
            var walletId = await _connection.Organizations
                .Where(i => i.Id == organizationId)
                .Select(i => i.WalletId)
                .FirstOrDefaultAsync();

            if (walletId != null)
            {
                var tariffValidator = await _connection.Wallets
                    .Where(i => i.Id == walletId)
                    .Select(i => i.Tariff.TariffValidator)
                    .FirstOrDefaultAsync();
                return tariffValidator;
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
            var wallets = await _connection.Wallets.Where(i => i.TariffId != null && i.PaidDate > i.LastChargeDate).ToListAsync();
            return wallets;
        }

        public async Task DepositeAsync(Guid walletId, double value)
        {
            // Инкремент баланса тарифного кошелька (не overwrite).
            await _connection.Wallets.Where(i => i.Id == walletId)
                .Set(i => i.PaidDate, DateTimeOffset.UtcNow)
                .Set(i => i.Balance, i => i.Balance + value)
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

        public async Task<Guid> CreateWalletDepositAsync(WalletDepositDto item)
        {
            var result = (Guid)await _connection.InsertWithIdentityAsync(item);
            return result;
        }

        public async Task<WalletDepositDto?> GetWalletDepositAsync(Guid depositId)
        {
            return await _connection.WalletDeposits.FirstOrDefaultAsync(i => i.Id == depositId);
        }

        public async Task<WalletDepositDto?> GetWalletDepositByProviderPaymentAsync(
            PaymentProvider provider, string providerPaymentId)
        {
            return await _connection.WalletDeposits.FirstOrDefaultAsync(i =>
                i.Provider == provider && i.ProviderPaymentId == providerPaymentId);
        }

        public async Task<WalletDepositDto?> GetWalletDepositByIdempotencyAsync(
            Guid walletId, string idempotencyKey)
        {
            return await _connection.WalletDeposits.FirstOrDefaultAsync(i =>
                i.WalletId == walletId && i.IdempotencyKey == idempotencyKey);
        }

        public async Task UpdateWalletDepositAsync(
            Guid depositId,
            WalletDepositStatus status,
            string? providerPaymentId,
            DateTimeOffset? paidAt)
        {
            var query = _connection.WalletDeposits.Where(i => i.Id == depositId)
                .Set(i => i.Status, status);

            if (providerPaymentId != null)
                query = query.Set(i => i.ProviderPaymentId, providerPaymentId);

            if (paidAt != null)
                query = query.Set(i => i.PaidAt, paidAt);

            await query.UpdateAsync();
        }

        public async Task<List<WalletDepositDto>> GetWalletDepositsAsync(Guid walletId)
        {
            return await _connection.WalletDeposits
                .Where(i => i.WalletId == walletId)
                .OrderByDescending(i => i.CreateDate)
                .ToListAsync();
        }

        public async Task<Guid?> FindAccountIdByWalletAsync(Guid walletId)
        {
            return await _connection.Accounts
                .Where(i => i.WalletId == walletId)
                .Select(i => (Guid?)i.Id)
                .FirstOrDefaultAsync();
        }

        public async Task<Guid?> FindOrganizationIdByWalletAsync(Guid walletId)
        {
            return await _connection.Organizations
                .Where(i => i.WalletId == walletId)
                .Select(i => (Guid?)i.Id)
                .FirstOrDefaultAsync();
        }
    }
}
