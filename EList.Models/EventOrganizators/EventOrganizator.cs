using EList.Models.Accounts;
using EList.Models.Person;

namespace EList.Models.EventOrganizators
{
    public class EventOrganizator
    {
        public Guid Id { get; set; }
        public Guid EventId { get; set; }
        public AccountPublicData? Account { get; set; }
        public PersonInfo? PersonInfo { get; set; }
        public Guid? OrganizationId { get; set; }
    }
}
