using EList.Models.Accounts;
using EList.Models.Organizations;
using EList.Models.Person;

namespace EList.Models.EventOrganizators
{
    public class EventOrganizator
    {
        public Guid Id { get; set; }
        public Guid EventId { get; set; }
        public Guid? AccountId { get; set; }
        public AccountPublicData? Account { get; set; }
        public PersonInfo? PersonInfo { get; set; }
        public Organization? Organization { get; set; }
        public Guid? OrganizationId { get; set; }
    }
}
