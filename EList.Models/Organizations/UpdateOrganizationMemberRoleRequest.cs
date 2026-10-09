using EList.Models.Enums;

namespace EList.Models.Organizations
{
    /// <summary>
    /// Запрос на смену роли участника организации
    /// </summary>
    public class UpdateOrganizationMemberRoleRequest
    {
        /// <summary>
        /// Идентификатор аккаунта участника
        /// </summary>
        public Guid AccountId { get; set; }

        /// <summary>
        /// Новая роль (Manager или TicketTaker; Owner — только через transferOwnership)
        /// </summary>
        public OrganizationMemberRole Role { get; set; }
    }
}
