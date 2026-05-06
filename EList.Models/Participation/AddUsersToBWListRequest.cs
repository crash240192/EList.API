namespace EList.Models.Participation
{
    public class AddUsersToBWListRequest
    {
        public Guid EventId { get; set; }
        public List<Guid> AccountIds { get; set; }
    }
}
