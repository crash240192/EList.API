namespace EList.Models.Invitations
{
    /// <summary>
    /// POST /api/invitations/canInviteToEvent — проверка списка аккаунтов для одного события.
    /// </summary>
    public class CanInviteToEventByEventRequest
    {
        public Guid EventId { get; set; }

        public List<Guid> AccountIds { get; set; } = new();

        public Guid? InviterOrganizationId { get; set; }
    }
}
