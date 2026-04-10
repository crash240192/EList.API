using EList.Common.Configuration;
using EList.Common.Support;
using EList.Models.Accounts;
using EList.Validators.Interfaces;
using Newtonsoft.Json;
using System.Net.Mail;

namespace EList.Validators.Impl
{
    public class UserDataValidator : IUserDataValidator
    {
        public bool IsEmailValid(string email)
        {
            return MailAddress.TryCreate(email, out _);
        }
    }
}
