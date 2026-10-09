namespace EList.Models.Enums
{
    /// <summary>
    /// Роль участника организации
    /// </summary>
    public enum OrganizationMemberRole
    {
        /// <summary>
        /// Владелец
        /// </summary>
        Owner = 0,

        /// <summary>
        /// Менеджер
        /// </summary>
        Manager = 1,

        /// <summary>
        /// Билетёр — check-in / desk по назначенным событиям, без орг-админки
        /// </summary>
        TicketTaker = 2
    }
}
