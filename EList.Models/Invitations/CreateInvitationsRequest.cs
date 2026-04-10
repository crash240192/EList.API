namespace EList.Models.Invitations
{
    /// <summary>
    /// Тело запроса на создание приглашений
    /// </summary>
    public class CreateInvitationsRequest
    {
        /// <summary>
        /// Список приглашаемых аккаунтов
        /// </summary>
        public List<Guid> AccountIds { get; set; }

        /// <summary>
        /// Идентификатор приглашающей организации
        /// </summary>
        public Guid? InviterOrganizationId { get; set; }

        /// <summary>
        /// Идентификатор мероприятия
        /// </summary>
        public Guid EventId { get; set; }
    }
}
