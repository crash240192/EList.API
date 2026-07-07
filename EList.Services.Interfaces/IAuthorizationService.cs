using EList.Common.Models;
using EList.Models.Accounts;
using EList.Models.Authorization;

namespace EList.Services.Interfaces
{
    public interface IAuthorizationService
    {
        Task<CommandResult<AuthorizationResponse>> AuthorizeAsync(string login, string password, string clientHash);

        Task<CommandResult<string>> SendActivationCodeAsync();
        Task<CommandResult<Authorization?>> GetAuthorizationDataAsync(Guid token);
        Task<CommandResult<AuthorizationResponse?>> GetAuthorizationDataAsync(string clientHash);
        
        [Obsolete]
        Task<CommandResult<Guid>> CreateTokenAsync(string clientHash);

        Task<CommandResult> ActivateTokenAsync(string activationKey, string clientHash);
        Task<CommandResult> DeactivateTokenAsync(Guid token);

        Task<CommandResult> ChangePasswordAsync(ChangePasswordRequest request);
        Task<CommandResult> ForgotPasswordAsync(string login);
    }
}
