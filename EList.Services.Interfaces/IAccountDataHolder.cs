using EList.Models.Accounts;
using EList.Models.Person;

namespace EList.Services.Interfaces
{
    public interface IAccountDataHolder
    {
        Guid? Token { get; set; }
        Account? Account { get; set; }
        string Jwt { get; set; }
        string ClientHash { get; set; }
        string ClientInfo { get; set; }
        PersonInfo? PersonInfo { get; set; }
        Guid? AccountId { get; }
        public bool AdultConfirmed { get; set; }

        string AccountNameFullString { get; }
        public int Age { get; }
    }
}
