using EList.Common.Models;
using EList.Common.Support;
using EList.Models.ContactData;
using EList.Validators.Interfaces;

namespace EList.Validators.Impl
{
    public class ContactAccessValidator : IContactAccessValidator
    {
        public CommandResult CanModifyAccountContact(ContactDataItem contact, Guid editorAccountId)
        {
            if (contact.AccountId == null)
                return CommandResult.Fail(ErrorCode.ContactNotFound, "Контакт пользователя не найден");

            if (contact.AccountId != editorAccountId)
                return CommandResult.Fail(ErrorCode.AccessError, "Изменять контакт может только владелец аккаунта");

            return CommandResult.OK;
        }

        public CommandResult CanViewAccountContact(ContactDataItem contact, Guid? viewerAccountId)
        {
            if (contact.IsAuthorizationContact && contact.AccountId != viewerAccountId)
                return CommandResult.Fail(ErrorCode.AccessError, "Контакт для авторизации недоступен для просмотра");

            if (contact.AccountId != viewerAccountId && !contact.Show)
                return CommandResult.Fail(ErrorCode.AccessError, "Контакт недоступен для просмотра");

            return CommandResult.OK;
        }

        public List<ContactDataItem> FilterAccountContacts(
            IEnumerable<ContactDataItem> contacts,
            Guid ownerAccountId,
            Guid? viewerAccountId)
        {
            var isOwner = viewerAccountId == ownerAccountId;

            return contacts
                .Where(contact => isOwner
                    || (!contact.IsAuthorizationContact && contact.Show))
                .ToList();
        }

        public List<ContactDataItem> FilterOrganizationContacts(
            IEnumerable<ContactDataItem> contacts,
            bool canManage)
        {
            if (canManage)
                return contacts.ToList();

            return contacts.Where(contact => contact.Show).ToList();
        }
    }
}
