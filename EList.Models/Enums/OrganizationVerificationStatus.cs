namespace EList.Models.Enums
{
    /// <summary>
    /// Статус верификации организации
    /// </summary>
    public enum OrganizationVerificationStatus
    {
        /// <summary>
        /// Не верифицирована
        /// </summary>
        Unverified = 0,

        /// <summary>
        /// На проверке
        /// </summary>
        Pending = 1,

        /// <summary>
        /// Верифицирована
        /// </summary>
        Verified = 2,

        /// <summary>
        /// Отклонена
        /// </summary>
        Rejected = 3
    }
}
