using EList.DbDataProvider.Interfaces;
using EList.DbDataProvider.Models;
using LinqToDB;
using LinqToDB.Async;

namespace EList.DbDataProvider.DataProviders
{
    public class AccountPrivacyDataProvider : DataProviderBase, IAccountPrivacyDataProvider
    {
        public AccountPrivacyDataProvider(IDataConnectionProvider dataConnectionProvider)
            : base(dataConnectionProvider)
        {
        }

        public async Task<AccountPrivacySettingsDto?> GetAsync(Guid accountId)
        {
            return await _connection.AccountPrivacySettings
                .FirstOrDefaultAsync(i => i.AccountId == accountId);
        }

        public async Task UpsertAsync(AccountPrivacySettingsDto item)
        {
            var existing = await _connection.AccountPrivacySettings
                .AnyAsync(i => i.AccountId == item.AccountId);

            if (!existing)
            {
                await _connection.InsertAsync(item);
                return;
            }

            await _connection.AccountPrivacySettings
                .Where(i => i.AccountId == item.AccountId)
                .Set(i => i.WhoCanInviteMe, item.WhoCanInviteMe)
                .Set(i => i.AgeVisibility, item.AgeVisibility)
                .Set(i => i.GenderVisibility, item.GenderVisibility)
                .Set(i => i.ShowBirthdayToday, item.ShowBirthdayToday)
                .Set(i => i.LocationVisibility, item.LocationVisibility)
                .Set(i => i.ProfilePhotosVisibility, item.ProfilePhotosVisibility)
                .Set(i => i.UpdatedAt, item.UpdatedAt)
                .UpdateAsync();
        }
    }
}
