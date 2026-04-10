using AutoMapper;
using EList.DbDataProvider.Interfaces;
using EList.DbDataProvider.Models;
using EList.Models.Accounts;
using EList.Repositories.Interfaces;

namespace EList.Repositories.Impl
{
    public class AccountsRepository : IAccountsRepository
    {
        private readonly IAccountsDataProvider _accountsDataProvider;
        private readonly IMapper _mapper;

        public AccountsRepository(IAccountsDataProvider accountsDataProvider,
            IMapper mapper)
        {
            _accountsDataProvider = accountsDataProvider;
            _mapper = mapper;
        }

        public async Task<Guid> CreateAccountAsync(CreateAccountRequest request)
        {
            var mappedRequest = new AccountDto
            {
                Active = true,
                Latitude = request.Latitude,
                Longitude = request.Longitude,
                PasswordHash = request.Password,
                Login = request.Login,
                LastActionDate = DateTimeOffset.Now.ToUniversalTime(),
                LastSeenDate = DateTimeOffset.Now.ToUniversalTime(),
                RegistrationDate = DateTimeOffset.Now.ToUniversalTime()
            };

            return await _accountsDataProvider.CreateAccountAsync(mappedRequest);
        }

        public async Task<Account?> GetAccountAsync(Guid id)
        {
            var result = await _accountsDataProvider.GetAccountAsync(id);
            var account = _mapper.Map<Account>(result);
            return account;
        }

        public async Task<Account?> GetAccountAsync(string login)
        {
            var result = await _accountsDataProvider.GetAccountAsync(login);
            var account = _mapper.Map<Account>(result);
            return account;
        }
        public async Task<Account?> GetAccountAsync(string login, string passwordHash)
        {
            var result = await _accountsDataProvider.GetAccountAsync(login, passwordHash);
            var account = _mapper.Map<Account>(result);
            return account;
        }

        public async Task SetAccountWalletAsync(Guid accountId, Guid walletId)
        {
            await _accountsDataProvider.SetAccountWalletAsync(accountId, walletId);
        }


        #region locations
        public async Task UpdateLocationAsync(Guid accountId, double latitude, double longitude)
        {
            await _accountsDataProvider.UpdateLocationAsync(accountId, latitude, longitude);
        }

        public async Task UpdateLoginAsync(Guid accountId, string newLogin)
        {
            await _accountsDataProvider.UpdateLoginAsync(accountId, newLogin);
        }

        public async Task UpdatePasswordAsync(Guid accountId, string newPasswordHash)
        {
            await _accountsDataProvider.UpdatePasswordAsync(accountId, newPasswordHash);
        }
        #endregion
    }
}
