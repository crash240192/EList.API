using System.Net.Mail;

namespace EList.Validators.Impl
{
    public class ContactDataValidator
    {
        private const string PHONE_TYPE_ID = "";
        private const string EMAIL_TYPE_IDD = "";

        public bool ValidateIsEmail(string value)
        { 
            var isEmail = MailAddress.TryCreate(value, out var emailAddress);
            return isEmail;
        }

        public bool ValidatePhoneNumber(string value)
        {
            throw new NotImplementedException();
        }
    }
}
