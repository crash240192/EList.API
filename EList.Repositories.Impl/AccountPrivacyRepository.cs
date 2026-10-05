using EList.DbDataProvider.Interfaces;
using EList.DbDataProvider.Models;
using EList.Models.Enums;
using EList.Models.Privacy;
using EList.Repositories.Interfaces;

namespace EList.Repositories.Impl
{
    public class AccountPrivacyRepository : IAccountPrivacyRepository
    {
        private readonly IAccountPrivacyDataProvider _dataProvider;

        public AccountPrivacyRepository(IAccountPrivacyDataProvider dataProvider)
        {
            _dataProvider = dataProvider ?? throw new ArgumentNullException(nameof(dataProvider));
        }

        public async Task<AccountPrivacySettings> GetOrDefaultAsync(Guid accountId)
        {
            var dto = await _dataProvider.GetAsync(accountId);
            if (dto == null)
                return AccountPrivacySettings.Defaults(accountId);

            return FromDto(dto);
        }

        public async Task UpsertAsync(AccountPrivacySettings settings)
        {
            await _dataProvider.UpsertAsync(ToDto(settings));
        }

        private static AccountPrivacySettings FromDto(AccountPrivacySettingsDto dto)
        {
            return new AccountPrivacySettings
            {
                AccountId = dto.AccountId,
                WhoCanInviteMe = ParseAudience(dto.WhoCanInviteMe, PrivacyAudience.Everyone),
                AgeVisibility = ParseAudience(dto.AgeVisibility, PrivacyAudience.Nobody),
                GenderVisibility = ParseAudience(dto.GenderVisibility, PrivacyAudience.Nobody),
                ShowBirthdayToday = dto.ShowBirthdayToday,
                LocationVisibility = ParseAudience(dto.LocationVisibility, PrivacyAudience.Nobody),
                ProfilePhotosVisibility = ParseAudience(dto.ProfilePhotosVisibility, PrivacyAudience.Everyone),
                UpdatedAt = dto.UpdatedAt
            };
        }

        private static AccountPrivacySettingsDto ToDto(AccountPrivacySettings settings)
        {
            return new AccountPrivacySettingsDto
            {
                AccountId = settings.AccountId,
                WhoCanInviteMe = settings.WhoCanInviteMe.ToString(),
                AgeVisibility = settings.AgeVisibility.ToString(),
                GenderVisibility = settings.GenderVisibility.ToString(),
                ShowBirthdayToday = settings.ShowBirthdayToday,
                LocationVisibility = settings.LocationVisibility.ToString(),
                ProfilePhotosVisibility = settings.ProfilePhotosVisibility.ToString(),
                UpdatedAt = settings.UpdatedAt ?? DateTimeOffset.UtcNow
            };
        }

        private static PrivacyAudience ParseAudience(string? value, PrivacyAudience fallback)
        {
            if (string.IsNullOrWhiteSpace(value))
                return fallback;

            return Enum.TryParse<PrivacyAudience>(value, ignoreCase: true, out var parsed)
                ? parsed
                : fallback;
        }
    }
}
