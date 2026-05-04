using EList.Models.Accounts;
using EList.Models.Person;

namespace EList.Models.Participation
{
    public class Participant
    {
        public AccountPublicData Account { get; set; }
        public PersonInfo PersonInfo { get; set; }
    }
}
