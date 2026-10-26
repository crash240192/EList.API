using EList.DbDataProvider.Interfaces;
using EList.DbDataProvider.Models;
using LinqToDB;
using LinqToDB.Async;

namespace EList.DbDataProvider.DataProviders
{
    public class AccountsDataProvider : DataProviderBase, IAccountsDataProvider
    {
        public AccountsDataProvider(IDataConnectionProvider dataConnectionProvider) : base(dataConnectionProvider)
        {
        }

        public async Task<Guid> CreateAccountAsync(AccountDto item)
        {
            var accountId = (Guid)await _connection.InsertWithIdentityAsync(item);
            return accountId;
        }

        public async Task<AccountDto?> GetAccountAsync(Guid id)
        {
            var result = await _connection.Accounts
                .LoadWith(i => i.Avatars)
                .FirstOrDefaultAsync(i => i.Id == id);
            return result;
        }

        public async Task<AccountDto?> GetAccountAsync(string login)
        {
            var result = await _connection.Accounts
                .LoadWith(i => i.Avatars)
                .FirstOrDefaultAsync(i => i.Login == login);
            return result;
        }

        public async Task<AccountDto?> GetAccountAsync(string login, string passwordHash)
        {
            var result = await _connection.Accounts.FirstOrDefaultAsync(i => i.Login == login && i.PasswordHash == passwordHash);
            return result;
        }

        public async Task UpdateLocationAsync(Guid accountId, double latitude, double longitude)
        {
            var result = await _connection.Accounts.Where(i => i.Id == accountId)
                .Set(i => i.Latitude, latitude)
                .Set(i => i.Longitude, longitude)
                .UpdateAsync();
        }

        public async Task UpdateLoginAsync(Guid accountId, string newLogin)
        {
            var result = await _connection.Accounts.Where(i => i.Id == accountId)
                .Set(i => i.Login, newLogin)
                .UpdateAsync();
        }

        public async Task UpdatePasswordAsync(Guid accountId, string newPasswordHash)
        {
            var result = await _connection.Accounts.Where(i => i.Id == accountId)
                .Set(i => i.PasswordHash, newPasswordHash)
                .UpdateAsync();
        }

        public async Task SetAccountWalletAsync(Guid accountId, Guid walletId)
        {
            await _connection.Accounts.Where(i => i.Id == accountId)
                .Set(i => i.WalletId, walletId)
                .UpdateAsync();
        }

        public async Task SetAccountActiveAsync(Guid accountId, bool active)
        {
            await _connection.Accounts.Where(i => i.Id == accountId)
                .Set(i => i.Active, active)
                .UpdateAsync();
        }

        public async Task ClearSensitiveDataAsync(Guid accountId)
        {
            await _connection.Accounts.Where(i => i.Id == accountId)
                .Set(i => i.Latitude, (double?)null)
                .Set(i => i.Longitude, (double?)null)
                .Set(i => i.PasswordHash, $"deleted:{accountId:N}")
                .Set(i => i.HeaderImageId, (Guid?)null)
                .Set(i => i.HeaderFocusX, (double?)null)
                .Set(i => i.HeaderFocusY, (double?)null)
                .Set(i => i.HeaderBlur, false)
                .UpdateAsync();
        }

        public async Task SetAccountHeaderAsync(
            Guid accountId,
            Guid? imageId,
            double? focusX,
            double? focusY,
            bool blur)
        {
            await _connection.Accounts.Where(i => i.Id == accountId)
                .Set(i => i.HeaderImageId, imageId)
                .Set(i => i.HeaderFocusX, focusX)
                .Set(i => i.HeaderFocusY, focusY)
                .Set(i => i.HeaderBlur, blur)
                .UpdateAsync();
        }

        public async Task<List<Guid>> GetEventPinIdsAsync(Guid accountId)
        {
            return await _connection.AccountEventPins
                .Where(i => i.AccountId == accountId)
                .OrderByDescending(i => i.SortOrder)
                .ThenByDescending(i => i.PinnedAt)
                .Select(i => i.EventId)
                .ToListAsync();
        }

        public async Task ReplaceEventPinsAsync(Guid accountId, IReadOnlyList<Guid> eventIds)
        {
            await _connection.AccountEventPins
                .Where(i => i.AccountId == accountId)
                .DeleteAsync();

            if (eventIds == null || eventIds.Count == 0)
                return;

            var now = DateTimeOffset.UtcNow;
            // Первый в списке — сверху (больший sort_order).
            var requested = eventIds.Where(id => id != Guid.Empty).Distinct().ToList();
            if (requested.Count == 0)
                return;

            var existing = await _connection.Events
                .Where(e => requested.Contains(e.Id))
                .Select(e => e.Id)
                .ToListAsync();
            var existingSet = new HashSet<Guid>(existing);
            var distinct = requested.Where(id => existingSet.Contains(id)).ToList();

            for (var i = 0; i < distinct.Count; i++)
            {
                await _connection.InsertAsync(new AccountEventPinDto
                {
                    AccountId = accountId,
                    EventId = distinct[i],
                    PinnedAt = now,
                    SortOrder = distinct.Count - 1 - i
                });
            }
        }
    }
}
