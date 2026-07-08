using EList.Models.Authorization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EList.Repositories.Interfaces
{
    public interface IAuthorizationRepository
    {
        Task<Guid> CreateTokenAsync(Guid accountId, string clientHash);
        Task DecreaseActivationAttempts(Guid token);
        Task ActivateTokenAsync(Guid token);
        Task DeactivateTokenAsync(Guid token);
        Task DeactivateAccountTokensAsync(Guid accountId);
        Task<Authorization> GetAuthorizationDataAsync(Guid token);
        Task<Authorization?> GetAuthorizationDataAsync(Guid accountId, string clientHash);
        Task<AuthorizationResponse?> GetAuthorizationDataAsync(string clientHash);
        Task GenerateNewActivationKey(Guid token);
    }
}
