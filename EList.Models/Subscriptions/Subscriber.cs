using EList.Models.Accounts;
using EList.Models.Person;

namespace EList.Models.Subscriptions
{
    public class Subscriber
    {
        public Account Account { get; set; }
        public PersonInfo PersonInfo { get; set; }
    }
}
