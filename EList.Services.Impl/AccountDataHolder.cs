using EList.Models.Accounts;
using EList.Models.Person;
using EList.Services.Interfaces;

namespace EList.Services.Impl
{
    public class AccountDataHolder : IAccountDataHolder
    {
        private bool? _adultConfirmed = false;
        public Guid? Token { get; set; }
        public string Jwt { get; set; }
        public string ClientHash { get; set; }
        public string ClientInfo { get; set; }
        public Account? Account { get; set; }
        public PersonInfo? PersonInfo { get; set; }
        public Guid? AccountId => Account?.Id;

        public string AccountNameFullString
        {
            get
            {
                if (Account == null)
                    return null;

                return !string.IsNullOrWhiteSpace(PersonInfo?.FIO)
                ? $"{PersonInfo?.FIO} ({Account.Login})"
                : $"{Account.Login}";
            }
        }

        public int Age
        {
            get
            {
                if (PersonInfo?.BirthDate == null)
                    return 0;

                var age = DateTime.Today.Year - PersonInfo.BirthDate.Value.Year;
                if (PersonInfo.BirthDate.Value.Date > DateTime.Today.AddYears(-age)) age--;
                return age;
            }
        }

        public bool AdultConfirmed
        {
            get
            {
                if (_adultConfirmed != null)
                    return _adultConfirmed.Value;
                if (Age > 18) 
                    return true;
                return false;
            }
            set
            {
                _adultConfirmed = value;
            }
        }
    }
}
