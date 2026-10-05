namespace EList.Models.Invitations
{
    /// <summary>
    /// POST /api/invitations/canInviteToEvents — проверка списка событий для одного аккаунта.
    /// </summary>
    public class CanInviteToEventByAccountRequest
    {
        public Guid AccountId { get; set; }

        public List<Guid> EventIds { get; set; } = new();

        public Guid? InviterOrganizationId { get; set; }
    }
}
