using EList.Common.Models;
using EList.Models.Accounts;
using EList.Models.Events;

namespace EList.Services.Interfaces
{
    public interface IAccountsService
    {
        Task<CommandResult<Guid?>> CreateAccountAsync(CreateAccountRequest request);

        /// <summary>
        /// Проверка, что логин и контакт авторизации свободны (до шага 2 регистрации).
        /// </summary>
        Task<CommandResult> CheckRegistrationAvailabilityAsync(CheckRegistrationAvailabilityRequest request);

        Task<CommandResult<Account?>> GetAccountByTokenAsync();
        Task<CommandResult<Account?>> GetAccountAsync(Guid accountId);

        /// <summary>Lookup по логину или GUID (для gift/transfer). Без password hash.</summary>
        Task<CommandResult<AccountLookupResponse?>> LookupAccountAsync(string loginOrId);

        Task<CommandResult> UpdateLocationAsync(double latitude, double longitude);
        Task<CommandResult> UpdateLoginAsync(string newLogin);
        Task<CommandResult> DeleteMyAccountAsync();
        Task<CommandResult<AccountDataExport>> ExportMyDataAsync();

        /// <summary>Установить / сбросить фото шапки профиля (focus + blur).</summary>
        Task<CommandResult<Account?>> SetAccountHeaderAsync(AccountHeaderRequest request);

        Task<CommandResult<MyEventPinsResponse>> GetMyEventPinsAsync();
        Task<CommandResult<MyEventPinsResponse>> SetMyEventPinsAsync(MyEventPinsRequest request);
    }
}
