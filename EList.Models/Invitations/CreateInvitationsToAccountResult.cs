namespace EList.Models.Invitations
{
    public class CreateInvitationsToAccountResult
    {
        public List<Guid> SucceededEventIds { get; set; } = new();

        public List<InvitationToAccountFailure> Failures { get; set; } = new();
    }

    public class InvitationToAccountFailure
    {
        public Guid EventId { get; set; }

        public int ErrorCode { get; set; }

        public string Message { get; set; }
    }
}
