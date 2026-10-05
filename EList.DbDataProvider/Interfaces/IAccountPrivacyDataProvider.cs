using EList.DbDataProvider.Models;

namespace EList.DbDataProvider.Interfaces
{
    public interface IAccountPrivacyDataProvider
    {
        Task<AccountPrivacySettingsDto?> GetAsync(Guid accountId);
        Task UpsertAsync(AccountPrivacySettingsDto item);
    }
}
