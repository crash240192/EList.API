using EList.Models.Accounts;
using EList.Models.Person;

namespace EList.Models.Subscriptions
{
    public class Subscriber
    {
        public AccountPublicData Account { get; set; }
        public PersonInfo PersonInfo { get; set; }
    }
}
