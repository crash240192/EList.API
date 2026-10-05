using EList.Models.Enums;

namespace EList.Models.Privacy
{
    /// <summary>
    /// Настройки приватности профиля аккаунта.
    /// </summary>
    public class AccountPrivacySettings
    {
        public Guid AccountId { get; set; }

        /// <summary>
        /// Кто может приглашать владельца на мероприятия.
        /// <see cref="PrivacyAudience.Subscriptions"/> = только те, на кого подписан владелец.
        /// </summary>
        public PrivacyAudience WhoCanInviteMe { get; set; } = PrivacyAudience.Everyone;

        /// <summary>Кто видит возраст (без полной даты рождения).</summary>
        public PrivacyAudience AgeVisibility { get; set; } = PrivacyAudience.Nobody;

        /// <summary>Кто видит пол.</summary>
        public PrivacyAudience GenderVisibility { get; set; } = PrivacyAudience.Nobody;

        /// <summary>
        /// Показывать акцент «день рождения сегодня» зрителям, которым виден возраст
        /// (или всем авторизованным, если возраст скрыт — только флаг без возраста).
        /// </summary>
        public bool ShowBirthdayToday { get; set; }

        /// <summary>Кто видит местоположение / город на профиле.</summary>
        public PrivacyAudience LocationVisibility { get; set; } = PrivacyAudience.Nobody;

        /// <summary>Кто видит фотографии на странице профиля (личные альбомы / галерея).</summary>
        public PrivacyAudience ProfilePhotosVisibility { get; set; } = PrivacyAudience.Everyone;

        public DateTimeOffset? UpdatedAt { get; set; }

        public static AccountPrivacySettings Defaults(Guid accountId) => new()
        {
            AccountId = accountId,
            WhoCanInviteMe = PrivacyAudience.Everyone,
            AgeVisibility = PrivacyAudience.Nobody,
            GenderVisibility = PrivacyAudience.Nobody,
            ShowBirthdayToday = false,
            LocationVisibility = PrivacyAudience.Nobody,
            ProfilePhotosVisibility = PrivacyAudience.Everyone
        };
    }
}
