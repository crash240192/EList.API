using EList.Common.Configuration;
using EList.Common.Support;
using EList.DbDataProvider.Interfaces;
using EList.DbDataProvider.Models;
using LinqToDB;
using LinqToDB.Async;

namespace EList.DbDataProvider.DataProviders
{
    public class AuthorizationDataProvider : DataProviderBase, IAuthorizationDataProvider
    {
        private int activationAttemptsCount = 5;
        private int activationKeyLength = ConfigurationManager.AppSettings.Contains("system:authorization:activationCodeLength")
            ? Convert.ToInt32(ConfigurationManager.AppSettings["system:authorization:activationCodeLength"]) : 6;

        public AuthorizationDataProvider(IDataConnectionProvider dataConnectionProvider) : base(dataConnectionProvider)
        {
        }

        public async Task<Guid> CreateTokenAsync(Guid accountId, string clientHash)
        {
            var newToken = new AuthorizationDto
            {
                CreationDate = DateTimeOffset.Now.ToUniversalTime(),
                AuthorizationDate = DateTimeOffset.Now.ToUniversalTime(),
                Active = false,
                AccountId = accountId,
                ClientHash = clientHash,
                ActivationAttemptsRemaining = activationAttemptsCount,
                ActivationKey = ActivationKeysGenerator.Generate(activationKeyLength)
            };

            var tokenId = (Guid) await _connection.InsertWithIdentityAsync(newToken);
            return tokenId;
        }

        public async Task<AuthorizationDto?> GetAuthorizationDataAsync(Guid token)
        {
            var result = await _connection.Authorization.FirstOrDefaultAsync(i => i.Token == token);
            return result;
        }

        public async Task<AuthorizationDto?> GetAuthorizationDataAsync(Guid accountId, string clientHash)
        {
            var result = await _connection.Authorization.FirstOrDefaultAsync(i => i.AccountId == accountId && i.ClientHash == clientHash);
            return result;
        }

        public async Task<AuthorizationDto?> GetAuthorizationDataAsync(string clientHash)
        {
            var result = await _connection.Authorization.FirstOrDefaultAsync(i => i.ClientHash == clientHash);
            return result;
        }

        public async Task DecreaseActivationAttempts(Guid token)
        {
            var existingToken = await _connection.Authorization.FirstOrDefaultAsync(i => i.Token == token);

            if (existingToken != null)
            {
                var attemptsCount = existingToken.ActivationAttemptsRemaining - 1;
                var res = await _connection.Authorization.Where(i => i.Token == token)
                .Set(i => i.ActivationAttemptsRemaining, attemptsCount)
                .UpdateAsync();
            }
        }

        public async Task ActivateTokenAsync(Guid token)
        {
            await _connection.Authorization.Where(i => i.Token == token)
                .Set(i => i.Active, true)
                .UpdateAsync();
        }

        public async Task DeactivateTokenAsync(Guid token)
        {
            await _connection.Authorization.Where(i => i.Token == token)
                .Set(i => i.Active, false)
                .Set(i => i.ActivationKey, ActivationKeysGenerator.Generate(activationKeyLength))
                .Set(i => i.ActivationAttemptsRemaining, activationAttemptsCount)
                .UpdateAsync();
        }

        public async Task DeactivateAccountTokensAsync(Guid accountId)
        {
            await _connection.Authorization
                .Where(i => i.AccountId == accountId)
                .Set(i => i.Active, false)
                .UpdateAsync();
        }
    }
}
