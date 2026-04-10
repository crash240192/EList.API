using EList.DbDataProvider.Models;

namespace EList.DbDataProvider.Interfaces
{
    public interface IAuthorizationDataProvider 
    {
        Task<Guid> CreateTokenAsync(Guid accountId, string clientHash);
        Task DecreaseActivationAttempts(Guid token);
        Task ActivateTokenAsync(Guid token);
        Task DeactivateTokenAsync(Guid token);
        Task DeactivateAccountTokensAsync(Guid accountId);
        Task<AuthorizationDto?> GetAuthorizationDataAsync(Guid token);
        Task<AuthorizationDto?> GetAuthorizationDataAsync(Guid accountId, string clientHash);
        Task<AuthorizationDto?> GetAuthorizationDataAsync(string clientHash);
    }
}
