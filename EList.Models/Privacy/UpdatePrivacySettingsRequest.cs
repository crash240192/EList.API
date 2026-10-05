using EList.Models.Enums;

namespace EList.Models.Privacy
{
    /// <summary>
    /// Тело PUT /api/accounts/privacy. Все поля опциональны — обновляются только переданные.
    /// </summary>
    public class UpdatePrivacySettingsRequest
    {
        public PrivacyAudience? WhoCanInviteMe { get; set; }
        public PrivacyAudience? AgeVisibility { get; set; }
        public PrivacyAudience? GenderVisibility { get; set; }
        public bool? ShowBirthdayToday { get; set; }
        public PrivacyAudience? LocationVisibility { get; set; }
        public PrivacyAudience? ProfilePhotosVisibility { get; set; }
    }
}
