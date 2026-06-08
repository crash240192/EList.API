using EList.DbDataProvider.Interfaces;
using EList.DbDataProvider.Models;
using LinqToDB;
using LinqToDB.Async;

namespace EList.DbDataProvider.DataProviders
{
    public class AccountsDataProvider : DataProviderBase, IAccountsDataProvider
    {
        public AccountsDataProvider(IDataConnectionProvider dataConnectionProvider) : base(dataConnectionProvider)
        {
        }

        public async Task<Guid> CreateAccountAsync(AccountDto item)
        {
            var accountId = (Guid)await _connection.InsertWithIdentityAsync(item);
            return accountId;
        }

        public async Task<AccountDto?> GetAccountAsync(Guid id)
        {
            var result = await _connection.Accounts
                .LoadWith(i => i.Avatars)
                .FirstOrDefaultAsync(i => i.Id == id);
            return result;
        }

        public async Task<AccountDto?> GetAccountAsync(string login)
        {
            var result = await _connection.Accounts
                .LoadWith(i => i.Avatars)
                .FirstOrDefaultAsync(i => i.Login == login);
            return result;
        }

        public async Task<AccountDto?> GetAccountAsync(string login, string passwordHash)
        {
            var result = await _connection.Accounts.FirstOrDefaultAsync(i => i.Login == login && i.PasswordHash == passwordHash);
            return result;
        }

        public async Task UpdateLocationAsync(Guid accountId, double latitude, double longitude)
        {
            var result = await _connection.Accounts.Where(i => i.Id == accountId)
                .Set(i => i.Latitude, latitude)
                .Set(i => i.Longitude, longitude)
                .UpdateAsync();
        }

        public async Task UpdateLoginAsync(Guid accountId, string newLogin)
        {
            var result = await _connection.Accounts.Where(i => i.Id == accountId)
                .Set(i => i.Login, newLogin)
                .UpdateAsync();
        }

        public async Task UpdatePasswordAsync(Guid accountId, string newPasswordHash)
        {
            var result = await _connection.Accounts.Where(i => i.Id == accountId)
                .Set(i => i.PasswordHash, newPasswordHash)
                .UpdateAsync();
        }

        public async Task SetAccountWalletAsync(Guid accountId, Guid walletId)
        {
            await _connection.Accounts.Where(i => i.Id == accountId)
                .Set(i => i.WalletId, walletId)
                .UpdateAsync();
        }
    }
}
