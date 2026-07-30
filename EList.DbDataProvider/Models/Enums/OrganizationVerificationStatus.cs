using LinqToDB.Mapping;

namespace EList.DbDataProvider.Models.Enums
{
    /// <summary>
    /// Статус верификации организации
    /// </summary>
    public enum OrganizationVerificationStatus
    {
        /// <summary>
        /// Не верифицирована
        /// </summary>
        [MapValue(Value = "unverified")]
        Unverified = 0,

        /// <summary>
        /// На проверке
        /// </summary>
        [MapValue(Value = "pending")]
        Pending = 1,

        /// <summary>
        /// Верифицирована
        /// </summary>
        [MapValue(Value = "verified")]
        Verified = 2,

        /// <summary>
        /// Отклонена
        /// </summary>
        [MapValue(Value = "rejected")]
        Rejected = 3
    }
}
