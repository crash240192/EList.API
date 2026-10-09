using LinqToDB.Mapping;

namespace EList.DbDataProvider.Models.Enums
{
    /// <summary>
    /// Роль участника организации
    /// </summary>
    public enum OrganizationMemberRole
    {
        /// <summary>
        /// Владелец
        /// </summary>
        [MapValue(Value = "owner")]
        Owner = 0,

        /// <summary>
        /// Менеджер
        /// </summary>
        [MapValue(Value = "manager")]
        Manager = 1,

        /// <summary>
        /// Билетёр
        /// </summary>
        [MapValue(Value = "ticket_taker")]
        TicketTaker = 2
    }
}
