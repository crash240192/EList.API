using EList.Common.Models;
using EList.Models.Accounts;

namespace EList.Services.Interfaces
{
    public interface IAccountsService
    {
        Task<CommandResult<Guid?>> CreateAccountAsync(CreateAccountRequest request, string clientHash);
        Task<CommandResult<Account?>> GetAccountByTokenAsync();
        Task<CommandResult<Account?>> GetAccountAsync(Guid accountId);
        Task<CommandResult> UpdateLocationAsync(double latitude, double longitude);
        Task<CommandResult> UpdateLoginAsync(string newLogin);
        Task<CommandResult> ChangePasswordAsync(ChangePasswordRequest request);
    }
}
