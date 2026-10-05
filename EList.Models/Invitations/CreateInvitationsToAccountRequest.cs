namespace EList.Models.Invitations
{
    /// <summary>
    /// Пригласить одного пользователя на несколько своих мероприятий.
    /// </summary>
    public class CreateInvitationsToAccountRequest
    {
        /// <summary>Кого приглашаем.</summary>
        public Guid InvitedAccountId { get; set; }

        /// <summary>Список мероприятий.</summary>
        public List<Guid> EventIds { get; set; }

        /// <summary>
        /// Если задан — приглашения от имени организации (как в CreateInvitationsRequest).
        /// </summary>
        public Guid? InviterOrganizationId { get; set; }
    }
}
