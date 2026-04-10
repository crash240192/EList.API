using System;

namespace EList.Models.Accounts
{
    public class UserHashData
    {
        public Guid UserId { get; set; }
        public string Email { get; set; }
        public DateTime ExpirationDate { get; set; }
    }
}
