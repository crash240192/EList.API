using EList.Common.Models;
using EList.Models.Accounts;

namespace EList.Services.Interfaces
{
    public interface IAccountsService
    {
        Task<CommandResult<Guid?>> CreateAccountAsync(CreateAccountRequest request);
        Task<CommandResult<Account?>> GetAccountByTokenAsync();
        Task<CommandResult<Account?>> GetAccountAsync(Guid accountId);

        /// <summary>Lookup по логину или GUID (для gift/transfer). Без password hash.</summary>
        Task<CommandResult<AccountLookupResponse?>> LookupAccountAsync(string loginOrId);

        Task<CommandResult> UpdateLocationAsync(double latitude, double longitude);
        Task<CommandResult> UpdateLoginAsync(string newLogin);
        Task<CommandResult> DeleteMyAccountAsync();
        Task<CommandResult<AccountDataExport>> ExportMyDataAsync();
    }
}
