using EList.Models.Accounts;

namespace EList.Repositories.Interfaces
{
    public interface IAccountsRepository
    {
        Task<Guid> CreateAccountAsync(CreateAccountRequest request);
        Task<Account?> GetAccountAsync(Guid id);
        Task<Account?> GetAccountAsync(string login);
        Task<Account?> GetAccountAsync(string login, string passwordHash);
        Task UpdateLocationAsync(Guid accountId, double latitude, double longitude);
        Task UpdateLoginAsync(Guid accountId, string newLogin);
        Task UpdatePasswordAsync(Guid accountId, string newPasswordHash);

        Task SetAccountWalletAsync(Guid accountId, Guid walletId);
    }
}
