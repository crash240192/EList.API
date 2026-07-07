using AutoMapper;
using EList.DbDataProvider.Interfaces;
using EList.Models.Authorization;
using EList.Repositories.Interfaces;

namespace EList.Repositories.Impl
{
    public class AuthorizationRepository : IAuthorizationRepository
    {
        private readonly IAuthorizationDataProvider _authorizationDataProvider;
        private readonly IMapper _mapper;

        public AuthorizationRepository(IAuthorizationDataProvider authorizationDataProvider, IMapper mapper)
        {
            _authorizationDataProvider = authorizationDataProvider;
            _mapper = mapper;
        }

        public async Task DecreaseActivationAttempts(Guid token)
        {
            await _authorizationDataProvider.DecreaseActivationAttempts(token);
        }
        public async Task ActivateTokenAsync(Guid token)
        {
            await _authorizationDataProvider.ActivateTokenAsync(token);
        }

        public async Task<Guid> CreateTokenAsync(Guid accountId, string clientHash)
        {
            var result = await _authorizationDataProvider.CreateTokenAsync(accountId, clientHash);
            return result;
        }

        public async Task DeactivateTokenAsync(Guid token)
        {
            await _authorizationDataProvider.DeactivateTokenAsync(token);
        }

        public async Task DeactivateAccountTokensAsync(Guid accountId)
        {
            await _authorizationDataProvider.DeactivateAccountTokensAsync(accountId);
        }

        public async Task<Authorization> GetAuthorizationDataAsync(Guid token)
        {
            var authorizationItem = await _authorizationDataProvider.GetAuthorizationDataAsync(token);
            var result = _mapper.Map<Authorization>(authorizationItem);
            return result;
        }

        public async Task<Authorization?> GetAuthorizationDataAsync(Guid accountId, string clientHash)
        {
            var authorizationItem = await _authorizationDataProvider.GetAuthorizationDataAsync(accountId, clientHash);
            var result = _mapper.Map<Authorization>(authorizationItem);
            return result;
        }

        public async Task<AuthorizationResponse?> GetAuthorizationDataAsync(string clientHash)
        {
            var authorizationItem = await _authorizationDataProvider.GetAuthorizationDataAsync(clientHash);
            var result = _mapper.Map<AuthorizationResponse>(authorizationItem);
            return result;
        }
    }
}
