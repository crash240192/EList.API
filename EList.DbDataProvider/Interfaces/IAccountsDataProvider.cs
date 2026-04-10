using EList.DbDataProvider.Models;

namespace EList.DbDataProvider.Interfaces
{
    public interface IAccountsDataProvider 
    {
        Task<Guid> CreateAccountAsync(AccountDto request);
        Task<AccountDto?> GetAccountAsync(Guid id);
        Task<AccountDto?> GetAccountAsync(string login);
        Task<AccountDto?> GetAccountAsync(string login, string passwordHash);
        Task UpdateLocationAsync(Guid accountId, double latitude, double longitude);
        Task UpdateLoginAsync(Guid accountId, string newLogin);
        Task UpdatePasswordAsync(Guid accountId, string newPasswordHash);
        Task SetAccountWalletAsync(Guid accountId, Guid walletId);
    }
}
