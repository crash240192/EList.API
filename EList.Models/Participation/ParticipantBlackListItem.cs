using EList.Models.Accounts;
using EList.Models.Person;

namespace EList.Models.Participation
{
    public class ParticipantBlackListItem
    {
        public Guid Id { get; set; }
        public Guid EventId { get; set; }
        public Guid AccountId { get; set; }
        public AccountPublicData Account { get; set; }
        public PersonInfo PersonInfo { get; set; }
    }
}
