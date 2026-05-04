namespace EList.Models.Participation
{
    public class ParticipantBlackListItemRequest
    {
        public Guid EventId { get; set; }
        public Guid AccountId { get; set; }
    }
}
