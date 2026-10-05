using EList.Models.Privacy;

namespace EList.Repositories.Interfaces
{
    public interface IAccountPrivacyRepository
    {
        Task<AccountPrivacySettings> GetOrDefaultAsync(Guid accountId);
        Task UpsertAsync(AccountPrivacySettings settings);
    }
}
