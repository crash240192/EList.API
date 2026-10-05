using LinqToDB.Mapping;

namespace EList.DbDataProvider.Models
{
    [Table("account_privacy_settings")]
    public class AccountPrivacySettingsDto
    {
        [Column("account_id"), PrimaryKey]
        public Guid AccountId { get; set; }

        [Column("who_can_invite_me")]
        public string WhoCanInviteMe { get; set; } = "Everyone";

        [Column("age_visibility")]
        public string AgeVisibility { get; set; } = "Nobody";

        [Column("gender_visibility")]
        public string GenderVisibility { get; set; } = "Nobody";

        [Column("show_birthday_today")]
        public bool ShowBirthdayToday { get; set; }

        [Column("location_visibility")]
        public string LocationVisibility { get; set; } = "Nobody";

        [Column("profile_photos_visibility")]
        public string ProfilePhotosVisibility { get; set; } = "Everyone";

        [Column("updated_at")]
        public DateTimeOffset UpdatedAt { get; set; }
    }
}
