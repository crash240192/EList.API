namespace EList.Models.EventOrganizators
{
    public class EventOrganizator
    {
        public Guid Id { get; set; }
        public Guid EventId { get; set; }
        public Guid? AccountId { get; set; }
        public Guid? OrganizationId { get; set; }
    }
}
