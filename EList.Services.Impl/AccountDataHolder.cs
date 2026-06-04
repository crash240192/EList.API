using EList.Models.Accounts;
using EList.Models.Person;
using EList.Services.Interfaces;

namespace EList.Services.Impl
{
    public class AccountDataHolder : IAccountDataHolder
    {
        public Guid Token { get; set; }
        public Account Account { get; set; }
        public PersonInfo? PersonInfo { get; set; }
        public Guid AccountId => Account.Id;
    }
}
