using EList.Common.Models;
using EList.Models.ContactData;

namespace EList.Validators.Interfaces
{
    public interface IContactAccessValidator
    {
        CommandResult CanModifyAccountContact(ContactDataItem contact, Guid editorAccountId);

        CommandResult CanViewAccountContact(ContactDataItem contact, Guid? viewerAccountId);

        List<ContactDataItem> FilterAccountContacts(
            IEnumerable<ContactDataItem> contacts,
            Guid ownerAccountId,
            Guid? viewerAccountId);

        List<ContactDataItem> FilterOrganizationContacts(
            IEnumerable<ContactDataItem> contacts,
            bool canManage);
    }

}
