using EList.Models.ContactData;

namespace EList.Repositories.Interfaces
{
    public interface IContactsRepository
    {
        Task<Guid> CreateContactTypeAsync(ContactTypeRequest request);
        Task<List<ContactType>> GetAllContactTypesAsync();
        Task<ContactType?> GetContactTypeAsync(Guid id);
        Task UpdateContactTypeAsync(Guid id, ContactTypeRequest request);

        Task<Guid> CreateContactAsync(ContactRequest request);
        Task<bool> CheckContactIsEmptyAsync(string contactValue, Guid contactType);
        Task UpdateContactAsync(Guid id, ContactRequest request);
        Task BindAccountAndContactAsync(Guid accountId, Guid contactId);
        Task BindOrganizationAndContactAsync(Guid organizationId, Guid contactId);
        Task<ContactDataItem?> GetAccountContactAsync(Guid id);
        Task<ContactDataItem?> GetOrganizationContactAsync(Guid id);
        Task<ContactDataItem?> GetContactAsync(string contactValue);
        Task<ContactDataItem?> GetAuthorizationContactAsync(Guid accountId);
        Task<List<ContactDataItem>> GetAccountContactsAsync(Guid accountId);
        Task<List<ContactDataItem>> GetOrganizationContactsAsync(Guid organizationId);
    }
}
